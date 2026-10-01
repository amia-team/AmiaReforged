# Glyph editor bundle

From this directory, run:

```sh
npm ci
npm run build
```

Commit `package-lock.json`, the source, and the generated
`../../wwwroot/js/glyph-editor.js` and its generated license notices together. The bundle is checked in so normal
.NET builds and the existing Docker deployment do not require Node or a CDN.
Dependencies use exact versions; the lockfile also pins transitive dependencies.
Rebuild after changing dependencies or source and bump the import query version
in both `GlyphCodeEditor.razor` and `GlyphReferencePanel.razor` when publishing a new bundle.

The component lazily imports the bundle. It owns one editor per DOM host and
recreates the editor when the parent changes its document key (load/rollback).
Browser revisions reject delayed callbacks. `capture` freezes input and returns
current text before Save, Validate, or Activate; the parent then restores the
read-only state through its busy flag. Tab retains normal focus navigation except
while moving between active snippet placeholders (Escape exits the snippet).

`glyph.grammar` mirrors the server lexer and
parser; the build regenerates `glyph-parser.js` and `glyph-parser.terms.js` before
bundling. Commit those generated files with grammar changes. `glyph-language.js`
exposes the CodeMirror language and its syntax tree for autocomplete.
`UnterminatedString` intentionally preserves highlighting while typing; it does
not imply valid source. The server compiler remains authoritative for validation.

`glyph-completion.js` combines the syntax tree with metadata from
`GlyphApiService.GetLanguageMetadataAsync()`. Function and context suggestions
respect event/stage availability; local variables follow block scope and declaration
position. Completion starts automatically or with Ctrl+Space. Enter accepts a
suggestion, Escape closes it, and snippets expose editable parameter placeholders.
If metadata loading fails, editing and basic language suggestions remain available.

`glyph-diagnostics.js` maps compiler UTF-16 offsets to CodeMirror ranges. Validation
is explicit via Compile / validate. Clicking a diagnostic navigates to its range.
The browser clears markers immediately on an edit and rejects markers for a
mismatched source. The parent component also rejects asynchronous results from
older documents or edit revisions. Load and rollback create a fresh editor instance.
Run `npm run test:language` for corpus, highlighting, and incomplete-source tests.
These read the compiler's existing corpus directly and check keyword parity with
`GlyphLexer.cs`. Update the grammar and tests when changing server syntax.

Run all language, completion, diagnostics, and browser tests with `npm test`. It uses installed Chromium at
`/usr/bin/chromium`; set `CHROMIUM_PATH` to use another Chromium executable.
The test serves the production bundle on an ephemeral localhost port.

Engine completion uses the compiler's canonical `nwn.*` functions. Object is an opaque handle;
`player.` shows explicit domain aliases, not inferred NWScript methods. Location and Effect methods
come only from the classified `receiverMethods` metadata, with a known receiver type and policy.
The editor never derives methods from a function's first Object parameter.

## Persistent language reference

The source editor includes a collapsible reference beside CodeMirror on desktop,
with a stacked layout in windows below 1100px or editor containers below 1000px. Each pane scrolls independently. Functions use
compiler categories and canonical names; compatibility aliases are grouped in the
details rather than repeated in the list. Details show Glyph signatures, required
and optional parameters, descriptions, provenance (including `NWScript.GetTag`),
availability and deprecation. Typed receiver members and Glyph types have their
own sections. Members insert their canonical procedure with an explicit receiver.

The function reference is projected from compiler-owned language metadata. It is
not a separate maintained API list. It uses the same
`GlyphApiService.GetLanguageMetadataAsync()` request as autocomplete; no reference
endpoint or hand-maintained function/constant catalog exists. The Admin Panel DTO
also reads the server's receiver policy and deprecation fields.

Search is independent in each section (the text carries across section changes).
The browser filters a metadata-derived search index immediately, without a server
round trip per keystroke. It matches names, aliases, canonical/source names,
categories, parameter names/types, return types, constant names/domains/types and
descriptions. Exact names precede prefixes, canonical/source matches, category or
parameter matches, and description matches. The Blazor filter provides a fallback
if browser initialization fails. Constants start as collapsed domains, and search
or expanded domains initially render at most 80 rows, with explicit Show more.

Select an entry to read its details. Double-click it or use the keyboard-accessible
Insert button to insert at CodeMirror's remembered selection. Functions reuse
`functionSnippet` from autocomplete and select the first required parameter;
Tab moves between placeholders and Escape exits the snippet. Constants insert
only their qualified name. Both operations use CodeMirror transactions, replace
the selection, support undo and restore editor focus. Search-result arrow keys
move between buttons; Enter selects; ordinary Tab navigation reaches Insert.
Insertion is disabled while busy/read-only or when the code editor falls back to
a textarea. Metadata failure never prevents source editing; Retry reference and
Retry suggestions both retry the existing request.

Cursor context comes from the same `completionScope` syntax tree used by
completion, including while editing comments/strings. Availability is marked
against the server's event/stage pairs. Prioritize current context promotes usable
functions among equally ranked matches; unavailable APIs remain browsable and
show their supported contexts. Unknown contexts are not claimed to be valid.
Changing endpoints clears metadata and selection; generation checks reject old
metadata responses and reference selection callbacks.

`editor-reference.test.mjs` exercises the production bundle's insertion, focus,
selection replacement, snippet placeholders, undo, read-only guards and cursor
context events. `glyph-reference.test.mjs` covers browser-local search, ranking,
bounded constants, keyboard controls and metadata generation replacement. The
bUnit Glyph tests cover categories, details, search, availability, retry,
endpoint switching, accessibility and reference-to-editor interop.

## Function documentation

Hover a function in source or a Functions/Members reference row to read its Glyph
signature and documentation. NWScript bindings use the offline NWN Lexicon pack
joined by native `Source`, including aliases and real typed members. Other standard
functions use compiler-owned descriptions. Unknown calls, comments, strings and
unresolved receiver types have no function hover. In source, F1 opens the full
reference at the caret; Escape dismisses the tooltip. Reference rows show the same
help when focused, with `aria-describedby`. Hover never inserts text or moves the
CodeMirror selection. `Open full reference` opens the panel even if it was hidden.

Full details retain native signature, parameter prose, examples, remarks, version,
see-also sections, historical bug notes, attribution, revision, original wiki source
and license. Native examples are labeled NWScript and are read-only documentation.
Glyph's current compiler signature/defaults remain authoritative for calls and
insertion, including explicit command actors and adapted returns. Content is
rendered as escaped plain text and code, never imported HTML.

The pack is GFDL 1.1 or later and remains separate from application licensing.
See `tools/Glyph.Lexicon/README.md` for deterministic regeneration, coverage and
preservation of editable originals and notices. The existing metadata request and
cache carry the pack; hover and search do not fetch documentation from the network.
Old servers that omit the optional `documentation` field still show their binding
descriptions. Metadata replacement/null clears editor and row tooltips.

`glyph-documentation.test.mjs` checks syntax/type resolution and the production
bundle in Chromium: substantive native content, aliases, typed members, focus,
selection/source preservation, F1, Escape, metadata replacement, both reference
row renderers, viewport bounds and cleanup. bUnit tests verify real imported text,
parameter mapping, command/adapter distinctions, escaping, missing-doc fallback,
canonical navigation and opening a collapsed reference without recreating the editor.


## Authored modules

The grammar supports `mod`, `using`, `pub`, and qualified type/ADT names. The source
editor loads a small document-scoped overlay from `/glyphs/module-metadata`, separately
from the cached standard catalog and Lexicon pack. `setModuleMetadata` combines that
small overlay with the standard metadata already in the browser. Source/endpoint
versions discard stale responses; declaration edits are debounced. Script body edits
reuse the existing overlay when the prelude has not changed. Explicit validation also
refreshes module metadata so published dependency updates become visible.

Consumers receive public imported declarations; a module editor also receives its own
private declarations. Completion and function hovers use the same compiler-projected
signatures and availability. The module source editor shares the script editor's
capture, draft, and validation behavior, with publication and module revision history.
