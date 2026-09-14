# 05 — splitter.js: drag + persist + canvas hook

> Redesign §4.2 (JS half), §5. The only layout JS we own (~60 lines).

## Files

- `wwwroot/js/splitter.js` (new, ES module)
- `Components/Shared/ResizableSplit.razor` (wire up)

## Steps

- [x] `initSplit(el, options)` — pointer events on the splitter div, update
  pane `flex-basis` during drag (pure DOM, no SignalR per frame).
- [x] On pointer-up, write width to `localStorage["we-split:"+persistKey]`;
  on init, read it back.
- [x] Export `observeResize(el, dotNetRef, methodName, debounceMs=200)` for
  canvas editors (region Cytoscape container, glyph canvas) — single debounced
  callback, replacing the GL bridge's per-frame `OnPanelResized` batching.
- [x] `ResizableSplit` calls `initSplit` in `OnAfterRenderAsync` (first render
  only) via `IJSRuntime`; disposes observer on `Dispose`.
- [x] No Blazor re-render during drag (JS touches style directly).

**Done when:** drag resizes, widths survive reload, canvas hook fires at most
once per 200ms during resizes; no C# event per mousemove.
