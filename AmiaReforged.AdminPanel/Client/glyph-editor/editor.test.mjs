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
            <div id="host"></div><button id="after">After editor</button>
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
        assert.equal(await page.locator('#after').evaluate(el => el === document.activeElement), true);

        const snapshot = await page.evaluate(() => editor.capture(document.querySelector('#host')));
        assert.equal(snapshot.source, 'glyph example : interaction {} edited');
        assert.ok(snapshot.revision >= 3);
        assert.equal(await content.getAttribute('contenteditable'), 'false');
        await page.evaluate(() => editor.setReadOnly(document.querySelector('#host'), false));
        assert.equal(await content.getAttribute('contenteditable'), 'true');
        const revisions = await page.evaluate(() => changes.map(change => change[2]));
        assert.deepEqual(revisions, revisions.map((_, i) => i + 1));

        await page.evaluate(() => editor.create(document.querySelector('#host'), {
            invokeMethodAsync: async () => {}
        }, 'replacement', false));
        assert.equal(await page.locator('.cm-editor').count(), 1);
        assert.equal(await content.innerText(), 'replacement');
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
