import { test } from 'node:test';
import assert from 'node:assert/strict';
import { glyphLanguage } from './glyph-language.js';

function parse(source) { return glyphLanguage.parser.parse(source); }
function errors(tree) {
    const result = [];
    tree.iterate({ enter(node) { if (node.type.isError) result.push([node.from, node.to]); } });
    return result;
}
function nodes(tree, name) {
    const result = [];
    tree.iterate({ enter(node) { if (node.name === name) result.push(node.node); } });
    return result;
}

// The prelude forms must parse as standalone source with zero recovery, exactly like the C#
// lexer/parser accept them. None of these require a `glyph name : event { ... }` body.
test('standalone global declarations parse with zero errors', () => {
    const sources = [
        'const OBJECT_DOOR = "door"',
        'struct Result { target: Object, }',
        'type LookupResult { Found { target: Object, }, }',
        'fn nearest(origin: Object, kind: String): Object = nwn.nearest_object_by_kind(origin, kind)',
        'const OBJECT_TRIGGER = "trigger";\n' +
            'fn nearest(o: Object, k: String): Object = Object;\n' +
            'struct Result { target: Object, }',
    ];
    for (const source of sources) {
        assert.deepEqual(errors(parse(source)), [], source);
    }
});

test('prelude declarations keep their declaration node shapes', () => {
    assert.equal(nodes(parse('const OBJECT_DOOR = "door"'), 'ConstantDeclaration').length, 1);
    assert.equal(nodes(parse('fn nearest(o: Object): Object = o'), 'FunctionDeclaration').length, 1);
    assert.equal(nodes(parse('struct Result { target: Object, }'), 'StructDeclaration').length, 1);
    assert.equal(nodes(parse('type LookupResult { Found { target: Object, }, }'), 'AdtDeclaration').length, 1);
});

test('const initializer and function body parse as full expressions', () => {
    // A member-accessed call in the function body, and a string in the constant initializer, must
    // parse as expressions rather than stopping at a single identifier.
    const fn = parse('fn nearest(origin: Object, kind: String): Object = nwn.nearest_object_by_kind(origin, kind)');
    assert.deepEqual(errors(fn), []);
    assert.equal(nodes(fn, 'FunctionDeclaration').length, 1);
    assert.equal(nodes(fn, 'CallExpression').length, 1);

    const constant = parse('const OBJECT_DOOR = "door"');
    assert.deepEqual(errors(constant), []);
    assert.equal(nodes(constant, 'String').length, 1);
});

test('normal event scripts continue to parse unchanged', () => {
    const source = 'glyph vampiric_kill : encounter.on_creature_death {\n' +
        '    let killer = context.killer\n' +
        '    if distance(killer, context.dead_creature) <= 10 { heal(killer, 10) }\n' +
        '}';
    assert.deepEqual(errors(parse(source)), []);
    assert.equal(nodes(parse(source), 'Program').length, 1);
});

test('prelude declarations and an event script parse together', () => {
    const source = 'const OBJECT_TRIGGER = "trigger";\n' +
        'fn nearest(o: Object, k: String): Object = Object;\n' +
        'glyph test : interaction { tick {} }';
    const tree = parse(source);
    assert.deepEqual(errors(tree), []);
    assert.equal(nodes(tree, 'ConstantDeclaration').length, 1);
    assert.equal(nodes(tree, 'FunctionDeclaration').length, 1);
    assert.equal(nodes(tree, 'Program').length, 1);
});

test('const and fn keyword specializations stay aligned with GlyphLexer', () => {
    const lexerTypes = glyphLanguage.parser.nodeSet.types.map(type => type.name);
    assert.ok(lexerTypes.includes('const'), 'const must be a specialized keyword');
    assert.ok(lexerTypes.includes('fn'), 'fn must be a specialized keyword');
    const source = 'const X = "x"\nfn f(): Object = X\nglyph g : interaction { tick {} }';
    assert.deepEqual(errors(parse(source)), []);
});
