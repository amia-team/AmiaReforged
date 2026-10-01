import json
from pathlib import Path
import unittest

from import_lexicon import extract_article, sections, OUTPUT


class LexiconImportTests(unittest.TestCase):
    def test_heading_variants_toc_code_and_reversed_definition_list(self):
        source = '''<!-- start content --><div id="mw-content-text">
        <table><tr><td>Warning: historical bug</td></tr></table><div id="toc">Contents</div>
        <h1>GetTag(object)</h1><p>Look up a tag.</p><pre>string GetTag(object oObject);</pre>
        <h4>Parameters</h4><dl><dd>oObject</dd><dt>Target object.</dt></dl>
        <h4>Description</h4><p>Returns the tag. Invalid objects return an empty string.</p>
        <h4>Example</h4><pre>void main() {\n    GetTag(OBJECT_SELF);\n}</pre>
        <p>author: Example contributor</p></div><!-- end content -->'''
        article = extract_article('GetTag', source, 'editable wiki', {'wgRevisionId': 12})
        self.assertEqual(article['parameters'], [{'name': 'oObject', 'description': 'Target object.'}])
        self.assertIn('Invalid objects', article['summary'])
        self.assertEqual(article['credits'], ['author: Example contributor'])
        self.assertIn('\n    GetTag', article['sections'][-1]['blocks'][0]['text'])
        self.assertNotIn('Contents', json.dumps(article))

    def test_untrusted_html_stays_inert_and_optional_parameters_match_names(self):
        source = '''<!-- start content --><div id="mw-content-text"><p>Query.</p>
        <pre>int Query(object oCreature, int nBase = FALSE);</pre>
        <h3>Parameters</h3><p><i>oCreature</i></p><p>The creature.</p><p><i>nBase</i></p><p>Base score.</p>
        <h3>Description</h3><p>&lt;script&gt;example&lt;/script&gt;</p><script>bad()</script>
        <h3>Known Bugs</h3><p>Historical bug.</p></div><!-- end content -->'''
        article = extract_article('Query', source, '{{wiki}}', {'wgRevisionId': 3})
        self.assertEqual([p['name'] for p in article['parameters']], ['oCreature', 'nBase'])
        self.assertEqual(article['summary'], '<script>example</script>')
        self.assertNotIn('bad()', json.dumps(article))
        self.assertEqual(article['sections'][-1]['title'], 'Known Bugs')

    def test_checked_in_pack_is_complete_attributed_and_transparent(self):
        pack = json.loads((OUTPUT / 'lexicon.json').read_text())
        report = json.loads((OUTPUT / 'coverage.json').read_text())
        self.assertEqual(len(pack['functions']), 441)
        self.assertEqual(report['publishedMembers'], 456)
        self.assertEqual(len(report['missing']), 15)
        self.assertEqual(pack['license'], 'GFDL-1.1-or-later')
        self.assertIn('BioWare', pack['notice'])
        self.assertIn('10. FUTURE REVISIONS', pack['licenseText'])
        self.assertIn('AmiaReforged', pack['history'])
        for source, article in pack['functions'].items():
            with self.subTest(source=source):
                self.assertEqual(source, 'NWScript.' + article['title'])
                self.assertTrue(article['originalSource'])
                self.assertTrue(article['summary'])
                self.assertTrue(article['nativeSignature'])
                self.assertGreater(article['revision'], 0)
                self.assertEqual(article['archiveDate'], '2022-07-13')
                self.assertTrue(article['sourceUrl'].startswith('https://lexicon.nwn.wiki/'))
                self.assertIn('Description', [s['title'] for s in article['sections']])


if __name__ == '__main__':
    unittest.main()
