import { test } from 'node:test';
import assert from 'node:assert/strict';
import { EditorState } from '@codemirror/state';
import { CompletionContext } from '@codemirror/autocomplete';
import { glyph, glyphLanguage } from './glyph-language.js';
import { glyphCompletions } from './glyph-completion.js';
import { metadata } from './test-metadata.js';

function complete(marked) {
    const pos = marked.indexOf('|');
    const state = EditorState.create({ doc: marked.replace('|', ''), extensions: [glyph()] });
    return glyphCompletions(metadata)(new CompletionContext(state, pos, true))?.options || [];
}

test('statement function bodies, returns and expression bodies parse together', () => {
    for (const source of [
        'fn larger(a: Int, b: Int): Int { if a > b { return a } return b }',
        'fn stop(): Void { return; }',
        'fn stop(): Void { return }',
        'mod helpers { pub fn identity(x: Int): Int { var value = x return value } fn old(x: Int): Int = x }',
        'fn find(): Int { for i in 0..3 { if i == 1 { return i } } return 0 } glyph g : interaction { tick { let n = find() } }',
        'fn f(): Int { return nwn.get_tag(return: context.creature) }'
    ]) {
        const errors = [];
        glyphLanguage.parser.parse(source).iterate({ enter(node) { if (node.type.isError) errors.push([node.from, node.to]); } });
        assert.deepEqual(errors, [], source);
    }
});

test('function body completion includes parameters, scoped locals and return statements', () => {
    const options = complete('fn choose(value: Int): Int { var local = value if true { let inner = 2 | } let later = 3 } glyph g : interaction { tick {} }');
    const labels = options.map(o => o.label);
    for (const label of ['value', 'local', 'inner', 'return', 'var', 'if']) assert.ok(labels.includes(label), label);
    for (const label of ['later', 'tick', 'attempted', 'break', 'continue']) assert.ok(!labels.includes(label), label);
    assert.ok(complete('fn f(): Int { for i in 0..3 { | } return 0 }').some(o => o.label === 'continue'));
    assert.equal(complete('fn f(): Void { | }').find(o => o.label === 'return').apply, 'return;');
    assert.ok(!complete('fn f(value: Int): Int = |').some(o => o.label === 'return'));
    assert.ok(!complete('glyph g : interaction { tick { | } }').some(o => o.label === 'return'));
});

test('function declaration snippets are offered in scripts and modules', () => {
    assert.ok(complete('|').some(o => o.label === 'fn' && typeof o.apply === 'function'));
    assert.ok(complete('mod helpers { | }').some(o => o.label === 'fn' && typeof o.apply === 'function'));
});
