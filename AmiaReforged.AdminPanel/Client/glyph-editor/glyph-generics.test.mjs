import { test } from 'node:test';
import assert from 'node:assert/strict';
import { EditorState } from '@codemirror/state';
import { CompletionContext } from '@codemirror/autocomplete';
import { glyph, glyphLanguage } from './glyph-language.js';
import { glyphCompletions, functionSnippet, resolveFunctionAt } from './glyph-completion.js';

const source = `
struct Box<T> { value: T, }
type Result<T, E> { Ok { value: T, }, Err { error: E, }, }
fn wrap<T>(value: T): Option<T> = Option<T>.Some(value: value)
impl<T> Box<T> { fn get(self): T = self.value }
glyph g : interaction { completed {
  let nested = Result<Box<Option<Int>>, String>.Ok(value: Box<Option<Int>>(value: Option<Int>.None()))
  match wrap(3) { Some { value } {} None {} {} }
  match Option<Box<Int>>.None() { Option<Box<Int>>.Some { value } {} None {} {} }
  let value = wrap<Int>(2)
  if 1 < 2 && 3 > 2 { }
  if 1 < 2 == true { }
} }`;
test('generic declarations, nested applications and comparisons parse', () => {
  const errors = [];
  glyphLanguage.parser.parse(source).iterate({ enter(n) { if (n.type.isError) errors.push(source.slice(n.from, n.to)); } });
  assert.deepEqual(errors, []);
});
const availableIn = [{ event: 'interaction', stage: 'completed' }];
const parameter = (name, type) => ({ name, type, required: true });
const fn = (name, returnType, parameters, extra = {}) => ({ name, canonicalName: name, returnType, parameters, typeParameters: ['T'], kind: 'Value', availableIn, ...extra });
const metadata = {
  languageVersion: 5, events: [{ name: 'interaction', stages: ['completed'] }], contexts: [],
  types: ['Option<T>', 'Box<T>'],
  constants: [{ name: 'OBJECT.INVALID', type: 'Object' }],
  functions: [fn('Option.Some', 'Option<T>', [parameter('value', 'T')], { declaringType: 'Option<T>' }),
    fn('Option.None', 'Option<T>', [], { declaringType: 'Option<T>' }), fn('Box', 'Box<T>', [parameter('value', 'T')]),
    fn('wrap', 'Option<T>', [parameter('value', 'T')])],
  aggregates: [{ name: 'Box', typeParameters: ['T'], fields: [{ name: 'value', typeName: 'T' }], variants: [] },
    { name: 'Option', typeParameters: ['T'], fields: [], variants: [{ name: 'Some', fields: [{ name: 'value', typeName: 'T' }] }, { name: 'None', fields: [] }] }],
  receiverMethods: [{ name: 'get', canonicalName: 'Box.get', receiverType: 'Box<T>', returnType: 'T', typeParameters: ['T'], parameters: [], availableIn },
    { name: 'is_valid', canonicalName: 'nwn.get_is_object_valid', receiverType: 'Object', returnType: 'Bool', parameters: [], availableIn }]
};
function state(marked) {
  return { state: EditorState.create({ doc: marked.replace('|', ''), extensions: [glyph()] }), pos: marked.indexOf('|') };
}
function complete(body) {
  const current = state(`glyph g : interaction { completed { ${body} } }`);
  return glyphCompletions(metadata)(new CompletionContext(current.state, current.pos, true));
}
test('constructor snippets put type parameters on the generic owner', () => {
  assert.equal(functionSnippet(metadata.functions[0]), 'Option<${T}>.Some(${value})');
  assert.equal(functionSnippet(metadata.functions[3]), 'wrap<${T}>(${value})');
});
test('applied variant completion specializes parameters', () => {
  const result = complete('let value = Option<Int>.|');
  assert.ok(result.options.some(o => o.label === 'Some' && o.detail === '(value: Int) → Option<Int>'));
  assert.ok(complete('let value = Option<Int>.Some(|)').options.some(o => o.label === 'value:' && o.detail.startsWith('Int')));
});
test('generic struct fields and receiver methods specialize from initializers', () => {
  const result = complete('let box = Box<Int>(value: 3) box.|');
  assert.ok(result.options.some(o => o.label === 'value' && o.detail === 'Int'));
  assert.ok(result.options.some(o => o.label === 'get' && o.detail.endsWith('Int')));
  const current = state('glyph g : interaction { completed { let box = Box<Int>(value: 3) box.ge|t() } }');
  assert.equal(resolveFunctionAt(current.state, current.pos, metadata).fn.returnType, 'Int');
});
test('Some pattern bindings expose specialized object receiver methods', () => {
  assert.ok(complete('match wrap(OBJECT.INVALID) { Some { value } { value.| } None {} {} }').options.some(o => o.label === 'is_valid'));
  assert.ok(complete('match Option<Object>.Some(value: unknown_object) { Some { value } { value.| } None {} {} }').options.some(o => o.label === 'is_valid'));
});

test('hover specializes explicit and inferred generic functions', () => {
  for (const expression of ['wr|ap(3)', 'wr|ap<Int>(3)']) {
    const current = state(`glyph g : interaction { completed { let x = ${expression} } }`);
    assert.equal(resolveFunctionAt(current.state, current.pos, metadata).fn.returnType, 'Option<Int>');
  }
});
