# 13 — Wire LayoutPresetService into splits

> 07 built the service but nothing consumed it. Collapse state now persists
> per entity type; widths were already persisted by splitter.js `PersistKey`s.

## Files

- `Components/Shared/ResizableSplit.razor` (+ 2 bUnit tests)
- `Components/Pages/WorldEngine/WorldEngineEditor.razor(.cs, .RegionGraph.cs)`
- `Components/Pages/WorldEngine/InteractionEditor.razor`
- `Tests/Components/InteractionEditorTests.cs` (DI for new dependency)

## Steps

- [x] `ResizableSplit`: rename private collapse flags, add
  `FirstCollapsedInitial` / `SecondCollapsedInitial` params (applied in
  `OnInitialized`); existing `OnFirst/SecondCollapsedChanged` events report back
- [x] Region inspector: load `Regions` preset in `OpenRegionGraph`
  (`_rgInspectorCollapsed`, `_rgInspectorWidth`), persist on toggle via
  `OnRgInspectorCollapsedChanged`
- [x] Interaction palette + inspector: load `Interactions` preset in
  `OnInitializedAsync`, persist per-rail via `OnPalette/InspectorCollapsedChanged`
- [x] Codex splits: widths persist via `PersistKey`; collapse left static
  (rails are content-driven, no preset record fits — documented limitation)
- [x] Tests: `StartsCollapsed_WhenInitialFlagsSet`, `AppliesSwapOrderClass`;
  `LayoutPresetService` (JS-throwing mock → defaults) in `InteractionEditorTests`

**Done when:** rail collapse survives reload per entity type; tests green.
