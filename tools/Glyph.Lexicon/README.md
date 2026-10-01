# Glyph NWScript Lexicon import

This importer turns the local 2022-07-13 HTTrack NWN Lexicon mirror into an offline
reference for registered NWScript bindings. It does not execute archive scripts,
load remote pages, or infer the Glyph API from wiki signatures. Python 3 with its
standard library is sufficient.

```sh
python3 tools/Glyph.Lexicon/import_lexicon.py /path/to/lexicon.nwn_.wiki-2022-07-13
python3 -m unittest discover -s tools/Glyph.Lexicon -p 'test_*.py'
```

The deterministic output is checked in under
`AmiaReforged.PwEngine/Features/Glyph/Documentation/Lexicon/`. Import selects only
published `bound` and `adapted` native members from the reviewed
`NWN_API_SNAPSHOT.json`, matches MediaWiki page names exactly, chooses the latest
archived revision, and prefers regular views to print/oldid copies. Every selected
article must have a native signature, description and editable wiki source.
Missing articles and preserved encoding artifacts are listed in `coverage.json`.
The July 2022 archive covers 441 of 456 published native members; 15 have no matching
article. Missing documentation never removes a function from Glyph.

`lexicon.json` contains plain-text sections and code blocks, summary, native
parameter prose, original wikitext, article URL, revision and credits. Heading
levels and parameter markup vary in the archive. The importer preserves all
sections even where parameter extraction cannot produce a reliable name match.
Known bugs describe the archived version; they are not claims about current NWN.
Eleven articles contain legacy encoding artifacts. They are reported and retained
instead of being repaired speculatively.

The runtime embeds the JSON and reads it once. It projects only registered native
sources into the existing language metadata response, keyed by `NWScript.Name`.
Aliases and typed receiver methods share their canonical binding's article. Shared
adapters may declare a per-export `DocumentationSource`; the two hit-point exports
use their respective native articles rather than parsing a compound source label. Glyph
signatures, types, required arguments, defaults, availability and insertion remain
compiler-owned. Generated bindings carry the original native parameter names, so
renamed Glyph parameters can show native prose without positional guessing. Manual
adapters without explicit parameter provenance show the native parameter section
separately. An injected command `actor` is a Glyph parameter, not a native argument.

The complete pack is also copied to build and publish output under
`GlyphDocumentation/Lexicon`. The metadata includes editable original sources and
the shared license/history/notice, which can be read in the reference's disclosures.
There are no network requests on hover. The pack adds approximately 2.8 MB of
uncompressed compact JSON before filtering to registered sources, including
transparent originals. Existing metadata caching retains it once per endpoint;
reference search receives only summaries/provenance, not another copy of article
bodies or original sources. The importer has no dependency on a developer's
absolute archive path after generation.

## Documentation license

The imported reference is a separate documentation collection under GNU Free
Documentation License version 1.1 or any later version, **not** the application
code's license. `LICENSE.txt` preserves the archive's copyright and notices and the
full license. Article credits, revision identifiers, source URLs and original
editable wiki text remain in the pack. The collection's `history` records the
AmiaReforged transformation, title, contributors and date. Preserve these files
and notices when distributing or regenerating the documentation. Some native
examples carry the archive's BioWare notice. The importer and integration code are
application tooling; the imported prose and examples retain their separate license.

Packaged AmiaReforged function documentation is outside this import's scope.
