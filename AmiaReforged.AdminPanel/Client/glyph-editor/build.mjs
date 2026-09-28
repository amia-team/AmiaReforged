import { build } from 'esbuild';
import { buildParserFile } from '@lezer/generator';
import { readFile, writeFile } from 'node:fs/promises';

const generated = buildParserFile(await readFile('glyph.grammar', 'utf8'), { fileName: 'glyph.grammar' });
await writeFile('glyph-parser.js', generated.parser);
await writeFile('glyph-parser.terms.js', generated.terms);

const result = await build({
    entryPoints: ['editor.js'], bundle: true, format: 'esm', target: 'es2022',
    minify: true, legalComments: 'eof', metafile: true,
    outfile: '../../wwwroot/js/glyph-editor.js'
});
const packages = [...new Set(Object.keys(result.metafile.inputs)
    .filter(path => path.startsWith('node_modules/'))
    .map(path => path.match(/^node_modules\/((?:@[^/]+\/)?[^/]+)/)[1]))].sort();
const notices = await Promise.all(packages.map(async name =>
    `${name}\n${await readFile(`node_modules/${name}/LICENSE`, 'utf8')}`));
await writeFile('../../wwwroot/js/glyph-editor.LICENSE.txt', notices.join('\n\n'));
