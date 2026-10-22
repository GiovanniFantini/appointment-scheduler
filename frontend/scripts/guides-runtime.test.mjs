import { test } from 'node:test'
import assert from 'node:assert/strict'
import fs from 'node:fs/promises'
import path from 'node:path'
import { pathToFileURL } from 'node:url'
import { build } from 'esbuild'
import { JSDOM } from 'jsdom'
import React, { act } from 'react'
import { createRoot } from 'react-dom/client'

const output = path.resolve(import.meta.dirname, '../node_modules/.cache/guides-test.mjs')
await build({ entryPoints: [path.resolve(import.meta.dirname, '../shared-ui/src/guides/PageGuide.tsx')], outfile: output, bundle: true, platform: 'node', format: 'esm', jsx: 'automatic', external: ['react', 'react-dom', 'react/jsx-runtime'], loader: { '.css': 'empty' } })
const { PageGuide } = await import(pathToFileURL(output).href)
await fs.unlink(output)
const pause = ms => new Promise(resolve => setTimeout(resolve, ms))

async function fixture(t, tour, { animate = false } = {}) {
  const dom = new JSDOM('<div class="su-shell"><main><button data-guide="entry">Apri</button><div id="mount"></div></main></div>', { url: 'http://localhost', pretendToBeVisual: true })
  const w = dom.window
  for (const key of ['window', 'document', 'HTMLElement', 'Element', 'Node', 'MutationObserver', 'localStorage']) globalThis[key] = key === 'window' ? w : w[key]
  globalThis.getComputedStyle = w.getComputedStyle.bind(w)
  globalThis.requestAnimationFrame = w.requestAnimationFrame.bind(w)
  globalThis.cancelAnimationFrame = w.cancelAnimationFrame.bind(w)
  globalThis.IS_REACT_ACT_ENVIRONMENT = true
  w.matchMedia = () => ({ matches: !animate })
  w.HTMLElement.prototype.scrollIntoView = () => {}
  w.HTMLElement.prototype.getClientRects = function () { return this.isConnected && this.style.display !== 'none' ? [this.getBoundingClientRect()] : [] }
  w.HTMLElement.prototype.getBoundingClientRect = () => ({ top: 100, left: 100, right: 220, bottom: 140, width: 120, height: 40, x: 100, y: 100 })
  const root = createRoot(document.querySelector('#mount'))
  const page = { id: 'fixture', title: 'Pagina', version: 1, tours: [{ id: 'flow', title: 'Percorso', description: 'Prova', ...tour }] }
  await act(async () => root.render(React.createElement(PageGuide, { app: 'employee', page, user: { userId: 1, activeFeatures: [] } })))
  const click = async selector => {
    assert.ok(document.querySelector(selector), selector)
    await act(async () => document.querySelector(selector).click())
  }
  const settle = async (ms = 60) => act(async () => pause(ms))
  const start = async () => { await click('[data-activity="guide.open"]'); await click('[data-activity="guide.start"]') }
  let mounted = true
  const unmount = async () => { if (mounted) { await act(async () => root.unmount()); mounted = false } }
  t.after(async () => { await unmount(); await pause(30); dom.window.close() })
  return { click, settle, start, unmount, w }
}
const simple = { steps: [{ target: '[data-guide="entry"]', title: 'Inizio', text: 'Test' }] }

for (const exit of ['escape', 'close', 'overview', 'unmount']) {
  test(`early animated ${exit} always removes the Escape capture listener`, async t => {
    const f = await fixture(t, simple, { animate: true })
    await f.start()
    if (exit === 'escape') await act(async () => document.dispatchEvent(new f.w.KeyboardEvent('keydown', { key: 'Escape', bubbles: true })))
    else if (exit === 'close') await f.click('[data-activity="guide.close"]')
    else if (exit === 'overview') { await f.click('[data-activity="guide.overview"]'); await f.click('[data-activity="guide.catalogue.close"]') }
    else await f.unmount()
    let received = false
    document.addEventListener('keydown', () => { received = true }, { once: true })
    document.dispatchEvent(new f.w.KeyboardEvent('keydown', { key: 'Escape', bubbles: true }))
    assert.equal(received, true)
    assert.equal(document.querySelector('.driver-overlay'), null)
    await f.settle(450)
    assert.equal(document.querySelector('.driver-overlay'), null)
  })
}

test('Back stays on the opener even when its form remains visible', async t => {
  const f = await fixture(t, { steps: [{ target: '[data-guide="entry"]', nextWhen: '[data-guide="form"]', title: 'Inizio', text: 'Apri' }, { target: '[data-guide="form"]', title: 'Modulo', text: 'Compila' }] })
  document.querySelector('[data-guide="entry"]').onclick = () => { const el = document.createElement('div'); el.dataset.guide = 'form'; document.body.append(el) }
  await f.start()
  await f.click('[data-guide="entry"]')
  await f.settle()
  assert.equal(document.querySelector('.driver-popover-title').textContent, 'Modulo')
  await f.click('[data-activity="guide.previous"]')
  await f.settle()
  assert.equal(document.querySelector('.driver-popover-title').textContent, 'Inizio')
  await f.click('[data-activity="guide.next"]')
  await f.settle()
  assert.equal(document.querySelector('.driver-popover-title').textContent, 'Modulo')
})

test('missing prerequisite explains the unavailable tour without marking it seen', async t => {
  const f = await fixture(t, { ...simple, requiresTarget: '[data-guide="missing"]', unavailableMessage: 'Manca un documento' })
  await f.start()
  assert.equal(document.querySelector('[role="status"]').textContent, 'Manca un documento')
  assert.equal(document.querySelector('.driver-overlay'), null)
  assert.ok(!document.body.textContent.includes('Già visto'))
})

test('a prerequisite error appearing after an action exits the waiting step', async t => {
  const f = await fixture(t, { unavailableWhen: '[data-guide="noBranches"]', unavailableMessage: 'Nessuna filiale', steps: [{ target: '[data-guide="entry"]', nextWhen: '[data-guide="form"]', title: 'Apri', text: 'Apri' }] })
  document.querySelector('[data-guide="entry"]').onclick = () => { const el = document.createElement('p'); el.dataset.guide = 'noBranches'; document.body.append(el) }
  await f.start()
  await f.click('[data-guide="entry"]')
  await f.settle()
  assert.equal(document.querySelector('[role="status"]').textContent, 'Nessuna filiale')
  assert.equal(document.querySelector('.driver-overlay'), null)
  assert.ok(!document.body.textContent.includes('Già visto'))
})

test('waiting expires only after the user tries the indicated control', async t => {
  const f = await fixture(t, { steps: [{ target: '[data-guide="entry"]', nextWhen: '[data-guide="missing"]', title: 'Apri', text: 'Apri' }] })
  const originalTimeout = f.w.setTimeout.bind(f.w)
  f.w.setTimeout = (callback, ms, ...args) => originalTimeout(callback, ms === 8000 ? 30 : ms, ...args)
  await f.start()
  await f.settle(80)
  assert.ok(document.querySelector('.driver-overlay'))
  await f.click('[data-guide="entry"]')
  await f.settle(80)
  assert.match(document.querySelector('[role="status"]').textContent, /controllo atteso non è disponibile/)
  assert.equal(document.querySelector('.driver-overlay'), null)
  assert.ok(!document.body.textContent.includes('Già visto'))
})
