# 14 — Extract editor styles + delete GL files

> True deletion (11 was rescoped because Codex still needed GL — 12 removed
> that blocker).

## Files

- `wwwroot/css/worldengine.css` (new, 112 rules)
- `Components/App.razor` (stylesheet links)
- `wwwroot/js/glyph-editor.js` (doc comment only)

## Deleted

- `wwwroot/css/goldenlayout-amia-theme.css`
- `wwwroot/css/lib/` (base + dark theme + img)
- `wwwroot/js/golden-layout-bridge.js`
- `wwwroot/js/lib/golden-layout.js` (+ now-empty `js/lib/`)

## Steps

- [x] Split `goldenlayout-amia-theme.css` by top-level rule: dropped 38 rules
  matching `lm[_-]` / `gl-host` / `gl-panel` (pure GL chrome + dead host/panel
  classes); kept 112 editor rules into `worldengine.css` (one false positive —
  `.we-editor__list-graph-btn` under a "Golden Layout" comment — caught and kept)
- [x] `App.razor`: drop `css/lib` + theme links, add `css/worldengine.css?v=1`
- [x] Grep gate: no `golden-layout`, `bl-panel`, `data-gl-panel`,
  `waitForContainerReady`, or `gl-container` references remain in
  Components/Services/wwwroot JS (only historical doc mentions)
- [x] `dotnet build` clean; `dotnet test` green (89/89)

**Done when:** zero GL files, zero live references; build + tests green.
