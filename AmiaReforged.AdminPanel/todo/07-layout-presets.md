# 07 — LayoutPresetService

> Redesign §4.5. Replaces GL `saveLayout/loadLayout` JSON.

## Files

- `Services/LayoutPresetService.cs` (new)
- `Program.cs` (register scoped)
- NUnit tests

## Steps

- [x] `record EditorPreset(bool PaletteOpen, bool InspectorOpen,
  int PaletteWidth, int InspectorWidth)`.
- [x] Defaults: Regions → inspector open 340; Interaction → palette 240 +
  inspector 320; everything else → no rails.
- [x] `GetPreset(WorldEngineEntityType)` merges defaults with
  `localStorage` overrides (read via JS interop lazily, fall back to defaults
  when unavailable e.g. prerender/tests).
- [x] `SetPresetAsync(...)` writes back on splitter pointer-up / collapse.
- [x] NUnit: defaults correct per entity type, merge prefers stored values,
  missing storage → defaults (mock `IJSRuntime`).

**Done when:** presets resolve per entity type with persistence; tests pass;
nothing consumes them yet (steps 08/09 wire them).
