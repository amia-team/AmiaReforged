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
read-only state through its busy flag. Tab retains normal focus navigation.

This is step 19.1 only. Glyph highlighting, language metadata, autocomplete, and
inline compiler diagnostics are later steps in the plan.

Run the browser smoke test with `npm test`. It uses installed Chromium at
`/usr/bin/chromium`; set `CHROMIUM_PATH` to use another Chromium executable.
The test serves the production bundle on an ephemeral localhost port.
