import { test } from 'node:test';
import assert from 'node:assert/strict';
import { EditorState } from '@codemirror/state';
import { CompletionContext } from '@codemirror/autocomplete';
import { glyph } from './glyph-language.js';
import { glyphCompletions } from './glyph-completion.js';
import { metadata } from './test-metadata.js';
import { compilerDiagnostics, diagnosticRange } from './glyph-diagnostics.js';

function complete(marked, explicit = true, catalog = metadata) {
    const pos = marked.indexOf('|');
    const source = marked.replace('|', '');
    const state = EditorState.create({ doc: source, extensions: [glyph()] });
    return glyphCompletions(catalog)(new CompletionContext(state, pos, explicit));
}
function labels(source) { return complete(source)?.options.map(o => o.label) || []; }

test('members and functions respect event/stage restrictions and signatures', () => {
    assert.deepEqual(labels('glyph g : interaction { attempted { player.| } }'), ['player.has_item']);
    assert.deepEqual(labels('glyph g : interaction { tick { creature.| } }'), ['creature.hp']);
    assert.ok(!labels('glyph g : interaction { attempted { context.| } }').includes('context.session_id'));
    assert.ok(labels('glyph g : interaction { tick { context.| } }').includes('context.session_id'));
    assert.ok(!labels('glyph g : interaction { attempted { | } }').includes('set_progress'));
    assert.ok(labels('glyph g : interaction { tick { | } }').includes('set_progress'));
    assert.ok(!labels('glyph g : interaction { tick { | } }').includes('spawn.cancel'));
    assert.ok(labels('glyph g : encounter.before_group_spawn { | }').includes('spawn.cancel'));
    const option = complete('glyph g : interaction { tick { mess| } }').options.find(o => o.label === 'message');
    assert.match(option.detail, /creature: Object, message: String/);
    assert.equal(typeof option.apply, 'function');
});

test('named arguments omit parameters already supplied positionally or by name', () => {
    assert.ok(labels('glyph g : interaction { tick { if skill_check(|) {} } }').includes('creature:'));
    const options = labels('glyph g : interaction { tick { if skill_check(player, "search", |) {} } }');
    assert.ok(options.includes('dc:'));
    assert.ok(!options.includes('creature:'));
    assert.ok(!options.includes('skill:'));
    assert.ok(!labels('glyph g : interaction { tick { if skill_check(creature: player, |) {} } }').includes('creature:'));
    assert.ok(!labels('glyph g : interaction { tick { if skill_check(creature: |) {} } }').includes('dc:'));
    assert.ok(labels('glyph g : interaction { tick { if skill_check(|) {} } }').includes('dc:'));
    const result = complete('glyph g : interaction { tick { if skill_check(player, dc|: 12) {} } }');
    assert.equal(result.options.find(o => o.label === 'dc:').apply, 'dc');
});

test('locals obey block scope, declaration position, shadowing and foreach lifetime', () => {
    const source = 'glyph g : interaction { tick { let outer = 1 if true { let inner = 2 | } let later = 3 } completed { let other = 4 } }';
    const names = labels(source);
    assert.ok(names.includes('outer') && names.includes('inner'));
    assert.ok(!names.includes('later') && !names.includes('other'));
    assert.ok(!labels('glyph g : interaction { tick { if true { let hidden = 1 } | } }').includes('hidden'));
    assert.ok(!labels('glyph g : interaction { tick { let own = | } }').includes('own'));
    assert.ok(!labels('glyph g : interaction { tick { let own = |').includes('own'));
    assert.ok(!labels('glyph g : interaction { tick { let own = player.|').includes('own'));
    assert.ok(labels('glyph g : interaction { tick { foreach member in party.members { | } } }').includes('member'));
    assert.ok(!labels('glyph g : interaction { tick { foreach member in party.members {} | } }').includes('member'));
    assert.ok(!labels('glyph g : interaction { tick { foreach member in party.members { | } } }').includes('fail'));
    assert.equal(labels('glyph g : interaction { tick { let local = 1 if true { let local = 2 | } } }').filter(n => n === 'local').length, 1);
});

test('strings/comments suppress completion, including unfinished tokens', () => {
    for (const source of [
        'glyph g : interaction { tick { // player.|\n } }',
        'glyph g : interaction { tick { message(player, "hello |there") } }',
        'glyph g : interaction { tick { message(player, "unfinished|',
        'glyph g : interaction { tick { message(player, "escaped \\" |") } }'
    ]) assert.equal(complete(source), null, source);
});

test('event/stage snippets and incomplete programs remain useful without metadata', () => {
    assert.ok(labels('glyph g : encounter.|').includes('encounter.before_group_spawn'));
    assert.ok(labels('glyph g : interaction { | }').includes('attempted'));
    assert.ok(!labels('glyph g : interaction { attempted {} | }').includes('attempted'));
    assert.ok(labels('glyph g : interaction { tick { player.|').includes('player.has_item'));
    assert.ok(complete('glyph g : interaction { tick { le|', true, null).options.some(o => o.label === 'let'));
    assert.equal(complete('glyph g : interaction { tick { |', false), null);
});

test('diagnostic offsets preserve UTF-16/multiline ranges and clamp EOF spans', () => {
    const source = 'glyph café : interaction {\n// 🐈\ninvalid()\n}';
    const start = source.indexOf('invalid');
    const result = compilerDiagnostics([{ code: 'GLYPH2002', message: 'Unknown function', span: { start, length: 7 } }], source.length);
    assert.equal(source.slice(result[0].from, result[0].to), 'invalid');
    assert.equal(result[0].severity, 'error');
    assert.deepEqual(diagnosticRange({ start: source.length, length: 0 }, source.length), { from: source.length, to: source.length });
    assert.deepEqual(diagnosticRange({ start: -1, length: 999 }, 10), { from: 0, to: 10 });
});
