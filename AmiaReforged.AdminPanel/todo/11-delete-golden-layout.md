# 11 — Scrub dead Golden Layout references (rescoped)

> Redesign §6. Rescoped during execution: full deletion is blocked because
> `Editors/CodexEditor.razor` still runs its inner GL layout (no todo converts
> it), and `goldenlayout-amia-theme.css` hosts the entire `we-editor` stylesheet
> (not just GL theme). Deleting those files now would break Codex + all editor
> styling. Follow-up: convert Codex to splits (same pattern as 08/09), then delete.

## Kept (still required by Codex)

- `wwwroot/js/lib/golden-layout.js`
- `wwwroot/js/golden-layout-bridge.js`
- `wwwroot/css/goldenlayout-amia-theme.css`
- `wwwroot/css/lib/` GL base/dark theme + img
- `bl-panel-*` ids / `data-gl-panel` / `CeInitLayout` inside
  `Editors/CodexEditor.razor` only

## Scrubbed

- [x] `WorldEngineEditor.razor(.cs)`, `WorldEngineEditor.RegionGraph.cs`,
  `InteractionEditor.razor`: no `bl-panel-*`, `data-gl-panel`, `__gl-host`,
  `gl-container`, bridge imports, or `waitForContainerReady` remain
  (verified via grep; one stale "Golden Layout Lifecycle" comment renamed)
- [x] Only remaining `golden-layout-bridge` importer is `CodexEditor.razor`
- [x] `dotnet build` clean; `dotnet test` green (87/87)
- [ ] Manual smoke (needs running server + browser — not done here):
  open each editor, resize rails, reload (widths persist),
  switch tabs, deploy dialog still opens

**Done when:** no dead GL references outside Codex; build + tests green.
Full file deletion moves to the Codex-conversion follow-up.
