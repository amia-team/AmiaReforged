# Redesign todos — Golden Layout removal

> Map: `../Redesign.md`. Each file below is one shippable step.
> Work in numeric order; each step builds cleanly on the last.

| # | File | What | Depends on |
|---|---|---|---|
| 01 | `01-codex-overlay-to-tab.md` | Codex opens as a tab, not an overlay | — |
| 02 | `02-interaction-overlay-to-tab.md` | Interaction opens as a tab, not an overlay | — |
| 03 | `03-tabstrip-extract.md` | Extract inline tab bar to `TabStrip.razor` | 01, 02 |
| 04 | `04-resizable-split.md` | Build `ResizableSplit.razor` + CSS | — |
| 05 | `05-splitter-js.md` | `splitter.js`: drag + persist + ResizeObserver hook | 04 |
| 06 | `06-inspector-rail.md` | Build `Inspector.razor` right rail | — |
| 07 | `07-layout-presets.md` | `LayoutPresetService` per-entity presets | 05 |
| 08 | `08-region-graph-split.md` | Region graph → graph + inspector split | 04, 05, 06 |
| 09 | `09-interaction-split.md` | Interaction → palette \| canvas \| inspector | 02, 08 |
| 10 | `10-framework-slots.md` | Retarget `EditorFramework` slots to new shell | 06, 08 |
| 11 | `11-delete-golden-layout.md` | Delete GL JS/CSS/bridge + dead C# | 08, 09 |

Conventions per step file: goal, files touched, checkboxes, **Done when**,
verify command (`dotnet build`, `dotnet test` where applicable).

Note: the older `../todo.md` (Phases 0–2 monolith extraction) still tracks
editor extractions; these `todo/` steps track the layout replacement and
assume editor components exist or are being extracted in parallel.
