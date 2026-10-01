#!/usr/bin/env python3
"""Import a local HTTrack Lexicon archive as inert text; Python standard library only."""
import argparse
import html
from html.parser import HTMLParser
import json
from pathlib import Path
import re
from urllib.parse import quote

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'AmiaReforged.PwEngine/Features/Glyph/Documentation/Lexicon'

class Node:
    def __init__(self, tag='', attrs=()):
        self.tag, self.attrs, self.children = tag, dict(attrs), []
    def text(self):
        return ''.join(c.text() if isinstance(c, Node) else c for c in self.children)
    def descendants(self, tag):
        for child in self.children:
            if isinstance(child, Node):
                if child.tag == tag:
                    yield child
                yield from child.descendants(tag)

class Parser(HTMLParser):
    def __init__(self, source):
        super().__init__(convert_charrefs=True)
        self.root = Node()
        self.stack = [self.root]
        self.feed(source)
    def handle_starttag(self, tag, attrs):
        node = Node(tag, attrs)
        self.stack[-1].children.append(node)
        if tag not in {'br', 'hr', 'img', 'input', 'meta', 'link', 'wbr'}:
            self.stack.append(node)
    def handle_endtag(self, tag):
        for i in range(len(self.stack) - 1, 0, -1):
            if self.stack[i].tag == tag:
                del self.stack[i:]
                break
    def handle_data(self, data):
        self.stack[-1].children.append(data)


def clean(text):
    return re.sub(r'\s+', ' ', text.replace('\xa0', ' ')).strip()


def blocks(node):
    """Keep prose, table/list boundaries and literal code; never copy HTML or execute scripts."""
    if isinstance(node, str):
        value = clean(node)
        return [{'kind': 'text', 'text': value}] if value else []
    if node.tag in {'script', 'style'} or node.attrs.get('id') == 'toc':
        return []
    if node.tag == 'pre':
        return [{'kind': 'code', 'text': node.text().replace('\xa0', ' ').strip('\n')}]
    if node.tag in {'p', 'dt', 'dd', 'li', 'tr'}:
        value = clean(node.text())
        return [{'kind': 'text', 'text': value}] if value else []
    return [block for child in node.children for block in blocks(child)]


def sections(source):
    body = source.split('<!-- start content -->', 1)[1].split('<!-- end content -->', 1)[0]
    tree = Parser(body).root
    content = next((n for n in tree.descendants('div') if n.attrs.get('id') == 'mw-content-text'), tree)
    result = [{'title': 'Overview', 'blocks': []}]
    for child in content.children:
        if isinstance(child, Node) and child.tag in {'h1', 'h2', 'h3', 'h4', 'h5', 'h6'}:
            result.append({'title': clean(child.text()), 'blocks': []})
        else:
            result[-1]['blocks'].extend(blocks(child))
    return [s for s in result if s['blocks']]


def configuration(source):
    marker = 'mw.config.set('
    if marker not in source:
        return None
    # JSON only. MediaWiki script is data, never evaluated.
    return json.JSONDecoder().raw_decode(source.split(marker, 1)[1])[0]


def extract_article(title, source, wiki, config):
    parts = sections(source)
    overview = next((s for s in parts if any(b['kind'] == 'code' and re.search(r'\b' + re.escape(title) + r'\s*\(', b['text']) for b in s['blocks'])), parts[0])
    signature = next((b['text'] for b in overview['blocks'] if b['kind'] == 'code'), '')
    if not signature or not any(s['title'] == 'Description' for s in parts):
        raise ValueError(f'{title}: missing native signature or description')
    description = next(s for s in parts if s['title'] == 'Description')
    summary = '\n\n'.join(b['text'] for b in description['blocks'] if b['kind'] == 'text')
    # Show substantive native behavior in hover; retain every paragraph in full reference.
    short = next((b['text'] for b in description['blocks'] if b['kind'] == 'text'), summary)
    native_names = re.findall(r'\b([a-zA-Z_]\w*)\s*(?:=[^,)]*)?(?=[,)])', signature)
    native_names = [n for n in native_names if n not in {'void'}]
    param_blocks = next((s['blocks'] for s in parts if s['title'] == 'Parameters'), [])
    parameters = []
    for name in native_names:
        texts = [b['text'] for b in param_blocks]
        prose = ''
        for i, value in enumerate(texts):
            if value == name and i + 1 < len(texts):
                prose = texts[i + 1]
                break
            if re.match(r'^' + re.escape(name) + r'(?:\s|[:–-])', value):
                prose = value[len(name):].lstrip(' :–-')
                break
        if prose:
            parameters.append({'name': name, 'description': prose})
    credits = [b['text'] for s in parts for b in s['blocks'] if re.search(r'\b(?:author|editor):', b['text'], re.I)]
    url = 'https://lexicon.nwn.wiki/index.php?title=' + quote(title) + '&oldid=' + str(config['wgRevisionId'])
    return {'title': title, 'sourceUrl': url, 'revision': config['wgRevisionId'],
            'archiveDate': '2022-07-13', 'summary': short, 'nativeSignature': signature,
            'parameters': parameters, 'sections': parts, 'credits': credits, 'originalSource': wiki}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archive', type=Path)
    parser.add_argument('--output', type=Path, default=OUTPUT)
    args = parser.parse_args()
    base = args.archive / 'lexicon.nwn.wiki'
    if not base.is_dir():
        parser.error('Expected archive/lexicon.nwn.wiki directory')
    snapshot = json.loads((ROOT / 'AmiaReforged.PwEngine/Features/Glyph/Language/Standard/NWN_API_SNAPSHOT.json').read_text())
    titles = sorted({b['member'] for b in snapshot['bindings'] if b['status'] in {'bound', 'adapted'}})
    views, edits = {}, {}
    warnings = []
    license_source = None
    for path in sorted(base.glob('*.html')):
        source = path.read_text(encoding='utf-8')
        config = configuration(source)
        if not config:
            continue
        title = config.get('wgPageName')
        if title == 'NWN_Lexicon:Copyrights' and config.get('wgAction') == 'view':
            license_source = source
        if title not in titles or config.get('wgNamespaceNumber') != 0:
            continue
        if config.get('wgAction') == 'edit':
            tree = Parser(source).root
            textarea = next((n for n in tree.descendants('textarea') if n.attrs.get('id') == 'wpTextbox1'), None)
            if textarea:
                edits[title] = textarea.text()
        if config.get('wgAction') == 'view' and not config.get('wgIsRedirect') and config.get('wgRevisionId') == config.get('wgCurRevisionId'):
            # Deterministic preference for regular latest view over oldid and print views.
            score = ('oldid=' in source[:1200], 'printable=' in source[:1200], path.name)
            if title not in views or score < views[title][0]:
                views[title] = (score, source, config)
    if not license_source:
        # Archive page title is sometimes simply "Copyright"; identify its exact copyright notice.
        for path in sorted(base.glob('*.html')):
            source = path.read_text(encoding='utf-8')
            config = configuration(source)
            if config and config.get('wgAction') == 'view' and 'id="NWN_Lexicon_Contributor_Agreement"' in source and 'id="License"' in source:
                license_source = source
                break
    if not license_source:
        raise ValueError('Archive copyright/license page not found')
    license_sections = sections(license_source)
    copyright_section = next(s for s in license_sections if s['title'] == 'Copyright')
    notice = '\n\n'.join(b['text'] for b in copyright_section['blocks'])
    license_begin = next(i for i, s in enumerate(license_sections) if s['title'] == 'GNU Free Documentation License')
    license_text = '\n\n'.join(s['title'] + '\n\n' + '\n\n'.join(b['text'] for b in s['blocks']) for s in license_sections[license_begin:])
    articles = {}
    for title in titles:
        if title not in views:
            continue
        if title not in edits:
            raise ValueError(f'{title}: missing transparent wiki source')
        _, source, config = views[title]
        article = extract_article(title, source, edits[title], config)
        if any('\x80' <= c <= '\x9f' or c == '\ufffd' for c in source):
            warnings.append(f'{title}: legacy encoding artifacts preserved; no speculative repair')
        articles['NWScript.' + title] = article
    pack = {'name': 'Glyph NWScript Lexicon reference', 'archiveDate': '2022-07-13',
            'license': 'GFDL-1.1-or-later', 'notice': notice, 'licenseText': license_text,
            'history': '2026-10-01: AmiaReforged contributors extracted selected NWScript articles from the 2022-07-13 NWN Lexicon archive. HTML was converted to plain-text prose and code blocks, retaining native signatures, sections, credits, revision identifiers and editable original wiki sources. This modified reference is licensed under GFDL 1.1 or any later version. Original article history is available through each source URL (action=history).',
            'functions': articles}
    report = {'archiveDate': pack['archiveDate'], 'publishedMembers': len(titles), 'matched': len(articles),
              'missing': [t for t in titles if 'NWScript.' + t not in articles], 'warnings': warnings}
    args.output.mkdir(parents=True, exist_ok=True)
    for filename, data in [('lexicon.json', pack), ('coverage.json', report)]:
        (args.output / filename).write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    (args.output / 'LICENSE.txt').write_text(notice + '\n\n' + license_text + '\n', encoding='utf-8')
    print(f'Imported {len(articles)}/{len(titles)} members; {len(warnings)} encoding warnings; {len(json.dumps(pack).encode())} JSON bytes (ASCII encoding).')

if __name__ == '__main__':
    main()
