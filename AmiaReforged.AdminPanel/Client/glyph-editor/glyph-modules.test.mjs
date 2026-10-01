import { test } from 'node:test';
import assert from 'node:assert/strict';
import { EditorState } from '@codemirror/state';
import { CompletionContext } from '@codemirror/autocomplete';
import { glyphLanguage } from './glyph-language.js';
import { glyphCompletions, resolveFunctionAt } from './glyph-completion.js';
import { metadata as standard } from './test-metadata.js';

function state(source) { return EditorState.create({ doc: source, extensions: [glyphLanguage] }); }
function errors(source) {
  const result = []; glyphLanguage.parser.parse(source).iterate({ enter(node) { if (node.type.isError) result.push([node.from, node.to]); } }); return result;
}
const fn = { name: 'helpers.target', canonicalName: 'helpers.target', parameters: [{ name: 'actor', type: 'Object', required: true }], returnType: 'Object', kind: 'Value', description: 'Public declaration in helpers.', availableIn: [{ event: 'interaction', stage: 'attempted' }] };
const metadata = { ...standard, modules: ['helpers'], functions: [...standard.functions, fn, { ...fn, name: 'target' }],
  aggregates: [{ name: 'helpers.Item', fields: [{ name: 'actor', typeName: 'Object' }], variants: [] }] };
function complete(source) { const s = state(source); return glyphCompletions(metadata)(new CompletionContext(s, source.length, true)); }

test('modules, visibility, imports and qualified types parse without recovery', () => {
  assert.deepEqual(errors('mod helpers { using other const SECRET = "x" fn hidden(actor: Object): Object = actor pub fn target(actor: Object): Object = hidden(actor) pub struct Item { actor: Object, } pub type Result { Found { item: other.Item, }, Missing {}, } }'), []);
  assert.deepEqual(errors('using helpers glyph t : interaction { attempted { let result = helpers.Result.Missing() match result { helpers.Result.Found { item } {} helpers.Result.Missing {} {} } } }'), []);
});
test('using offers published module names and qualified calls replace only the member', () => {
  assert.ok(complete('using hel').options.some(o => o.label === 'helpers'));
  const result = complete('using helpers glyph t : interaction { attempted { helpers.tar');
  assert.ok(result.options.some(o => o.label === 'target'));
  assert.equal(result.from, 'using helpers glyph t : interaction { attempted { helpers.'.length);
});
test('module expression functions offer typed parameters and functions', () => {
  const result = complete('mod local { pub fn inspect(actor: Object): Object = tar');
  assert.ok(result.options.some(o => o.label === 'target'));
  assert.ok(result.options.some(o => o.label === 'actor' && o.type === 'Object'));
});
test('module functions resolve for hover with their canonical identity', () => {
  const source = 'using helpers glyph t : interaction { attempted { helpers.target(context.creature) } }';
  assert.equal(resolveFunctionAt(state(source), source.indexOf('target') + 2, metadata)?.canonical, 'helpers.target');
});
test('imported struct fields support completion through function return metadata', () => {
  const m = { ...metadata, functions: [...metadata.functions, { ...fn, name: 'helpers.Item', canonicalName: 'helpers.Item', returnType: 'helpers.Item' }] };
  const source = 'using helpers glyph t : interaction { attempted { let item = helpers.Item(context.creature) item.a';
  const result = glyphCompletions(m)(new CompletionContext(state(source), source.length, true));
  assert.ok(result.options.some(o => o.label === 'actor' && o.detail === 'Object'));
});
