import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright-core';
import { rankReference } from './glyph-reference.js';

const normalize = text => text.replace(/[^\p{L}\p{Nd}]/gu, '').toLowerCase();
const entry = (name, source, structural = '', description = '') => ({ tab: 'Functions', name,
    searchFields: [normalize(name.split('.').at(-1)), normalize(name), normalize(name + source), normalize(structural), normalize(description)],
    availableIn: [{ event: 'interaction', stage: 'completed' }] });
const entries = [entry('nwn.get_tag', 'NWScript.GetTag', 'Objects object Object String'),
    entry('nwn.set_tag', 'NWScript.SetTag', 'Objects object Object String'),
    entry('nwn.get_item_stack_size', 'NWScript.GetItemStackSize', 'Items Object Int', 'Look up a tag'),
    entry('object', '', 'Int', 'An exact name'),
    entry('nwn.get_location', 'NWScript.GetLocation', 'Location Object')];

test('reference ranks exact, prefix, source, parameter and description matches deterministically', () => {
    const names = query => entries.map(e => [e.name, rankReference(e.searchFields, query)]).filter(e => e[1] >= 0)
        .sort((a, b) => a[1] - b[1] || a[0].localeCompare(b[0])).map(e => e[0]);
    assert.deepEqual(names('tag'), ['nwn.get_tag', 'nwn.set_tag', 'nwn.get_item_stack_size']);
    assert.deepEqual(names('GetTag'), ['nwn.get_tag']);
    assert.equal(names('Object')[0], 'object');
    assert.deepEqual(names('location Object'), ['nwn.get_location']);
    assert.equal(rankReference(entries[0].searchFields, 'missing'), -1);
});

test('production reference search is local, bounded, accessible, endpoint-safe and responsive', async () => {
    const root = new URL('../../wwwroot/', import.meta.url);
    const files = new Map(await Promise.all(['js/glyph-editor.js', 'css/worldengine.css', 'css/admin.css', 'bootstrap/bootstrap.min.css', 'app.css']
        .map(async path => ['/' + path, await readFile(new URL(path, root))])));
    const panelMarkup = process.env.GLYPH_REFERENCE_PREVIEW ? await readFile(process.env.GLYPH_REFERENCE_PREVIEW, 'utf8') : `
        <aside id="glyph-reference" class="glyph-reference" aria-label="Glyph reference">
            <div class="glyph-reference-header"><strong>Glyph reference</strong>
                <input type="search" class="form-control" aria-label="Search reference" placeholder="Search reference…"></div>
            <div class="glyph-reference-client-results glyph-reference-list"></div>
            <div class="glyph-reference-server-list glyph-reference-list">Categories</div>
            <div class="glyph-reference-detail"><h4>nwn.get_tag</h4><pre class="glyph-reference-signature">nwn.get_tag(object: Object) → String</pre><p>Returns the tag of an object.</p><button type="button" class="btn btn-primary">Insert</button></div>
        </aside>`;
    const server = createServer((request, response) => {
        if (files.has(request.url)) {
            response.setHeader('Content-Type', request.url.endsWith('.js') ? 'text/javascript' : 'text/css'); response.end(files.get(request.url)); return;
        }
        response.setHeader('Content-Type', 'text/html');
        response.end(`<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
            <link rel="stylesheet" href="/bootstrap/bootstrap.min.css"><link rel="stylesheet" href="/app.css">
            <link rel="stylesheet" href="/css/admin.css"><link rel="stylesheet" href="/css/worldengine.css"></head>
            <body><main class="glyph-source-editor"><div class="glyph-source-toolbar">Glyph source<button class="btn btn-secondary">Hide reference</button></div>
            <div class="glyph-source-workspace"><div class="glyph-source-code"><div id="host" class="glyph-code-host"></div></div>${panelMarkup}</div></main>
            <script type="module">import * as editor from '/js/glyph-editor.js'; window.editor = editor;
                window.host = document.querySelector('#host'); window.panel = document.querySelector('#glyph-reference'); window.calls = [];
                window.callback = { invokeMethodAsync: async (...args) => { calls.push(args); } };
                editor.create(host, { invokeMethodAsync: async () => {} }, ${JSON.stringify('glyph example : interaction {\n    completed {\n        let tag = nwn.get_tag(player)\n        nwn.set_local_int(player, "flag", 1)\n    }\n}')}, false);
            </script></body></html>`);
    });
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    let browser;
    try {
        browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || '/usr/bin/chromium', headless: true });
        const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
        const errors = []; page.on('pageerror', e => errors.push(e.message));
        await page.goto(`http://127.0.0.1:${server.address().port}`); await page.waitForFunction(() => window.editor);
        await page.evaluate(entries => {
            panel.addEventListener('input', () => calls.push(['unexpected server input']));
            editor.setReferenceSearch(panel, callback, 1, entries, ['interaction']);
            editor.setReferenceSearchState(panel, 'Functions', true, { event: 'interaction', stage: 'tick' }, null);
        }, entries);
        const search = page.getByRole('searchbox', { name: 'Search reference' });
        await search.fill('tag');
        const rows = page.locator('.glyph-reference-client-results .glyph-reference-row');
        assert.deepEqual(await rows.allTextContents(), ['nwn.get_tag', 'nwn.set_tag', 'nwn.get_item_stack_size']);
        assert.deepEqual(await page.evaluate(() => calls), []); // no Blazor callback to filter.
        await rows.first().focus(); await page.keyboard.press('ArrowDown');
        assert.equal(await rows.nth(1).evaluate(el => el === document.activeElement), true);
        await page.keyboard.press('Enter');
        assert.equal(await rows.nth(1).getAttribute('aria-pressed'), 'true');
        assert.deepEqual(await page.evaluate(() => calls.at(-1)), ['OnReferenceSelected', 'Functions', 'nwn.set_tag', false, 1]);
        await rows.first().dblclick();
        assert.deepEqual(await page.evaluate(() => calls.at(-1)), ['OnReferenceSelected', 'Functions', 'nwn.get_tag', true, 1]);
        await search.fill('GetTag'); assert.equal(await rows.count(), 1);
        assert.equal(await rows.first().getAttribute('class'), 'glyph-reference-row glyph-reference-unavailable');
        await page.evaluate(() => editor.setReferenceSearchState(panel, 'Functions', false, { event: 'interaction', stage: 'completed' }, 'nwn.get_tag'));
        assert.equal(await rows.first().getAttribute('class'), 'glyph-reference-row');
        const bounds = await page.evaluate(() => ({ editor: host.getBoundingClientRect().width, panel: panel.getBoundingClientRect().width }));
        assert.ok(bounds.editor / (bounds.editor + bounds.panel) >= .7);
        if (process.env.GLYPH_REFERENCE_SCREENSHOT) await page.screenshot({ path: process.env.GLYPH_REFERENCE_SCREENSHOT });

        const constants = Array.from({ length: 3000 }, (_, i) => ({ ...entry(`VFX.VALUE_${i}`, 'NWScript'), tab: 'Constants', availableIn: null }));
        await page.evaluate(constants => {
            editor.setReferenceSearch(panel, callback, 2, constants, ['interaction']);
            editor.setReferenceSearchState(panel, 'Constants', false, null, null);
        }, constants);
        await search.fill('VFX'); assert.equal(await rows.count(), 80);
        await page.getByRole('button', { name: 'Show more results' }).click(); assert.equal(await rows.count(), 160);
        await page.evaluate(entries => editor.setReferenceSearch(panel, callback, 1, entries, ['interaction']), entries);
        assert.equal(await rows.count(), 160); // stale metadata generation is ignored.
        await page.setViewportSize({ width: 390, height: 844 });
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
        assert.ok(await page.evaluate(() => panel.getBoundingClientRect().top >= host.getBoundingClientRect().bottom));
        if (process.env.GLYPH_REFERENCE_MOBILE_SCREENSHOT) await page.screenshot({ path: process.env.GLYPH_REFERENCE_MOBILE_SCREENSHOT, fullPage: true });
        await search.fill(''); assert.equal(await rows.count(), 0);
        await page.evaluate(() => editor.destroyReferenceSearch(panel));
        assert.equal(await page.locator('.glyph-reference--searching').count(), 0);
        await search.fill('fallback');
        assert.deepEqual(await page.evaluate(() => calls.at(-1)), ['unexpected server input']);
        assert.deepEqual(errors, []);
    } finally {
        await browser?.close(); await new Promise(resolve => server.close(resolve));
    }
});
