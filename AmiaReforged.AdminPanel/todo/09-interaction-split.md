# 09 — Interaction → palette | canvas | inspector

> Redesign §3.1, §7.3. Second and last GL consumer removed.

## Files

- `Components/Pages/WorldEngine/InteractionEditor.razor`
- `wwwroot/js/glyph-editor.js` (init/resize only)

## Steps

- [x] Replace `gl-container` + `bl-panel-palette/canvas/form` with nested
  splits: palette 240px (collapsible) | canvas flex | inspector 320px
  (collapsible, holds Interaction Properties form verbatim).
- [x] Delete interaction GL lifecycle: bridge import, `addPanel/removePanel/
  focusPanel`, `_openPanels/_panelTitles`, View menu (re-open closed panels
  → replaced by collapse toggles), `ResetLayout`.
- [x] Glyph canvas inits against the flex div; resize via `observeResize`.
- [x] Keep palette search/grouping, drag-to-add (`application/glyph-node`),
  toolbar (Fit All / Arrange / Clear Script), save/cancel — only the parent
  changes.

**Done when:** palette, canvas, and form render in splits with
collapse/resize; View menu gone; no GL import in InteractionEditor.
