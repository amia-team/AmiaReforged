import { test } from 'node:test';
import assert from 'node:assert/strict';
import { EditorState } from '@codemirror/state';
import { CompletionContext } from '@codemirror/autocomplete';
import { glyph, glyphLanguage } from './glyph-language.js';
import { glyphCompletions, resolveFunctionAt } from './glyph-completion.js';
const scopes = [{ event: 'encounter.before_group_spawn', stage: null }];
const parameter = (name, type) => ({ name, displayName: name, type, required: true });
const method = (name, receiverType, returnType, parameters = [], typeParameters = []) => ({ name, receiverType, returnType,
  canonicalName: `language.${name}`, kind: 'Value', parameters, typeParameters, availableIn: scopes });
const metadata = { languageVersion: 6, events: [{ name: scopes[0].event, stages: [] }], contexts: [], functions: [],
  receiverMethods: [method('split', 'String', 'List<String>', [parameter('delimiter', 'String')]),
    method('length', 'String', 'Int'), method('contains', 'String', 'Bool', [parameter('fragment', 'String')]),
    method('union', 'List<T>', 'List<T>', [parameter('other', 'List<T>')], ['T']),
    method('contains', 'List<T>', 'Bool', [parameter('value', 'T')], ['T']),
    method('any', 'List<T>', 'Bool', [parameter('value', 'T')], ['T']),
    method('any', 'List<T>', 'Bool', [parameter('predicate', 'Fn<T, Bool>')], ['T']),
    method('iter', 'List<T>', 'Iterator<T>', [], ['T']),
    method('filter', 'Iterator<T>', 'Iterator<T>', [parameter('predicate', 'Fn<T, Bool>')], ['T']),
    method('map', 'Iterator<T>', 'Iterator<U>', [parameter('selector', 'Fn<T, U>')], ['T', 'U']),
    method('collect', 'Iterator<T>', 'List<T>', [], ['T']), method('count', 'List<T>', 'Int', [], ['T'])] };
function stateAt(body) {
  const marked = `glyph t : encounter.before_group_spawn { ${body} }`;
  const pos = marked.indexOf('§');
  return { pos, state: EditorState.create({ doc: marked.replace('§', ''), extensions: [glyph()] }) };
}
function completion(body) { const { state, pos } = stateAt(body); return glyphCompletions(metadata)(new CompletionContext(state, pos, true)); }
test('lambda pipelines parse, including nested predicates and boolean operators', () => {
  const source = `glyph t : encounter.before_group_spawn {
    let result = "a,,bb".split(",").iter().filter(|s| s.length() > 0 && s.contains("b")).map(|s| s.length()).collect()
    let nested = [[1], [2]].iter().filter(|list| list.any(|n| n == 1 || n == 2)).collect()
  }`;
  const errors = [];
  glyphLanguage.parser.parse(source).iterate({ enter(node) { if (node.type.isError) errors.push(source.slice(node.from, node.to)); } });
  assert.deepEqual(errors, []);
});
test('string and generic list receivers offer operations after chained calls', () => {
  assert.ok(completion('"a".§').options.some(o => o.label === 'split'));
  assert.ok(completion('"a".split(",").§').options.some(o => o.label === 'iter'));
  assert.ok(completion('[[1]].§').options.some(o => o.label === 'union'));
});
test('lambda parameters infer string receivers and do not leak out of scope', () => {
  assert.ok(completion('let list = ["a"] list.iter().filter(|part| part.§)').options.some(o => o.label === 'length'));
  const outside = completion('let list = ["a"].iter().filter(|part| part.length() > 0) §');
  assert.ok(!outside.options.some(o => o.label === 'part'));
});
test('type changing map chains infer the collected list and hover parameter types', () => {
  assert.ok(completion('["a"].iter().map(|part| part.length()).collect().§').options.some(o => o.label === 'contains'));
  const { state, pos } = stateAt('["a"].iter().map(|part| part.length()).collect().con§tains(2)');
  const found = resolveFunctionAt(state, pos, metadata);
  assert.equal(found.fn.receiverType, 'List<Int>');
  assert.equal(found.fn.parameters[0].type, 'Int');
});

test('membership and predicate any overloads both complete and predicate hover selects its callback', () => {
  const overloads = completion('[1].§').options.filter(o => o.label === 'any');
  assert.equal(overloads.length, 2);
  assert.ok(overloads.some(o => o.detail.includes('value: Int')));
  assert.ok(overloads.some(o => o.detail.includes('Fn<Int, Bool>')));
  const { state, pos } = stateAt('[1].an§y(|n| n > 0)');
  assert.equal(resolveFunctionAt(state, pos, metadata).fn.parameters[0].name, 'predicate');
});
