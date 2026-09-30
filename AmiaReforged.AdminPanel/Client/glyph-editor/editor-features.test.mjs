import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright-core';
import { metadata } from './test-metadata.js';

test('browser completion insertion, scope filtering, diagnostics and revision guards', async () => {
    const bundle = await readFile(new URL('../../wwwroot/js/glyph-editor.js', import.meta.url));
    const server = createServer((request, response) => {
        response.setHeader('Content-Type', request.url === '/editor.js' ? 'text/javascript' : 'text/html');
        response.end(request.url === '/editor.js' ? bundle : `<!doctype html>
            <div id="host"></div><button>After editor</button>
            <script type="module">
                import * as editor from '/editor.js';
                window.editor = editor;
                window.metadata = ${JSON.stringify(metadata)};
                window.host = document.querySelector('#host');
            </script>`);
    });
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    let browser;
    try {
        browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || '/usr/bin/chromium', headless: true });
        const page = await browser.newPage();
        const errors = [];
        page.on('pageerror', e => errors.push(e.message));
        await page.goto(`http://127.0.0.1:${server.address().port}`);
        await page.waitForFunction(() => window.editor);
        async function document(marked) {
            await page.evaluate(marked => {
                const pos = marked.indexOf('|');
                const source = marked.replace('|', '');
                editor.create(host, { invokeMethodAsync: async () => {} }, source, false);
                editor.setMetadata(host, metadata);
                editor.focusDiagnostic(host, source, { start: pos, length: 0 });
            }, marked);
        }
        async function source() {
            return page.evaluate(() => {
                const value = editor.capture(host).source;
                editor.setReadOnly(host, false);
                return value;
            });
        }
        const menu = page.locator('.cm-tooltip-autocomplete');
        await document('glyph g : interaction { tick { | } }');
        await page.keyboard.type('player.');
        await menu.waitFor();
        await menu.getByText('has_item', { exact: true }).click();
        assert.match(await source(), /player\.has_item\(tag\)/);
        assert.doesNotMatch(await source(), /player\.player/);
        await page.keyboard.press('Escape');
        await page.keyboard.press('Control+z');
        assert.match(await source(), /player\. /);

        await document('glyph g : interaction { tick { if skill_check(player, "search", |) {} } }');
        await page.keyboard.press('Control+Space');
        await menu.waitFor();
        assert.equal(await menu.getByText('creature:', { exact: true }).count(), 0);
        await menu.getByText('dc:', { exact: true }).click();
        assert.match(await source(), /"search", dc: \)/);

        await document('glyph g : interaction { tick { let outer = 1 if true { let inner = 2 | } let later = 3 } }');
        await page.keyboard.press('Control+Space');
        await menu.waitFor();
        assert.equal(await menu.getByText('inner', { exact: true }).count(), 1);
        assert.equal(await menu.getByText('outer', { exact: true }).count(), 1);
        assert.equal(await menu.getByText('later', { exact: true }).count(), 0);
        await page.keyboard.press('Escape');
        await document('glyph g : interaction { attempted { set_| } }');
        await page.keyboard.press('Control+Space');
        assert.equal(await page.getByText('set_progress', { exact: true }).count(), 0);

        const marked = 'glyph café : interaction {\n tick {\n // 🐈\n invalid|()\n }\n}';
        await document(marked);
        const original = marked.replace('|', '');
        const start = original.indexOf('invalid');
        const diagnostic = { code: 'GLYPH2002', message: 'Unknown function', span: { start, length: 7 } };
        await page.evaluate(({ original, diagnostic }) => editor.showDiagnostics(host, original, [diagnostic]), { original, diagnostic });
        await page.locator('.cm-lintRange-error').waitFor();
        assert.equal(await page.locator('.cm-lintRange-error').innerText(), 'invalid');
        await page.evaluate(({ original, diagnostic }) => editor.focusDiagnostic(host, original, diagnostic.span), { original, diagnostic });
        assert.equal(await page.evaluate(() => window.getSelection().toString()), 'invalid');
        await page.keyboard.type('message');
        assert.equal(await page.locator('.cm-lintRange-error').count(), 0);
        await page.evaluate(({ original, diagnostic }) => editor.showDiagnostics(host, original, [diagnostic]), { original, diagnostic });
        assert.equal(await page.locator('.cm-lintRange-error').count(), 0);
        await document('glyph next : interaction { tick { | } }');
        await page.evaluate(({ original, diagnostic }) => editor.showDiagnostics(host, original, [diagnostic]), { original, diagnostic });
        assert.equal(await page.locator('.cm-lintRange-error').count(), 0);
        await page.keyboard.type('player.');
        await menu.waitFor();
        if (process.env.GLYPH_FEATURE_SCREENSHOT) await page.screenshot({ path: process.env.GLYPH_FEATURE_SCREENSHOT });
        assert.deepEqual(errors, []);
    } finally {
        await browser?.close();
        await new Promise(resolve => server.close(resolve));
    }
});
