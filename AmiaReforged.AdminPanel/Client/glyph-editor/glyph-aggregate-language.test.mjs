import { test } from 'node:test';
import assert from 'node:assert/strict';
import { highlightTree } from '@lezer/highlight';
import { glyphLanguage, glyphHighlightStyle } from './glyph-language.js';

function parse(source) {
    return glyphLanguage.parser.parse(source);
}

function errors(tree) {
    const result = [];
    tree.iterate({
        enter(node) {
            if (node.type.isError) result.push([node.from, node.to]);
        }
    });
    return result;
}

function nodes(tree, name) {
    const result = [];
    tree.iterate({
        enter(node) {
            if (node.name === name) result.push(node.node);
        }
    });
    return result;
}

function highlights(source) {
    const result = [];
    highlightTree(parse(source), glyphHighlightStyle, (from, to, style) =>
        result.push({ text: source.slice(from, to), style }));
    return result;
}

test('declaration commas are required between fields and variants; trailing commas are optional', () => {
    for (const source of [
        'struct Pair { first: Int, second: String }',
        'struct Pair { first: Int, second: String, }',
        'type Result { Found { value: Int, label: String }, Missing {} }',
        'type Result { Found { value: Int, label: String, }, Missing {}, }',
        'struct Empty {} type Result { Missing {} }',
        'struct Pair { first: Int, // comment\n second: String, }',
    ]) {
        assert.deepEqual(errors(parse(source)), [], source);
    }

    for (const source of [
        'struct Pair { first: Int second: String }',
        'struct Pair { first: Int\n second: String }',
        'struct Pair { first: Int; second: String }',
        'struct Pair { first: Int; }',
        'type Result { Found { value: Int label: String }, Missing {} }',
        'type Result { Found { value: Int; label: String }, Missing {} }',
        'type Result { Found {} Missing {} }',
        'type Result { Found {}\n Missing {} }',
        'type Result { Found {}; Missing {} }',
        'type Result { Found {}; }',
    ]) {
        assert.ok(errors(parse(source)).length > 0, source);
    }
});

test('struct and ADT declarations parse with exhaustive match syntax', () => {
    const source = `
struct Request {
    target: Object,
    text: String,
}

type Result {
    Found {
        request: Request,
    },

    Missing {
        reason: String,
    },
}

glyph aggregate_test : interaction {
    tick {
        let request = Request(target: player, text: "hello")
        let result = Result.Found(request: request)

        match result {
            Found { request } {
                message(request.target, request.text)
            }

            Missing { reason } {
                message(player, reason)
            }
        }
    }
}`;

    const tree = parse(source);

    assert.deepEqual(errors(tree), [], tree.toString());
    assert.equal(nodes(tree, 'StructDeclaration').length, 1);
    assert.equal(nodes(tree, 'AdtDeclaration').length, 1);
    assert.equal(nodes(tree, 'VariantDeclaration').length, 2);
    assert.equal(nodes(tree, 'MatchStatement').length, 1);
    assert.equal(nodes(tree, 'MatchArm').length, 2);
});

test('aggregate keywords and names receive useful highlighting', () => {
    const source = `
struct Request { target: Object, }
type Result { Found { request: Request, }, }

glyph g : interaction {
    tick {
        let result = Result.Found(request: Request(target: player))
        match result {
            Found { request } {
                message(request.target, "ok")
            }
        }
    }
}`;

    const spans = highlights(source);

    for (const keyword of ['struct', 'type', 'match']) {
        assert.ok(
            spans.some(span => span.text === keyword && span.style === 'glyph-keyword'),
            `${keyword}: ${JSON.stringify(spans)}`
        );
    }

    for (const typeName of ['Request', 'Result', 'Found']) {
        assert.ok(
            spans.some(span => span.text === typeName && span.style === 'glyph-event'),
            `${typeName}: ${JSON.stringify(spans)}`
        );
    }
});
