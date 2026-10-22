import { test } from 'node:test'
import assert from 'node:assert/strict'
import fs from 'node:fs'
import path from 'node:path'
import ts from 'typescript'

const root = path.resolve(import.meta.dirname, '..')
const source = fs.readFileSync(path.join(root, 'shared-ui/src/guides/types.ts'), 'utf8')
const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ES2022, target: ts.ScriptTarget.ES2022 } }).outputText
const { availableGuideTours, guideStorageKey } = await import(`data:text/javascript;base64,${Buffer.from(compiled).toString('base64')}`)
const catalogue = []
for (const app of ['merchant', 'employee']) {
  for (const dir of fs.readdirSync(path.join(root, `${app}-app/src/pages`))) {
    const file = path.join(root, `${app}-app/src/pages`, dir, `${dir}.guide.ts`)
    if (fs.existsSync(file)) {
      const data = JSON.parse(fs.readFileSync(file, 'utf8').split('export default ')[1].split(' satisfies PageGuideDefinition')[0])
      catalogue.push({ app, data })
    }
  }
}
const features = ['Calendario', 'Richieste', 'Risorse', 'Ruoli', 'Documenti', 'Report', 'Mansioni', 'Filiali', 'Timbratura', 'Magazzino']
const access = level => ({ activeFeatures: features, featureLevels: Object.fromEntries(features.map(feature => [feature, level])) })

test('base employees never see planning or management actions', () => {
  const find = id => catalogue.find(item => item.app === 'employee' && item.data.id === id).data
  assert.deepEqual(availableGuideTours(find('requests'), access('ReadOnly')).map(t => t.id), ['overview', 'create'])
  assert.deepEqual(availableGuideTours(find('inventory'), access('ReadOnly')).map(t => t.id), ['overview', 'items'])
  assert.deepEqual(availableGuideTours(find('inventory'), access('Operator')).map(t => t.id), ['overview', 'items', 'adjust', 'receive'])
  assert.equal(availableGuideTours(find('inventory'), access('Manager')).length, 7)
  assert.equal(availableGuideTours(find('planning'), access('ReadOnly')).length, 0)
  assert.equal(availableGuideTours(find('clockManagement'), access('Operator')).length, 0)
  assert.deepEqual(availableGuideTours(find('documents'), access('Operator')).map(t => t.id), ['overview', 'upload', 'versions', 'acknowledge'])
})

test('revoked features and absent levels cannot expose management paths', () => {
  for (const {data} of catalogue) {
    if (data.feature) assert.equal(availableGuideTours(data, { activeFeatures: [] }).length, 0)
    const allowed = availableGuideTours(data, { activeFeatures: features })
    assert.ok(allowed.every(tour => !tour.minLevel))
  }
})

test('absence management and clock justifications follow independent permissions', () => {
  const page = catalogue.find(item => item.data.id === 'requests').data
  const tours = (requests, clock) => availableGuideTours(page, { activeFeatures: ['Richieste', 'Timbratura'], featureLevels: { Richieste: requests, Timbratura: clock } }).map(t => t.id)
  assert.deepEqual(tours('ReadOnly', 'Manager'), ['overview', 'create', 'justifications'])
  assert.deepEqual(tours('Operator', 'ReadOnly'), ['overview', 'create', 'manage'])
})

test('every protected business page has a guide and valid local targets', () => {
  assert.equal(catalogue.filter(item => item.app === 'merchant').length, 3)
  assert.equal(catalogue.filter(item => item.app === 'employee').length, 12)
  const sourceFiles = []
  function walk(dir) {
    for (const item of fs.readdirSync(dir, { withFileTypes: true })) {
      if (item.isDirectory()) walk(path.join(dir, item.name))
      else if (item.name.endsWith('.tsx')) sourceFiles.push(fs.readFileSync(path.join(dir, item.name), 'utf8'))
    }
  }
  for (const app of ['employee-app', 'merchant-app', 'shared-ui']) walk(path.join(root, app, 'src'))
  const allSource = sourceFiles.join('\n')
  for (const {data} of catalogue) {
    assert.ok(data.version >= 1)
    assert.equal(new Set(data.tours.map(t => t.id)).size, data.tours.length)
    for (const tour of data.tours) {
      assert.ok(tour.steps.length >= 2 && tour.steps.length <= 5)
      assert.ok(!tour.steps[0].optional, `${data.id}/${tour.id} needs a stable entry point`)
      for (const step of tour.steps) {
        for (const selector of [step.target, step.nextWhen, tour.requiresTarget, tour.unavailableWhen].filter(Boolean)) {
          const id = selector.match(/^\[data-guide="([\w.]+)"\](?::not\(:disabled\))?$/)?.[1]
          assert.ok(id, `${data.id}: invalid target ${selector}`)
          assert.ok(allSource.includes(`data-guide="${id}"`) || id.startsWith('inventory.tab.') && allSource.includes('data-guide={`inventory.tab.${tab.key}`}'), `${data.id}: missing anchor ${id}`)
        }
        assert.ok(!/[<>\u2014]/.test(step.title + step.text), 'plain text without HTML or em dashes')
      }
    }
  }
})

test('history is isolated by account, company, application and content version', () => {
  const page = catalogue[0].data
  const keys = [guideStorageKey('merchant', 1, 1, page), guideStorageKey('employee', 1, 1, page), guideStorageKey('merchant', 2, 1, page), guideStorageKey('merchant', 1, 2, page), guideStorageKey('merchant', 1, 1, { ...page, version: page.version + 1 })]
  assert.equal(new Set(keys).size, keys.length)
})
