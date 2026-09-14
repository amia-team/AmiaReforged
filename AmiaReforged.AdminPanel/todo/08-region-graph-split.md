# 08 — Region graph → split + inspector

> Redesign §3.1, §7.2. First real GL consumer removed.

## Files

- `Components/Pages/WorldEngine/WorldEngineEditor.razor`
  (region-graph branch)
- `Components/Pages/WorldEngine/WorldEngineEditor.RegionGraph.cs`
- `wwwroot/js/regiongraph.js` (shrink init only)

## Steps

- [x] Replace `we-region-gl-container` + `bl-panel-regiongraph` +
  `bl-panel-regionprops` with:
  `<ResizableSplit>` → graph flex child (`#region-cy` + toolbar + info bar)
  + `<Inspector>` (region list / region editor / area editor markup moved
  verbatim).
- [x] Delete region GL lifecycle: `_regionBridgeModule`, `_regionDotNetRef`,
  `RegionGlInstanceId`, `InitRegionGraphGl()`, `_regionGraphNeedsInit`,
  `[JSInvokable]` panel callbacks, `OnAfterRenderAsync` flag dance.
- [x] Init Cytoscape directly against the flex div (it has size by
  construction — no `waitForContainerReady` polling); wire canvas resize via
  `splitter.js observeResize` (step 05) if needed.
- [x] Keep all `Rg*` editing logic, import/export modal, toolbar — only the
  parent changes.
- [x] Update `DisposeAsync`: destroy Cytoscape only, no GL destroy.

**Done when:** region graph renders graph + inspector with resize/collapse;
no `golden-layout-bridge.js` import for regions; no GL polling.
