# 01 — Codex overlay → tab

> Redesign §3.1, §7.1. Makes `CodexEditor` behave like every other editor.

## Files

- `Components/Pages/WorldEngine/WorldEngineEditor.razor`
- `Components/Pages/WorldEngine/WorldEngineEditor.SpecialEditors.cs`
- `Components/Pages/WorldEngine/Editors/CodexEditor.razor`
- `Services/WorldEngineEditorState.cs` (read-only)

## Steps

- [x] Remove `_codexEditorOpen` / `_codexEditorRef` overlay branch
  (`@if (_codexEditorOpen)` in the editor area).
- [x] Route Codex through tabs: `OnListItemClick` for `Codex` opens/activates
  an `EditorTab` with `EntityKey` + sub-type (Lore vs Quest derived from key
  prefix, as today); `OpenNewCodexEditor` opens a tab with a new-entity key.
- [x] Render `<CodexEditor>` from the `@switch (tab.EntityType)` arm using
  `_tabData` / direct key param — no `@ref OpenNewAsync/OpenExistingAsync`
  with `Task.Delay(50)`.
- [x] Delete `OpenCodexEditor`, `OpenNewCodexEditor`, `CloseCodexEditor`
  overlay helpers in `SpecialEditors.cs`.
- [x] Verify Deploy (`_canDeploy`) and entity-list refresh still work from the
  tab path (`OnEntityListRefresh` → `LoadEntityList(reset: true)`).

**Done when:** lore + quest open, edit, save, and close as tabs; no
`_codexEditorOpen` remains; `dotnet build` clean.
