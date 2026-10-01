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
in `GlyphCodeEditor.razor` when publishing a new bundle.

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
