import { readdirSync, readFileSync, writeFileSync } from 'node:fs'
import { join, relative } from 'node:path'
import ts from 'typescript'

const write = process.argv.includes('--write')
let missing = 0
const roots = ['admin-app', 'merchant-app', 'employee-app', 'shared-ui']
const tags = new Set(['button', 'a', 'input', 'select', 'textarea', 'form', 'summary', 'Button', 'IconButton', 'Input', 'Select', 'Textarea', 'Checkbox'])
function visitFile(file, root) {
  const content = readFileSync(file, 'utf8')
  const ast = ts.createSourceFile(file, content, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX)
  const edits = []
  let index = 0
  const prefix = `${root.replace('-app', '')}.${relative(join(root, 'src'), file).replace(/\.tsx$/, '').replaceAll(/[\\/]/g, '.')}`
  const ids = new Set([...content.matchAll(/data-activity="([^"]+)"/g)].map(match => match[1]))
  function visit(node) {
    if (ts.isJsxOpeningElement(node) || ts.isJsxSelfClosingElement(node)) {
      const names = node.attributes.properties.filter(ts.isJsxAttribute).map(a => a.name.getText(ast))
      if ((tags.has(node.tagName.getText(ast)) || names.includes('onClick')) && !names.includes('data-activity')) {
        let id
        do { id = `${prefix}.${++index}` } while (ids.has(id))
        ids.add(id)
        edits.push({ position: node.tagName.end, text: ` data-activity="${id}"` })
      }
    }
    ts.forEachChild(node, visit)
  }
  visit(ast)
  if (edits.length) {
    missing += edits.length
    if (write) {
      let result = content
      for (const edit of edits.reverse()) result = result.slice(0, edit.position) + edit.text + result.slice(edit.position)
      writeFileSync(file, result)
    } else console.error(`${file}: ${edits.length} controlli senza identificativo attività`)
  }
}
function walk(directory, root) {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const path = join(directory, entry.name)
    if (entry.isDirectory()) walk(path, root)
    else if (entry.name.endsWith('.tsx')) visitFile(path, root)
  }
}
for (const root of roots) walk(join(root, 'src'), root)
if (missing && !write) process.exitCode = 1
else console.log(write ? `${missing} identificativi aggiunti.` : 'Copertura identificativi controlli verificata.')
