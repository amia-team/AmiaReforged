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

test('struct and ADT declarations parse with exhaustive match syntax', () => {
    const source = `
struct Request {
    target: Object
    text: String
}

type Result {
    Found {
        request: Request
    }

    Missing {
        reason: String
    }
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
struct Request { target: Object }
type Result { Found { request: Request } }

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
