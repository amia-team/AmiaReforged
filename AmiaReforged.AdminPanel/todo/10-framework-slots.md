# 10 — EditorFramework slots retarget

> Redesign §4.6. Keeps the extension model, drops GL assumptions.

## Files

- `Components/Pages/WorldEngine/EditorFramework/WorldEngineEditorExtensionSlot.cs`
- `Components/Pages/WorldEngine/EditorFramework/WorldEngineEditorCatalog.cs`
  (filter logic only if needed)
- `Components/Pages/WorldEngine/WorldEngineEditor.razor`
  (render new slots)
- `Components/Pages/WorldEngine/EditorFramework/Extensions/*`

## Steps

- [x] Add `InspectorTop`, `InspectorBottom` slot values; keep
  `ToolbarActions, SidebarHeaderActions, SidebarBeforeList, SidebarAfterList,
  SidebarFooter`.
- [x] Render `InspectorTop/Bottom` inside `Inspector` header/footer areas in
  the region + interaction presets.
- [x] Audit existing extensions (`CodexNewSidebarAction`,
  `InteractionNewSidebarAction`, `RegionGraphSidebarAction`) — confirm none
  reference GL (`typeToContainer`, `focusPanel`); fix any that do.
- [x] bUnit or build check: catalog filters by entity type + endpoint as
  before.

**Done when:** extensions render in the new shell; no extension depends on
Golden Layout APIs.
