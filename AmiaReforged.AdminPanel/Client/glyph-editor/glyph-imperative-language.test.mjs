import { test } from 'node:test';
import assert from 'node:assert/strict';
import { highlightTree } from '@lezer/highlight';
import { glyphLanguage, glyphHighlightStyle } from './glyph-language.js';

function parse(source) {
    const tree = glyphLanguage.parser.parse(source);
    const errors = [];
    tree.iterate({ enter(node) { if (node.type.isError) errors.push([node.from, node.to]); } });
    assert.deepEqual(errors, [], tree.toString());
    return tree;
}

test('imperative control flow, range boundaries, compound assignments and match patterns parse', () => {
    const tree = parse(`
        type Result { Found { target: Object } Missing { reason: String } }
        glyph imperative : interaction { completed {
            var i = 0
            var distance = 1.5
            while i < 10 && player.is_valid() {
                i += 1
                if i == 2 { continue } else { distance *= 2.5 }
                if i > 5 { break }
            }
            for item in player.inventory() { item.destroy() }
            foreach item in player.inventory() { continue }
            for n in 1..10 { i = n }
            for n in 1..=10 step 2 { i -= n }
            for n in 10..0 step -1 { i /= 2 }
            match i { 0 {} -1 {} OBJECT_TYPE.CREATURE {} _ {} }
            match true { true {} false {} }
            match "foo" { "foo" {} _ {} }
            match result { Found { target } { target.destroy() } Missing { reason } {} }
        } }
    `);
    for (const name of ['VarStatement', 'WhileStatement', 'ForStatement', 'ForeachStatement', 'ContinueStatement', 'RangeOperator', 'ValuePattern', 'VariantPattern', 'WildcardPattern'])
        assert.ok(tree.toString().includes(name), name);
});

test('new keywords, range operators and wildcard are highlighted', () => {
    const source = 'glyph g : interaction { tick { var i = 0 while true { for n in 0..=5 step 2 { continue } break } match i { _ {} } } }';
    const spans = [];
    highlightTree(parse(source), glyphHighlightStyle, (from, to, style) => spans.push({ text: source.slice(from, to), style }));
    for (const text of ['var', 'while', 'for', 'step', 'continue', '_'])
        assert.ok(spans.some(span => span.text === text && span.style === 'glyph-keyword'), text);
    assert.ok(spans.some(span => span.text === '..=' && span.style === 'glyph-operator'));
});

test('range tokenization leaves floating literals intact', () => {
    const source = 'glyph g : interaction { tick { var f = 1.5 for i in 1..5 {} for i in 1..=5 {} } }';
    const numbers = [];
    parse(source).iterate({ enter(node) { if (node.name === 'Number') numbers.push(source.slice(node.from, node.to)); } });
    assert.deepEqual(numbers, ['1.5', '1', '5', '1', '5']);
});
