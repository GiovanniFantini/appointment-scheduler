import { test } from 'node:test'
import assert from 'node:assert/strict'
import fs from 'node:fs/promises'
import path from 'node:path'
import { pathToFileURL } from 'node:url'
import { build } from 'esbuild'
import { JSDOM } from 'jsdom'
import React, { act } from 'react'
import { createRoot } from 'react-dom/client'

const output = path.resolve(import.meta.dirname, '../node_modules/.cache/employee-online-test.mjs')
await build({ entryPoints: [path.resolve(import.meta.dirname, '../employee-app/src/components/OnlineOnly.tsx')], outfile: output, bundle: true, platform: 'node', format: 'esm', jsx: 'automatic', external: ['react', 'react-dom', 'react/jsx-runtime'], loader: { '.css': 'empty' } })
const { OnlineOnly } = await import(pathToFileURL(output).href)
await fs.unlink(output)

async function fixture(t, initialResponse) {
  const dom = new JSDOM('<div id="root"></div>', { url: 'https://employee.example.test' })
  const w = dom.window
  for (const key of ['window', 'document', 'HTMLElement', 'Element', 'Node']) globalThis[key] = key === 'window' ? w : w[key]
  globalThis.IS_REACT_ACT_ENVIRONMENT = true
  w.HTMLDialogElement.prototype.showModal = function () { this.open = true }
  w.HTMLDialogElement.prototype.close = function () { this.open = false }
  let response = initialResponse
  const calls = []
  const originalFetch = globalThis.fetch
  globalThis.fetch = async (url, options) => {
    calls.push({ url, options })
    return response()
  }
  const root = createRoot(document.querySelector('#root'))
  await act(async () => root.render(React.createElement(OnlineOnly, null, React.createElement('input', { defaultValue: 'Richiesta in compilazione' }))))
  t.after(async () => {
    await act(async () => root.unmount())
    dom.window.close()
    globalThis.fetch = originalFetch
  })
  return {
    w, calls,
    setResponse: next => { response = next },
    event: async name => act(async () => w.dispatchEvent(new w.Event(name))),
    retry: async () => act(async () => document.querySelector('button').click()),
  }
}

const healthy = () => ({ ok: true, json: async () => ({ app: 'employee-app', status: 'healthy' }) })
const unavailable = () => { throw new TypeError('Failed to fetch') }

test('offline startup blocks the app; retry recovers without caching the check', async t => {
  const f = await fixture(t, unavailable)
  assert.equal(document.querySelector('input'), null)
  assert.equal(document.querySelector('dialog').open, true)
  assert.match(document.body.textContent, /Connessione non disponibile/)
  f.setResponse(healthy)
  await f.retry()
  assert.ok(document.querySelector('input'))
  assert.equal(document.querySelector('dialog'), null)
  assert.ok(f.calls.every(call => call.options.cache === 'no-store'))
})

test('disconnect blocks interaction and reconnect preserves the form', async t => {
  const f = await fixture(t, healthy)
  const input = document.querySelector('input')
  input.value = 'Modifica non ancora inviata'
  await f.event('offline')
  assert.equal(input.parentElement.hidden, true)
  assert.equal(document.querySelector('dialog').open, true)
  await f.event('online')
  assert.equal(document.querySelector('input'), input)
  assert.equal(input.value, 'Modifica non ancora inviata')
  assert.equal(input.parentElement.hidden, false)
  assert.equal(document.querySelector('dialog'), null)
})

test('a Wi-Fi login page or server failure does not unlock the app', async t => {
  const f = await fixture(t, () => ({ ok: true, json: async () => { throw new SyntaxError('HTML instead of JSON') } }))
  assert.equal(document.querySelector('input'), null)
  f.setResponse(() => ({ ok: false }))
  await f.retry()
  assert.equal(document.querySelector('input'), null)
  assert.equal(document.querySelector('dialog').open, true)
})

test('a stale connectivity response cannot unlock the app after disconnection', async t => {
  let complete
  const f = await fixture(t, () => new Promise(resolve => { complete = resolve }))
  await f.event('offline')
  await act(async () => complete(healthy()))
  assert.equal(document.querySelector('input'), null)
  assert.equal(document.querySelector('dialog').open, true)
})
