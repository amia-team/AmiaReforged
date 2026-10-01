import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { createServer } from 'node:http';
import { chromium } from 'playwright-core';
import { EditorState } from '@codemirror/state';
import { glyph } from './glyph-language.js';
import { resolveFunctionAt } from './glyph-completion.js';
import { functionDocumentation } from './glyph-documentation.js';

const pack = JSON.parse(await readFile(new URL('../../../AmiaReforged.PwEngine/Features/Glyph/Documentation/Lexicon/lexicon.json', import.meta.url)));
const fn = (name, source, type, parameters = []) => ({ name, canonicalName: name, source, backend: 'NWScript', description: 'Binding documentation',
    returnType: type, kind: 'Value', parameters, availableIn: [{ event: 'interaction', stage: 'tick' }] });
const getTag = fn('nwn.get_tag', 'NWScript.GetTag', 'String', [{ name: 'object', type: 'Object', required: true, sourceParameter: 'oObject' }]);
const metadata = { functions: [getTag, fn('nwn.get_location', 'NWScript.GetLocation', 'Location'),
    fn('nwn.location_x', 'NWScript.GetPositionFromLocation', 'Float'),
    fn('nwn.effect_haste', 'NWScript.EffectHaste', 'Effect'),
    { ...fn('effect.haste', 'NWScript.EffectHaste', 'Effect'), canonicalName: 'nwn.effect_haste' }],
    contexts: [], events: [{ name: 'interaction', stages: ['tick', 'completed'] }],
    receiverMethods: [{ name: 'get_x', canonicalName: 'nwn.location_x', receiverType: 'Location', returnType: 'Float', description: 'Coordinate adapter', parameters: [], availableIn: getTag.availableIn }],
    documentation: pack };
function binding(body) {
    const source = `glyph g : interaction { tick { ${body} } }`;
    const pos = source.indexOf('|');
    return resolveFunctionAt(EditorState.create({ doc: source.replace('|', ''), extensions: [glyph()] }), pos, metadata);
}

test('hover resolves qualified functions, aliases and typed locals through compiler metadata', () => {
    assert.equal(binding('nwn.get_|tag(player)').canonical, 'nwn.get_tag');
    assert.equal(binding('effect.has|te()').canonical, 'nwn.effect_haste');
    const typed = binding('let position = nwn.get_location(player) position.get_|x()');
    assert.equal(typed.canonical, 'nwn.location_x');
    assert.equal(typed.fn.source, 'NWScript.GetPositionFromLocation');
    assert.equal(typed.fn.name, 'Location.get_x');
    assert.equal(binding('nwn.get_location(player).get_|x()').canonical, 'nwn.location_x');
    assert.equal(binding('let unknown = player unknown.get_|x()'), null);
    assert.equal(binding('player.get_|tag()'), null);
    assert.equal(binding('"nwn.get_|tag(player)"'), null);
    assert.equal(binding('// nwn.get_|tag(player)\n'), null);
    assert.equal(binding('nwn.get_tag(pla|yer)'), null);
    assert.equal(binding('made_|up()'), null);
    assert.match(functionDocumentation(metadata, getTag).summary, /empty string/);
    assert.equal(functionDocumentation({ ...metadata, documentation: null }, getTag), null);
});

test('production hovers show offline docs, preserve source, support keyboard and clear stale metadata', async () => {
    const bundle = await readFile(new URL('../../wwwroot/js/glyph-editor.js', import.meta.url));
    const css = await readFile(new URL('../../wwwroot/css/worldengine.css', import.meta.url));
    const server = createServer((request, response) => {
        response.setHeader('Content-Type', request.url === '/editor.js' ? 'text/javascript' : request.url === '/style.css' ? 'text/css' : 'text/html');
        response.end(request.url === '/editor.js' ? bundle : request.url === '/style.css' ? css : `<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="/style.css">
            <div id="host"></div><aside id="panel"><input type="search"><div class="glyph-reference-client-results"></div>
            <button class="glyph-reference-row" data-reference-name="nwn.get_tag">get_tag</button></aside>
            <script type="module">import * as editor from '/editor.js'; window.editor = editor; window.host = document.querySelector('#host');
            window.panel = document.querySelector('#panel'); window.calls = []; window.callback = { invokeMethodAsync: async (...args) => { calls.push(args); } };</script>`);
    });
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    let browser;
    try {
        browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || '/usr/bin/chromium', headless: true });
        const page = await browser.newPage(); const errors = []; page.on('pageerror', e => errors.push(e.message));
        await page.goto(`http://127.0.0.1:${server.address().port}`); await page.waitForFunction(() => window.editor);
        const source = 'glyph g : interaction { tick { let tag = nwn.get_tag(player) } }';
        await page.evaluate(({ source, metadata }) => {
            editor.create(host, callback, source, false); editor.setMetadata(host, metadata);
            editor.focusDiagnostic(host, source, { start: source.indexOf('player'), length: 6 });
        }, { source, metadata });
        const token = page.locator('.cm-content .glyph-function').filter({ hasText: 'get_tag' });
        await token.hover();
        const tooltip = page.locator('.cm-tooltip .glyph-documentation-tooltip'); await tooltip.waitFor();
        assert.match(await tooltip.innerText(), /empty string/);
        assert.match(await tooltip.innerText(), /object: Target object/);
        assert.match(await tooltip.innerText(), /revision 57468/);
        assert.equal(await page.evaluate(() => getSelection().toString()), 'player');
        await tooltip.getByRole('button', { name: 'Open full reference' }).click();
        assert.deepEqual(await page.evaluate(() => calls.at(-1)), ['OnDocumentationRequested', 'nwn.get_tag']);
        assert.equal(await page.getByRole('textbox', { name: 'Glyph source' }).innerText(), source);
        assert.equal(await page.evaluate(() => calls.filter(c => c[0] === 'OnEditorChanged').length), 0);
        await page.evaluate(source => editor.focusDiagnostic(host, source, { start: source.indexOf('get_tag') + 3, length: 0 }), source);
        await page.keyboard.press('F1');
        assert.deepEqual(await page.evaluate(() => calls.at(-1)), ['OnDocumentationRequested', 'nwn.get_tag']);
        await page.keyboard.press('Escape'); await tooltip.waitFor({ state: 'hidden' });
        await page.evaluate(() => editor.setMetadata(host, null));
        await token.hover(); await page.waitForTimeout(400); assert.equal(await tooltip.count(), 0);
        await page.evaluate(metadata => editor.setMetadata(host, metadata), metadata);
        await token.hover(); await tooltip.waitFor();
        await page.evaluate(metadata => editor.setMetadata(host, { ...metadata, documentation: null }), metadata);
        await token.hover(); await tooltip.waitFor(); assert.match(await tooltip.innerText(), /Binding documentation/);
        await page.evaluate(() => editor.setMetadata(host, null));
        const referenceEntry = { tab: 'Functions', name: 'nwn.get_tag', tooltipFunction: getTag,
            documentation: pack.functions['NWScript.GetTag'], searchFields: ['gettag', 'nwngettag', 'nwscriptgettag', 'object', 'tag'] };
        await page.evaluate(entry => editor.setReferenceSearch(panel, callback, 1, [entry], ['interaction']), referenceEntry);
        const row = page.locator('#panel > .glyph-reference-row');
        await row.hover(); const referenceTip = page.locator('.glyph-reference-tooltip'); await referenceTip.waitFor();
        assert.match(await referenceTip.innerText(), /empty string/);
        assert.ok(await row.getAttribute('aria-describedby'));
        assert.equal(await row.evaluate(el => el === document.activeElement), false);
        await page.keyboard.press('Escape'); await referenceTip.waitFor({ state: 'hidden' });
        await row.focus(); await referenceTip.waitFor();
        await page.keyboard.press('Escape'); await referenceTip.waitFor({ state: 'hidden' });
        await page.getByRole('searchbox').fill('tag');
        const searchRow = page.locator('.glyph-reference-client-results .glyph-reference-row');
        await searchRow.hover(); await referenceTip.waitFor();
        await referenceTip.getByRole('button', { name: 'Open full reference' }).click();
        assert.deepEqual(await page.evaluate(() => calls.at(-1)), ['OnReferenceSelected', 'Functions', 'nwn.get_tag', false, 1]);
        await page.evaluate(() => editor.setReferenceSearch(panel, callback, 2, [], []));
        assert.equal(await referenceTip.count(), 0);
        await page.evaluate(entry => editor.setReferenceSearch(panel, callback, 3, [entry], []), referenceEntry);
        await searchRow.hover(); await referenceTip.waitFor();
        if (process.env.GLYPH_DOCUMENTATION_SCREENSHOT) await page.screenshot({ path: process.env.GLYPH_DOCUMENTATION_SCREENSHOT });
        await page.setViewportSize({ width: 390, height: 844 });
        await page.keyboard.press('Escape'); await searchRow.hover(); await referenceTip.waitFor();
        const box = await referenceTip.boundingBox(); assert.ok(box.x >= 0 && box.x + box.width <= 390);
        await page.evaluate(() => editor.destroyReferenceSearch(panel)); assert.equal(await referenceTip.count(), 0);
        await page.evaluate(() => editor.destroy(host)); assert.equal(await tooltip.count(), 0);
        assert.deepEqual(errors, []);
    } finally { await browser?.close(); await new Promise(resolve => server.close(resolve)); }
});
