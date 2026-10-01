import { test } from 'node:test';
import assert from 'node:assert/strict';
import { EditorState } from '@codemirror/state';
import { CompletionContext } from '@codemirror/autocomplete';
import { highlightTree } from '@lezer/highlight';
import { glyph, glyphLanguage, glyphHighlightStyle } from './glyph-language.js';
import { glyphCompletions, resolveFunctionAt } from './glyph-completion.js';

const source = `
mod items {
  pub struct Item { names: List<String>, scores: Dictionary<String, Int>, }
  impl Item {
    pub fn description(self): String = self.names[0]
    fn create(): Self { return Item(names: ["a", "b"], scores: Dictionary<String, Int>()) }
  }
  pub type Outcome { Found { values: List<Object>, }, Missing {}, }
  impl Outcome { pub fn found(self): Bool { match self { Found { values } { return true } Missing {} { return false } } } }
}`;
test('Glyph 4 collections and impl parse and highlight', () => {
  const errors = [];
  const tree = glyphLanguage.parser.parse(source);
  tree.iterate({ enter(node) { if (node.type.isError) errors.push(source.slice(node.from, node.to)); } });
  assert.deepEqual(errors, []);
  const highlights = [];
  highlightTree(tree, glyphHighlightStyle, (from, to, style) => highlights.push({ text: source.slice(from, to), style }));
  assert.ok(highlights.some(h => h.text === 'impl' && h.style === 'glyph-keyword'));
  assert.ok(highlights.some(h => h.text === 'List' && h.style.split(' ').includes('glyph-event')));
});
const scope = [{ event: 'encounter.before_group_spawn', stage: null }];
const method = (name, receiverType, returnType, parameters = []) => ({ name, receiverType, returnType, parameters, kind: 'Value', canonicalName: `${receiverType}.${name}`, availableIn: scope });
const metadata = {
  languageVersion: 4, events: [{ name: 'encounter.before_group_spawn', stages: [] }], contexts: [], functions: [],
  receiverMethods: [method('append', 'List<Int>', 'List<Int>', [{ name: 'value', type: 'Int', required: true }]), method('count', 'List<Int>', 'Int'),
    method('with', 'Dictionary<String, Int>', 'Dictionary<String, Int>', [{ name: 'key', type: 'String', required: true }, { name: 'value', type: 'Int', required: true }]),
    method('count', 'Dictionary<String, Int>', 'Int'), method('amount', 'Item', 'Int')]
};
function completion(body, prelude = '') {
  const marked = `${prelude} glyph t : encounter.before_group_spawn { ${body} }`;
  const pos = marked.indexOf('|');
  const state = EditorState.create({ doc: marked.replace('|', ''), extensions: [glyph()] });
  return glyphCompletions(metadata)(new CompletionContext(state, pos, true));
}
test('completion infers list literals, typed constructors and chained results', () => {
  assert.ok(completion('let list = [1, 2] list.|').options.some(o => o.label === 'append'));
  assert.ok(completion('let list = List<Int>() list.append(1).|').options.some(o => o.label === 'count'));
  assert.ok(completion('let d = Dictionary<String,Int>() d.|').options.some(o => o.label === 'with'));
  assert.ok(completion('let d = Dictionary<String, Int>() d.with("a", 1).|').options.some(o => o.label === 'count'));
});
test('completion supplies named collection parameters and self member methods', () => {
  assert.ok(completion('let list = [1] list.append(|)').options.some(o => o.label === 'value:'));
  const marked = 'struct Item {} impl Item { fn double(self): Int { return self.| } }';
  const pos = marked.indexOf('|');
  const state = EditorState.create({ doc: marked.replace('|', ''), extensions: [glyph()] });
  assert.ok(glyphCompletions(metadata)(new CompletionContext(state, pos, true)).options.some(o => o.label === 'amount'));
});
test('hover resolves immutable collection methods from receiver metadata', () => {
  const marked = 'glyph t : encounter.before_group_spawn { let list = [1] list.ap|pend(2) }';
  const state = EditorState.create({ doc: marked.replace('|', ''), extensions: [glyph()] });
  const found = resolveFunctionAt(state, marked.indexOf('|'), metadata);
  assert.equal(found.fn.returnType, 'List<Int>');
  assert.equal(found.fn.parameters[0].name, 'value');
});
