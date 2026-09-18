import test from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { webcrypto } from 'node:crypto'
import vm from 'node:vm'
import ts from 'typescript'

const source = ts.transpileModule(readFileSync(new URL('../shared-ui/src/lib/activity.ts', import.meta.url), 'utf8'), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 }
}).outputText
function setup() {
  const storage = new Map(), session = new Map(), requests = [], timers = [], listeners = new Map()
  let ok = true
  const fakeStorage = map => ({ getItem: key => map.get(key) ?? null, setItem: (key, value) => map.set(key, value) })
  const context = vm.createContext({ exports: {}, crypto: webcrypto, Date, JSON, Set, Array, Number,
    atob, localStorage: fakeStorage(storage), sessionStorage: fakeStorage(session),
    location: { pathname: '/reset-password', search: '?token=never-capture-this' }, navigator: { onLine: true },
    AbortSignal, fetch: async (url, options) => { requests.push({ url, ...options }); return { ok, status: ok ? 204 : 503 } },
    window: { setInterval: fn => timers.push(fn), addEventListener: (name, fn) => listeners.set(name, fn) },
    document: { addEventListener: (name, fn) => listeners.set(name, fn) }
  })
  vm.runInContext(source, context)
  context.exports.initActivity('employee', '/api')
  return { api: context.exports, storage, requests, session, listeners, setOk: value => { ok = value },
    flush: async () => { timers[0](); await new Promise(resolve => setImmediate(resolve)) } }
}
test('telemetry excludes query strings and credentials from payload and persists retry IDs', async () => {
  const s = setup()
  s.api.trackActivity('page.view', 'router')
  s.setOk(false)
  await s.flush()
  s.setOk(true)
  await s.flush()
  assert.equal(s.requests[0].body, s.requests[1].body)
  assert.ok(!s.requests[0].body.includes('never-capture-this'))
  assert.equal(JSON.parse(s.session.values().next().value).queue.length, 0)
})
test('switching credentials never attributes queued events to the next account', async () => {
  const s = setup()
  s.api.trackActivity('ui.click', 'previous-user-action')
  s.storage.set('token', 'new-token')
  s.api.trackActivity('page.view', 'current-user-action')
  await s.flush()
  assert.ok(!s.requests[0].body.includes('previous-user-action'))
  assert.ok(s.requests[0].body.includes('current-user-action'))
  assert.equal(s.requests[0].headers.Authorization, 'Bearer new-token')
  assert.ok(!s.session.values().next().value.includes('new-token'))
})
test('bounded queue reports losses without blocking calls', async () => {
  const s = setup()
  for (let i = 0; i < 600; i++) s.api.trackActivity('ui.click', `button:${i}`)
  const saved = JSON.parse(s.session.values().next().value)
  assert.equal(saved.queue.length, 500)
  assert.equal(saved.dropped, 100)
  await s.flush()
  assert.equal(JSON.parse(s.requests[0].body).events.length, 50)
})
test('logout dispatches with old credentials before the context changes', async () => {
  const s = setup()
  s.storage.set('token', 'old-token')
  s.api.endActivityContext()
  s.storage.set('token', 'new-token')
  await new Promise(resolve => setImmediate(resolve))
  assert.equal(s.requests[0].headers.Authorization, 'Bearer old-token')
  assert.ok(s.requests[0].body.includes('session.end'))
})
test('a late response cannot persist the old queue as belonging to the new user', async () => {
  const s = setup()
  const jwt = sub => `header.${Buffer.from(JSON.stringify({sub,jti:sub})).toString('base64url')}.signature`
  s.storage.set('token', jwt('old'))
  for (let i = 0; i < 60; i++) s.api.trackActivity('ui.click', `button:${i}`)
  s.api.endActivityContext()
  s.storage.set('token', jwt('new'))
  await new Promise(resolve => setImmediate(resolve))
  assert.equal(JSON.parse(s.session.values().next().value).owner, 'old::old')
})
