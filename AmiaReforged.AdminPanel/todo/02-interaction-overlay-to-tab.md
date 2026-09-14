# 02 — Interaction overlay → tab

> Redesign §3.1, §7.1. Same treatment as 01, for `InteractionEditor`.

## Files

- `Components/Pages/WorldEngine/WorldEngineEditor.razor`
- `Components/Pages/WorldEngine/WorldEngineEditor.SpecialEditors.cs`
- `Components/Pages/WorldEngine/InteractionEditor.razor`

## Steps

- [x] Remove full-screen overlay branch
  (`<InteractionEditor @ref=...>` fixed-position wrapper).
- [x] Route Interactions through tabs: list click opens/activates an
  `EditorTab`; new-interaction action opens a tab with a new-entity key.
- [x] Render `<InteractionEditor>` from the tab switch arm (tag param in,
  `OnClosed`/`OnSave` out) — no `@ref OpenCreate()/OpenEdit()` + `Close()`
  dance from the parent.
- [x] Simplify `_interactionEditorOpen / _interactionEditorIsCreating /
  _interactionEditorTag` to tab state; update `_canDeploy`,
  `OpenDeployDialog`, `OnActivityBarClick` branches that special-case the
  overlay.
- [x] Keep the *inner* GL host of `InteractionEditor` untouched here —
  step 09 replaces it.

**Done when:** interactions open, edit, save, close as tabs; overlay
`position:fixed` wrapper gone; `dotnet build` clean.
