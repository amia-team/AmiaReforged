# 12 — Codex GL → splits (list | form | preview)

> Follow-up to 01 (Codex became a tab but kept its inner GL layout).
> Same split pattern as 08/09.

## Files

- `Components/Pages/WorldEngine/Editors/CodexEditor.razor`
- `Tests/Components/CodexEditorTests.cs` (2 tests updated)

## Steps

- [x] Replace `we-codex-gl-container` + `bl-panel-ce-list/form/preview` with
  nested splits: outer `First` = entity list (260px, collapsible,
  `PersistKey="codex-list"`), `Second` = inner split; inner `Second` = form
  (flex), `First` = preview rail (340px, collapsible, `SwapOrder`,
  `PersistKey="codex-preview"`)
- [x] Delete Codex GL lifecycle: `CeInitLayout`, `CeBuildDefaultLayoutConfig`,
  `CeToggleViewMenu/CeTogglePanel/CeResetLayout`, `_showViewMenu`,
  `_openPanels`, `PanelTitles`, `_bridgeModule`, `GlInstanceId`,
  bridge destroy in `CloseInternal`/`DisposeAsync`, `Task.Delay(50)` open hacks
- [x] Delete toolbar View menu (re-open panels → collapse toggles)
- [x] Keep graph-editor overlay (clue/state-machine), save/cancel/delete,
  inner list search/select — only the parent changes
- [x] Update `CodexEditorTests`: `RendersGLContainer` →
  `RendersSplitLayout_InsteadOfGLContainer`; `RendersToolbar_WithViewMenuButton`
  → `RendersToolbar_WithoutViewMenuButton`

**Done when:** lore/quest list, form, and preview render in splits with
collapse/resize; no bridge import in CodexEditor; tests green.
