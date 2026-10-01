import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright-core';

// Tests the checked-in production bundle, including actual browser editing commands.
test('editor supports editing, history, snapshots, read-only state and DOM cleanup', async () => {
    const bundle = await readFile(new URL('../../wwwroot/js/glyph-editor.js', import.meta.url));
    const server = createServer((request, response) => {
        response.setHeader('Content-Type', request.url === '/editor.js' ? 'text/javascript' : 'text/html');
        response.end(request.url === '/editor.js' ? bundle : `<!doctype html>
            <div id="host"></div><button id="after">After editor</button><button id="next">Next button</button>
            <script type="module">
                import * as editor from '/editor.js';
                window.editor = editor;
                window.changes = [];
                editor.create(document.querySelector('#host'), {
                    invokeMethodAsync: async (...args) => { window.changes.push(args); }
                }, 'glyph example : interaction {}', false);
            </script>`);
    });
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    let browser;
    try {
        browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || '/usr/bin/chromium', headless: true });
        const page = await browser.newPage();
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        await page.goto(`http://127.0.0.1:${server.address().port}`);
        const content = page.getByRole('textbox', { name: 'Glyph source' });
        await content.waitFor();
        assert.equal(await page.locator('.glyph-keyword').first().innerText(), 'glyph');
        assert.equal(await page.locator('.glyph-keyword').first().evaluate(el => getComputedStyle(el).color), 'rgb(221, 192, 106)');
        assert.equal(await page.locator('.cm-lineNumbers').count(), 1);
        await content.click();
        await page.keyboard.press('Control+End');
        await page.keyboard.type(' edited');
        assert.match(await content.innerText(), /edited$/);
        await page.keyboard.press('Control+z');
        assert.equal(await content.innerText(), 'glyph example : interaction {}');
        await page.keyboard.press('Control+Shift+z');
        assert.match(await content.innerText(), /edited$/);
        await page.keyboard.press('Tab');
        assert.equal(await content.evaluate(el => el === document.activeElement), true);
        await page.keyboard.type('tabbed');
        assert.equal(await content.innerText(), 'glyph example : interaction {} edited\ttabbed');
        // Dismiss completion before using CodeMirror's Escape/Tab focus escape.
        await page.keyboard.press('Escape');
        await page.keyboard.press('Escape');
        await page.keyboard.press('Tab');
        assert.equal(await page.locator('#after').evaluate(el => el === document.activeElement), true);
        await page.keyboard.press('Tab');
        assert.equal(await page.locator('#next').evaluate(el => el === document.activeElement), true);

        const snapshot = await page.evaluate(() => editor.capture(document.querySelector('#host')));
        assert.equal(snapshot.source, 'glyph example : interaction {} edited\ttabbed');
        assert.ok(snapshot.revision >= 4);
        assert.equal(await content.getAttribute('contenteditable'), 'false');
        await page.evaluate(() => editor.setReadOnly(document.querySelector('#host'), false));
        assert.equal(await content.getAttribute('contenteditable'), 'true');
        const revisions = await page.evaluate(() => changes.filter(change => change[0] === 'OnEditorChanged').map(change => change[2]));
        assert.deepEqual(revisions, revisions.map((_, i) => i + 1));

        const indented = '\tfirst\n\tsecond';
        await page.evaluate(source => {
            const host = document.querySelector('#host');
            editor.create(host, { invokeMethodAsync: async () => {} }, source, false);
            editor.focusDiagnostic(host, source, { start: 0, length: source.length });
        }, indented);
        await page.keyboard.press('Tab');
        assert.equal(await content.innerText(), '\t\tfirst\n\t\tsecond');
        assert.equal(await content.evaluate(el => el === document.activeElement), true);
        await page.keyboard.press('Shift+Tab');
        assert.equal(await content.innerText(), indented);
        await page.evaluate(source => {
            const host = document.querySelector('#host');
            editor.focusDiagnostic(host, source, { start: source.length, length: 0 });
            editor.setReadOnly(host, true);
        }, indented);
        await page.keyboard.press('Tab');
        assert.equal((await page.evaluate(() => editor.capture(document.querySelector('#host')))).source, indented);

        await page.evaluate(() => editor.create(document.querySelector('#host'), {
            invokeMethodAsync: async () => {}
        }, 'replacement', false));
        assert.equal(await page.locator('.cm-editor').count(), 1);
        assert.equal(await content.innerText(), 'replacement');
        const highlighted = 'glyph colors : interaction {\n tick { // comment\n if true { message(player, "hello", dc: 15) progress += 1 }\n }\n}';
        await page.evaluate(source => editor.create(document.querySelector('#host'), {
            invokeMethodAsync: async () => {}
        }, source, false), highlighted);
        for (const [selector, text] of [
            ['.glyph-function', 'message'], ['.glyph-string', '"hello"'],
            ['.glyph-property', 'dc'], ['.glyph-comment', '// comment']
        ]) assert.equal(await page.locator(selector).first().innerText(), text);
        await content.click();
        await page.keyboard.press('Control+End');
        await page.keyboard.press('Backspace');
        await page.keyboard.type('message(player, "unfinished');
        assert.equal(await page.locator('.glyph-string').last().innerText(), '"unfinished');
        await page.keyboard.type('") }');
        assert.equal(await page.locator('.glyph-string').last().innerText(), '"unfinished"');
        if (process.env.GLYPH_SCREENSHOT) await page.screenshot({ path: process.env.GLYPH_SCREENSHOT });
        await page.evaluate(() => {
            window.detachedHost = document.querySelector('#host');
            detachedHost.remove();
        });
        await page.waitForFunction(() => !detachedHost.querySelector('.cm-editor'));
        assert.deepEqual(errors, []);
    } finally {
        await browser?.close();
        await new Promise(resolve => server.close(resolve));
    }
});
