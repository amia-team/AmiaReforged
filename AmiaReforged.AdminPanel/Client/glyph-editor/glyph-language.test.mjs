import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readdir, readFile } from 'node:fs/promises';
import { EditorState } from '@codemirror/state';
import { syntaxTree } from '@codemirror/language';
import { highlightTree } from '@lezer/highlight';
import { glyph, glyphLanguage, glyphHighlightStyle } from './glyph-language.js';

const languageRoot = new URL('../../../AmiaReforged.PwEngine/Features/Glyph/Language/', import.meta.url);
const corpus = new URL('Tests/Corpus/', languageRoot);
function nodes(tree, name) {
    const result = [];
    tree.iterate({ enter(node) { if (node.name === name) result.push(node.node); } });
    return result;
}
function parse(source) { return glyphLanguage.parser.parse(source); }
function errors(tree) {
    const result = [];
    tree.iterate({ enter(node) { if (node.type.isError) result.push([node.from, node.to]); } });
    return result;
}
function highlights(source) {
    const result = [];
    highlightTree(parse(source), glyphHighlightStyle, (from, to, style) =>
        result.push({ text: source.slice(from, to), style }));
    return result;
}

for (const file of (await readdir(corpus)).filter(file => file.endsWith('.glyph'))) {
    test(`compiler corpus parses without recovery: ${file}`, async () => {
        const source = await readFile(new URL(file, corpus), 'utf8');
        const tree = parse(source);
        assert.deepEqual(errors(tree), [], tree.toString());
        if (source.includes('fail "')) assert.ok(nodes(tree, 'FailStatement').length > 0);
    });
}

test('keyword specializations stay aligned with GlyphLexer', async () => {
    const lexer = await readFile(new URL('Parsing/GlyphLexer.cs', languageRoot), 'utf8');
    const keywords = lexer.match(/kind = word is ([\s\S]*?)\? word/)[1].match(/"[a-z]+"/g).map(word => word.slice(1, -1));
    for (const keyword of keywords)
        assert.ok(glyphLanguage.parser.nodeSet.types.some(type => type.name === keyword), keyword);
    const source = 'glyph glyph_test : interaction { let started_value = true if started_value { break } }';
    assert.deepEqual(errors(parse(source)), []);
});

test('typed namespaced constants and NWN value helpers parse without recovery', () => {
    const source = `const Custom.MASK : Int = -2147483648
        const Custom.NONE : Object = 2130706432
        fn kind(aura: Effect): Int = nwn.get_effect_type(aura)
        glyph test : interaction { completed {
            player.set_local_int("mask", Custom.MASK)
            let location = player.get_location()
            player.jump_to_location(location)
        } }`;
    const tree = parse(source);
    assert.deepEqual(errors(tree), [], tree.toString());
    assert.equal(nodes(tree, 'ConstantDeclaration').length, 2);
});

test('expressions preserve precedence, calls, named arguments and shorthand', () => {
    const source = `glyph résumé : interaction {
        let α2 = -1 + 2 * 3
        if α2 >= 0 && true || false { fail "No" } else if !false { fail("No") }
        foreach member in party.members { { message(member, "Hi",); break; } }
        metadata["quality"] = "good"
        progress -= 1.5
        skill_check(player, "search", dc: 15)
    }`;
    const tree = parse(source);
    assert.deepEqual(errors(tree), [], tree.toString());
    assert.equal(nodes(tree, 'FailStatement').length, 1);
    assert.equal(nodes(tree, 'ArgumentName').length, 1);
    const value = nodes(tree, 'LetStatement')[0].lastChild;
    assert.equal(value.name, 'BinaryExpression');
    assert.equal(value.firstChild.name, 'UnaryExpression');
    assert.equal(value.lastChild.name, 'BinaryExpression');
    assert.equal(source.slice(value.lastChild.from, value.lastChild.to), '2 * 3');
});

test('colors distinguish keywords, strings, comments, calls and named arguments', () => {
    const source = 'glyph test : interaction { tick { // comment\n if true { skill_check(player, "text", dc: 15) progress += 1 } } }';
    const spans = highlights(source);
    for (const [text, style] of [
        ['glyph', 'glyph-keyword'], ['test', 'glyph-definition'], ['interaction', 'glyph-event'],
        ['tick', 'glyph-keyword'], ['if', 'glyph-keyword'], ['true', 'glyph-literal'],
        ['// comment', 'glyph-comment'], ['skill_check', 'glyph-function'],
        ['"text"', 'glyph-string'], ['dc', 'glyph-property'], ['15', 'glyph-literal'],
        ['+=', 'glyph-operator']
    ]) assert.ok(spans.some(span => span.text === text && span.style === style), `${text}: ${JSON.stringify(spans)}`);
    assert.ok(highlights('glyph g : interaction { spawn.cancel() fail "no" }')
        .some(span => span.text === 'cancel' && span.style === 'glyph-function'));
});

test('strings and comments keep embedded syntax inert and recover at a newline', () => {
    const source = 'glyph g : interaction {\nmessage(player, "escaped \\" quote // text")\n// if true {\nmessage(player, "unfinished\nprogress += 1\n}';
    const tree = parse(source);
    assert.equal(nodes(tree, 'String').length, 1);
    assert.equal(nodes(tree, 'UnterminatedString').length, 1);
    assert.equal(nodes(tree, 'LineComment').length, 1);
    assert.equal(nodes(tree, 'AssignmentStatement').length, 1);
    assert.ok(highlights(source).some(span => span.text === '"unfinished' && span.style === 'glyph-string'));
});

test('unfinished member access, calls, arguments and blocks retain useful syntax nodes', () => {
    for (const [body, expected] of [
        ['player.', 'MemberExpression'], ['skill_check(', 'CallExpression'],
        ['skill_check(player, dc:', 'ArgumentName'], ['if true {', 'IfStatement'],
        ['foreach member in party.members {', 'ForeachStatement']
    ]) {
        const source = `glyph g : interaction { tick { ${body}`;
        const tree = parse(source);
        assert.equal(tree.length, source.length);
        assert.ok(nodes(tree, expected).length > 0, tree.toString());
        assert.ok(errors(tree).length > 0);
    }
});

test('incremental editing recovers from an incomplete program without losing syntax', () => {
    const prefix = 'glyph g : interaction { tick { message(player, ';
    let state = EditorState.create({ doc: prefix, extensions: [glyph()] });
    assert.ok(nodes(syntaxTree(state), 'CallExpression').length > 0);
    state = state.update({ changes: { from: prefix.length, insert: '"hello") } }' } }).state;
    assert.deepEqual(errors(syntaxTree(state)), []);
    assert.equal(nodes(syntaxTree(state), 'String').length, 1);
});
test('keyword-shaped words are accepted as named argument labels', () => {
    // A parameter name that is also a Glyph keyword (here `type`) must still parse as a
    // named argument label, with no parse-recovery errors, exactly as the C# parser accepts it.
    const source = 'glyph t : interaction { tick { let door = player.get_nearest_object_by_type(type: "door") } }';
    const tree = parse(source);
    assert.deepEqual(errors(tree), [], tree.toString());
    const labels = nodes(tree, 'ArgumentName');
    assert.equal(labels.length, 1);
    assert.equal(source.slice(labels[0].from, labels[0].to), 'type');
    // Both a keyword-shaped and a plain label work together, and keyword-as-value stays reserved.
    const both = parse('glyph t : interaction { tick { let x = player.get_nearest_object_by_type(origin: creature, type: "door") } }');
    assert.deepEqual(errors(both), [], both.toString());
    assert.equal(nodes(both, 'ArgumentName').length, 2);
    assert.ok(nodes(parse('glyph g : interaction { tick { type } }'), 'ArgumentName').length === 0);
});
