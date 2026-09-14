# WorldEngine Editor Redesign — Drop Golden Layout

## 1. Verdict

Remove Golden Layout entirely. Replace it with a Blazor-native fixed workbench:
CSS grid + flex + one tiny splitter module. No docking library.

Golden Layout costs ~10k vendored lines (`wwwroot/js/lib/golden-layout.js`),
1100 lines of theme CSS, and a 402-line bridge (`golden-layout-bridge.js`)
to produce layouts of 2–3 panels:

- Region graph: `regiongraph` + `regionprops`
- Interaction editor: `palette` + `canvas` + `form`

That is a resizable split, not a docking problem.

## 2. What Golden Layout costs today

1. **Fights Blazor Server rendering.** Panels are pre-rendered as
   `bl-panel-*` / `data-gl-panel` divs, hidden with `display:none`, then
   absolutely positioned by GL rect events. Every resize/tab switch crosses
   the SignalR bridge (`OnPanelResized`, `OnPanelVisibilityChanged`,
   `OnPanelRemoved`) with per-frame batching to avoid spam.
2. **Fragile init.** `OnAfterRenderAsync` → `_regionGraphNeedsInit` flag →
   `InitRegionGraphGl()` → `waitForContainerReady` polling until the GL host
   has non-zero size before Cytoscape/glyph canvas can init.
3. **Two tab systems.** `WorldEngineEditorState.OpenTabs/ActiveTab` (C#) plus
   GL stacks (`addPanel/focusPanel`, View menu to re-open closed panels).
4. **Three editor modes in one file.** `WorldEngineEditor.razor` (~650 lines +
   `RegionGraph.cs` ~894 lines + `EntityPreviews.cs` ~189 lines +
   `SpecialEditors.cs` ~109 lines) branches into: normal tab mode, region-graph
   GL mode, and full-screen overlays (Interaction, Codex) that bypass tabs.
   GL unifies none of them.
5. **Dead weight.** `saveLayout/loadLayout` persistence, popouts, stack
   maximising — none of which any editor uses.

## 3. Target layout: fixed workbench

```
┌ Toolbar: endpoint | breadcrumb | ToolbarActions | Deploy ──────────┐
├─────┬──────────┬────────────────────────────────┬──────────────────┤
│ Act │ Explorer │ TabStrip + editor area         │ Inspector (opt)  │
│ bar │ list     │  (one active tab, full-bleed)  │  contextual      │
│     │ search   │                                │                  │
└─────┴──────────┴────────────────────────────────┴──────────────────┘
```

Rules:

- No arbitrary drag/drop docking, no popouts, no GL stacks.
- Each entity type gets a **preset** (which rails are open, default widths).
  The user can only resize and collapse — that covers every current use.
- One tab system: `WorldEngineEditorState`. What is open is a tab; GL plays
  no role.

### 3.1 Region → preset

| Editor | Today (GL) | New preset |
|---|---|---|
| Items, Traits, ResourceNodes, Industries, Coinhouses, Glyphs | read-only `RenderTreeBuilder` preview in tab | full form, or form + JSON preview split |
| Regions / AreaGraph | `we-region-gl-container` + `regiongraph` + `regionprops` | horizontal `ResizableSplit`: Cytoscape flex + inspector 340px (region list / region editor / area editor). Existing toolbar stays above the graph |
| Interaction | `gl-container` + palette + canvas + form | `ResizableSplit`: palette 240px (collapsible) \| canvas flex \| inspector 320px (collapsible) |
| Codex, DialogueTree | full-screen overlay, bypasses tabs | normal tab content, single scroll column |

## 4. Building blocks (~200 lines, all Blazor)

### 4.1 `DockShell.razor` — CSS grid, no JS

Toolbar / body / status rows. Body is `activity-bar | explorer | editor |
inspector`. Pure markup + `admin.css` classes.

### 4.2 `ResizableSplit.razor` — the only JS in the layout

Flex container + 6px splitter div. Pointer events for drag, `min / max /
defaultWidth / collapsible / collapsed` parameters. Persists widths to
`localStorage` via one ~60-line `splitter.js` (pointer handling +
`ResizeObserver` for canvas children only).

Replaces `golden-layout-bridge.js` wholesale.

### 4.3 `TabStrip.razor` — extract what exists

The tab-bar markup already inline in `WorldEngineEditor.razor` (lines ~547–567:
icon, title, dirty dot, close button). Extract verbatim, bind to
`WorldEngineEditorState.OpenTabs / ActiveTabId / CloseTab`.

### 4.4 `Inspector.razor` — dumb right rail

Header / body / footer slots. Region props markup and the Interaction form
move here unchanged — no logic rewrite, just a new parent.

### 4.5 `LayoutPresetService` (or extend `WorldEngineEditorState`)

`Dictionary<WorldEngineEntityType, Preset>`:

```csharp
record EditorPreset(bool PaletteOpen, bool InspectorOpen,
    int PaletteWidth, int InspectorWidth);
```

Persist `{"regions.inspectorWidth":340,
"interaction.paletteCollapsed":false}` in `localStorage`.
Replaces GL `saveLayout/loadLayout` JSON.

### 4.6 Keep `EditorFramework`, retarget slots

`WorldEngineEditorCatalog` and `EditorExtensionPoint` stay. Slot mapping:

- `ToolbarActions` → unchanged
- `SidebarHeaderActions / SidebarBeforeList / SidebarAfterList / SidebarFooter`
  → unchanged (Explorer rail)
- Add `InspectorTop / InspectorBottom` for graph/canvas editors
- Delete any GL-aware extension assumptions (no `typeToContainer`, no
  `focusPanel`)

## 5. State and interop changes

Delete from `WorldEngineEditor(.razor.cs / .RegionGraph.cs / .SpecialEditors.cs)`:

- `_regionBridgeModule`, `_regionDotNetRef`, `RegionGlInstanceId`
- `InitRegionGraphGl()`, `[JSInvokable] OnPanelRemoved / OnPanelResized /
  OnPanelVisibilityChanged`
- `ResetLayout / TogglePanel / _openPanels / _panelTitles / _showViewMenu`
- `Task.Delay(50)` open hacks for Codex/Interaction overlays
- `waitForContainerReady` polling — flex parents have size by construction

Canvas resize becomes local, not global:

```js
// owned by regiongraph.js / glyph-editor.js, not a layout bridge
canvasModule.observeResize(el, dotNetRef); // debounced, canvas editors only
```

No per-frame SignalR batching for layout drags; splitter drag is pure CSS/DOM
until pointer-up, when width is persisted.

`WorldEngineEditorState` keeps `OpenTabs / ActiveTab / ActiveEntityType /
SelectedEndpointId`. Optionally add `PaletteOpen / InspectorOpen` if
per-tab rail state is wanted; otherwise rail state lives in
`LayoutPresetService`.

## 6. Delete list — DONE (todo 14)

- Deleted: `wwwroot/js/lib/golden-layout.js`, `wwwroot/js/golden-layout-bridge.js`,
  `wwwroot/css/goldenlayout-amia-theme.css`, `wwwroot/css/lib/` (base + dark + img)
- `wwwroot/css/worldengine.css`: 112 editor rules kept, 38 GL-only rules dropped
- Scrubbed: `bl-panel-*` ids, `data-gl-panel` attributes, GL host divs,
  `waitForContainerReady` polling, and View menus from region, interaction,
  and codex editors
- `regionGraph.destroy` + GL `destroy` in `DisposeAsync`
- View menu ("re-open closed panels") in `InteractionEditor.razor`

## 7. Migration order

> Incremental steps live in `todo/` (01–11). Traverse in numeric order; each
> step builds cleanly on the last. This section is the summary map.

1. **Overlays → tabs.** Convert Codex, DialogueTree, Interaction from
   full-screen overlays to normal tabs. Kills the 3-mode branch in
   `WorldEngineEditor.razor` (overlay vs. GL mode vs. tab mode). No GL touched.
2. **Region graph → split.** Build `ResizableSplit` + `Inspector`, move region
   list/editor/area editor markup into the inspector, Cytoscape into the flex
   child. Delete region GL lifecycle (`InitRegionGraphGl`,
   `_regionGraphNeedsInit`, bridge module). `regiongraph.js` keeps its Cytoscape
   code; it just gets a flex parent with a real size instead of a polled GL rect.
3. **Interaction → split.** Move palette/canvas/form into
   palette | canvas | inspector. Delete interaction GL host + View menu +
   `golden-layout-bridge.js` + theme CSS.
4. **Previews → editors.** Promote inline `RenderEntityEditor`
   (`WorldEngineEditor.EntityPreviews.cs`) read-only previews to real
   `Editors/*.Editor.razor` forms. Already scoped as IMPROVEMENT_PLAN 1.4;
   the new shell's `@switch (tab.EntityType)` stays, but each arm renders a
   component instead of a `RenderTreeBuilder` method.

Steps 1–3 are independently shippable; step 4 rides on the IMPROVEMENT_PLAN
Phase 1 extractions.

## 8. What good looks like when done

- Zero vendored layout JS. One ~60-line splitter module owned by us.
- No layout traffic on SignalR during drags; one `localStorage` write on
  pointer-up.
- One tab system (`WorldEngineEditorState`), one way to open an editor
  (open tab), one way to close it (close tab).
- `WorldEngineEditor.razor` back to a thin shell: toolbar + activity bar +
  explorer + tab strip + `@switch` to editor components + inspector.
- Canvas editors (`regiongraph.js`, `glyph-editor.js`) init against a flex div
  that already has dimensions — no polling, no `OnAfterRenderAsync` flag dance.

## 9. If docking is ever really needed

Use Dockview (maintained, framework-agnostic core), not Golden Layout v1/v2
(dead upstream, hostile to Blazor's diffing via its virtual-component mode).
Nothing in the current editors (max 3 panels, no popouts, no saved
multi-stack arrangements) justifies it today.
