import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright-core';
import { functionSnippet } from './glyph-completion.js';

const fn = { name: 'nwn.set_local_int', canonicalName: 'nwn.set_local_int', parameters: [
    { name: 'object', type: 'Object', required: true },
    { name: 'var_name', type: 'String', required: true },
    { name: 'value', type: 'Int', required: true },
    { name: 'optional', type: 'Bool', required: false, defaultValue: 'false' }
] };

test('reference uses autocomplete snippet template and only required arguments', () => {
    assert.equal(functionSnippet(fn), 'nwn.set_local_int(${object}, ${var_name}, ${value})');
    assert.equal(functionSnippet({ name: 'effect.haste', parameters: [] }), 'effect.haste()');
});

test('production CodeMirror inserts reference snippets and constants at remembered selection and reports context', async () => {
    const bundle = await readFile(new URL('../../wwwroot/js/glyph-editor.js', import.meta.url));
    const server = createServer((request, response) => {
        response.setHeader('Content-Type', request.url === '/editor.js' ? 'text/javascript' : 'text/html');
        response.end(request.url === '/editor.js' ? bundle : `<!doctype html>
            <div id="host"></div><input aria-label="Search reference"><button id="insert">Insert</button>
            <script type="module">
                import * as editor from '/editor.js';
                window.editor = editor; window.host = document.querySelector('#host'); window.callbacks = [];
            </script>`);
    });
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    let browser;
    try {
        browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || '/usr/bin/chromium', headless: true });
        const page = await browser.newPage();
        const errors = []; page.on('pageerror', e => errors.push(e.message));
        await page.goto(`http://127.0.0.1:${server.address().port}`);
        await page.waitForFunction(() => window.editor);
        const content = page.getByRole('textbox', { name: 'Glyph source' });
        const source = 'glyph g : interaction { tick { REPLACE } completed { } }';
        await page.evaluate(source => {
            editor.create(host, { invokeMethodAsync: async (...args) => { callbacks.push(args); } }, source, false);
            editor.focusDiagnostic(host, source, { start: source.indexOf('REPLACE'), length: 7 });
        }, source);
        await page.getByRole('textbox', { name: 'Search reference' }).fill('locals');
        await page.evaluate(fn => editor.insertFunction(host, fn), fn);
        assert.equal(await content.innerText(), source.replace('REPLACE', 'nwn.set_local_int(object, var_name, value)'));
        assert.equal(await content.evaluate(el => el === document.activeElement), true);
        assert.equal(await page.evaluate(() => window.getSelection().toString()), 'object');
        await page.keyboard.type('player');
        await page.keyboard.press('Tab');
        assert.equal(await page.evaluate(() => window.getSelection().toString()), 'var_name');
        await page.keyboard.type('"flag"');
        await page.keyboard.press('Tab');
        await page.keyboard.type('1');
        await page.keyboard.press('Escape');
        assert.equal(await content.innerText(), source.replace('REPLACE', 'nwn.set_local_int(player, "flag", 1)'));
        await page.keyboard.press('Control+z');
        await page.keyboard.press('Control+z');
        await page.keyboard.press('Control+z');
        await page.keyboard.press('Control+z');
        assert.equal(await content.innerText(), source);

        // Move into another stage, then insert without changing the saved CodeMirror selection.
        await page.evaluate(source => editor.focusDiagnostic(host, source, { start: source.indexOf('completed {') + 12, length: 0 }), source);
        await page.getByRole('textbox', { name: 'Search reference' }).fill('creature');
        await page.evaluate(() => editor.insertConstant(host, { name: 'OBJECT_TYPE.CREATURE' }));
        assert.equal(await content.innerText(), source.replace('completed { ', 'completed { OBJECT_TYPE.CREATURE'));
        assert.equal(await content.evaluate(el => el === document.activeElement), true);
        assert.equal(await page.evaluate(() => callbacks.filter(c => c[0] === 'OnCursorContextChanged').at(-1)[1].stage), 'completed');
        const contexts = await page.evaluate(() => callbacks.filter(c => c[0] === 'OnCursorContextChanged'));
        assert.ok(contexts.some(c => c[1].event === 'interaction' && c[1].stage === 'tick'));
        assert.deepEqual(contexts.map(c => c[2]), contexts.map((_, i) => i + 1));
        await page.keyboard.press('Control+z'); assert.equal(await content.innerText(), source);

        await page.evaluate(source => {
            editor.focusDiagnostic(host, source, { start: source.indexOf('REPLACE'), length: 7 });
            editor.insertConstant(host, { name: 'OBJECT_TYPE.CREATURE' });
        }, source);
        assert.equal(await content.innerText(), source.replace('REPLACE', 'OBJECT_TYPE.CREATURE'));
        await page.evaluate(() => editor.setReadOnly(host, true));
        await page.evaluate(fn => { editor.insertFunction(host, fn); editor.insertConstant(host, { name: 'DAMAGE_TYPE.FIRE' }); }, fn);
        assert.equal(await content.innerText(), source.replace('REPLACE', 'OBJECT_TYPE.CREATURE'));
        assert.deepEqual(errors, []);
    } finally {
        await browser?.close(); await new Promise(resolve => server.close(resolve));
    }
});
