# AdminPanel: WorldEngine Editor Improvement Plan

> Bite-sized, partitionable tasks. Each task is independent, has clear scope, and can be completed in isolation by any developer familiar with the codebase.

The goal is a single unified editor. Standalone CRUD pages (`Items.razor`, `ResourceNodes.razor`, `Regions.razor`, `Traits.razor`, `Interactions.razor`, `Coinhouses.razor`) will be absorbed and deleted — no point maintaining two editing UIs for the same entities.

---

## Phase 0 — Foundation (shared infrastructure)

### Task 0.1 — Extract HTTP helpers into `ApiServiceBase`

> **Files**: `Services/ApiServiceBase.cs` (new), all 17 `Services/*ApiService.cs` (refactor)
> **Estimate**: 1 session

Copy the shared HTTP plumbing (`ResolveEndpointAsync`, `CreateRequest`, `GetAsync<T>`, `PostAsync<T>`, `PutAsync<T>`, `DeleteRequestAsync`, `EnsureSuccessOrThrow`) into a base class. Each API service inherits from it and keeps only the endpoint-specific methods (`GetAllAsync`, `GetByTagAsync`, `CreateAsync`, etc.).

**Done when**: Every API service compiles and works with `: ApiServiceBase`, no HTTP code duplicated.

---

### Task 0.2 — Extract entity list panel into reusable component

> **Files**: `WorldEngineEditor.razor` (remove entity list markup + state), `Components/Shared/EntityListPanel.razor` (new)
> **Estimate**: 1 session

The editor's entity list panel (search bar + paginated list + "Load more" button + loading/error/empty states) is self-contained logic that doesn't need to be embedded in the editor itself. Extract it so the same component can be used when adding new entity editors.

Signature:
```razor
<EntityListPanel Items="@items" HasMore="@hasMore" Loading="@loading"
                 Search="@search" OnSearch="@handler"
                 OnLoadMore="@handler" OnItemClick="@handler" />
```

Where `EntityListPanel` takes `IReadOnlyList<EntityListItem>` and renders each as a clickable row with name + key.

**Done when**: Entity list renders from its own component with no behavior change.

---

### Task 0.3 — Add validation attributes to DTOs

> **Files**: `Models/WorldEngineDtos.cs`
> **Estimate**: 0.5 session

Add `System.ComponentModel.DataAnnotations` attributes to every DTO property that has constraints:

- `[Required]` for mandatory fields (Tag, Name, QuestId, etc.)
- `[StringLength(max)]` for bounded fields (ResRef, Description)
- `[Range(min, max)]` for numeric fields (BaseValue, StoredGold, RequiredCount)

**Done when**: All DTOs have appropriate annotations; forms can use `EditForm` + `DataAnnotationsValidator` instead of manual save guards.

---

### Task 0.4 — Move inline styles to CSS classes

> **Files**: `wwwroot/css/admin.css`, all `.razor` files
> **Estimate**: 1 session

Find the most repeated `style="..."` patterns across the editor and extract them:
- `.tag-badge` for removable tag chips (industry tags, area refs, knowledge tags)
- `.panel-section`, `.panel-section-title` for collapsible form sections
- `.toolbar-row`, `.toolbar-spacer`
- `.entity-row`, `.entity-row-name`, `.entity-row-key` for list items
- `.empty-state`, `.loading-state`
- `.form-row-compact` for the 3-column inline form layout used everywhere

Don't boil the ocean — just target patterns that appear 5+ times in `WorldEngineEditor.razor`.

**Done when**: No `.razor` file has more than 3 inline `style` attributes per page; the editor is <inline style count of 50 (currently seems 200+).

---

## Phase 1 — Extract editors from monolithic file

### Task 1.1 — Extract `InteractionEditor` component

> **Files**: `WorldEngineEditor.razor` (remove ~800 lines), `Editors/InteractionEditor.razor` (new)
> **Estimate**: 1 session

Pull all `_ie*` state fields, the interaction editor markup (`@if (_interactionEditorOpen)` block, all GoldenLayout panels), and all `Ie*` methods into a standalone component.

```razor
<InteractionEditor Interaction="@item" AvailableIndustries="@industries"
                   NodeCatalog="@catalog" OnSave="@handler" OnCancel="@handler" />
```

**Done when**: Interaction editor renders and functions identically from its own component.

---

### Task 1.2 — Extract `CodexEditor` component

> **Files**: `WorldEngineEditor.razor` (remove ~700 lines), `Editors/CodexEditor.razor` (new)
> **Estimate**: 1 session

Pull all `_ce*` state fields, the codex editor markup (`@if (_codexEditorOpen)` block, all GL panels for lore + quest forms), and all `Ce*` methods into a standalone component.

```razor
<CodexEditor LoreApi="@LoreApi" QuestApi="@QuestApi"
             OnSave="@handler" OnCancel="@handler" />
```

**Done when**: Lore and quest CRUD works identically from its own component.

---

### Task 1.3 — Extract `RegionGraphEditor` component

> **Files**: `WorldEngineEditor.razor` (remove ~2,000 lines), `Editors/RegionGraphEditor.razor` (new)
> **Estimate**: 1-2 sessions

Pull all:
- `_rg*` state fields (~40 fields)
- `RgPanelMode` enum and all region-graph-specific inner types
- Region graph markup (region list, region editor, area editor, import/export, graph canvas)
- All `Rg*` / `RegionGraph*` methods
- JS interop (`_regionBridgeModule`, `_regionDotNetRef`)
- Static arrays (`_rgClimateOptions`, `_rgQualityOptions`, `_rgPoiTypeOptions`)
- `RgCamelCase` JSON options

**Done when**: Region graph renders and functions identically from its own component.

---

### Task 1.4 — Extract remaining entity form editors

> **Files**: `WorldEngineEditor.razor` (remove inline forms), 7 new `Editors/*.razor` files
> **Estimate**: 2 sessions

| Component | What it renders |
|---|---|
| `ItemEditor.razor` | Item blueprint fields (ResRef, Tag, Name, Materials, Appearance, etc.) |
| `ResourceNodeEditor.razor` | Node definition (Tag, PlcAppearance, Uses, Outputs, Flora/Tree props) |
| `RegionEditor.razor` | Region definition + areas + environment (non-graph form) |
| `TraitEditor.razor` | Trait definition (Tag, Name, Cost, Effects, restrictions) |
| `IndustryEditor.razor` | Industry definition (knowledge tree, recipes, ingredients) |
| `CoinhouseEditor.razor` | Coinhouse fields (Tag, Settlement, StoredGold, etc.) |
| `DialogueEditor.razor` | Dialogue tree (uses existing `DialogueTreeEditor` sub-component) |

Each follows the same pattern:
```razor
[Parameter] public TDto Item { get; set; }
[Parameter] public EventCallback<TDto> OnSave { get; set; }
[Parameter] public EventCallback OnCancel { get; set; }
```

**Done when**: The editor's `@switch` block delegates to these components and the originals no longer exist inline.

---

### Task 1.5 — Flatten `WorldEngineEditor.razor` to a thin shell

> **Files**: `WorldEngineEditor.razor` (rewrite, ~5,412 → ~300 lines)
> **Estimate**: 0.5 session

After Tasks 0.2, 1.1–1.4, the editor becomes:

```
Toolbar (endpoint select, breadcrumb, deploy button)
Activity bar (entity type icons)
EntityListPanel (extracted in 0.2)
Editor area:
    @switch (tab.EntityType)
    {
        Items:          <ItemEditor ... />
        ResourceNodes:  <ResourceNodeEditor ... />
        Regions:        <RegionEditor ... />  (or RegionGraphEditor in graph mode)
        Codex:          <CodexEditor ... />
        Traits:         <TraitEditor ... />
        Industries:     <IndustryEditor ... />
        Interactions:   <InteractionEditor ... />
        Coinhouses:     <CoinhouseEditor ... />
        Dialogues:      <DialogueEditor ... />
        AreaGraph:      (read-only — keep inline or extract)
        Glyphs:         (link to full /worldengine/glyphs)
    }
```

No inline entity forms remain. State is limited to UI orchestration (tabs, list, endpoint selection).

**Done when**: File compiles cleanly under 350 lines and all editors work.

---

## Phase 2 — Absorb standalone pages into the editor, then delete them

### Task 2.1 — Audit standalone pages for editor gaps

> **Files**: `Items.razor`, `ResourceNodes.razor`, `Regions.razor`, `Traits.razor`, `Interactions.razor`, `Coinhouses.razor`
> **Estimate**: 0.5 session

Read each standalone page and list every feature the unified editor doesn't support. Known candidates:

| Page | Feature to check |
|---|---|
| `Items.razor` | Import JSON, Export JSON, template badge, enums endpoint |
| `ResourceNodes.razor` | Any filtering or fields unique to standalone |
| `Regions.razor` | (already superseded by editor's region graph — verify) |
| `Traits.razor` | Race/class restriction lists, conflicting/prerequisite traits |
| `Interactions.razor` | (already superseded by editor's interaction editor — verify) |
| `Coinhouses.razor` | EngineId parsing, persona ID string format |

**Done when**: A checklist exists of features that need to be backfilled into the editor.

---

### Task 2.2 — Backfill missing features into the editor

> **Files**: `Editors/ItemEditor.razor`, `Editors/CoinhouseEditor.razor`, etc. (depending on audit)
> **Estimate**: 1 session

Add each missing feature from Task 2.1 to the appropriate `Editors/*.razor` file. Most will be adding form fields, import/export buttons, or validation that the standalone page had but the inline editor form didn't.

**Done when**: Every feature from the standalone pages is available in the unified editor.

---

### Task 2.3 — Add missing entity types to the editor

> **Files**: `Models/WorldEngineEntityType.cs` (add values), `Editors/OrganizationEditor.razor`, `Editors/WorkstationEditor.razor`, `Editors/RecipeTemplateEditor.razor` (new), `WorldEngineEditor.razor` (add cases)
> **Estimate**: 1 session

Three entity types have API services but no editor entry:

| Entity | API Service | DTO |
|---|---|---|
| Organizations | `OrganizationApiService` | `OrganizationDto` |
| Workstations | `WorkstationApiService` | `WorkstationDefinitionDto` |
| Recipe Templates | `RecipeTemplateApiService` | `RecipeTemplateDefinitionDto` |

Add each to `WorldEngineEntityType` enum, create a simple editor component (list + form), and wire into the editor switch.

**Done when**: All three can be browsed and edited in the unified editor.

---

### Task 2.4 — Delete standalone pages and redirect routes

> **Files**: `Items.razor`, `ResourceNodes.razor`, `Regions.razor`, `Traits.razor`, `Interactions.razor`, `Coinhouses.razor`, `NavMenu.razor`
> **Estimate**: 0.5 session

For each page:
1. Delete the `.razor` file
2. Remove the `@page` route (already covered by deleting the file)
3. Update `NavMenu.razor` to point its link to `/worldengine/editor`

Optionally add a redirect: `WorldEngine/Editor` can interpret query params like `?entity=Items` to auto-select an entity type on load, so old bookmarks still work.

**Done when**: All standalone pages deleted; NavMenu links go to the unified editor; no 404s for old routes.

---

### Task 2.5 — NavMenu final cleanup

> **Files**: `Components/Layout/NavMenu.razor`
> **Estimate**: 0.25 session

After deleting standalone pages, the World Engine sub-nav becomes:

```
World Engine
  ▸ Editor  (the single entry point)
```

Optionally keep sub-nav links that jump directly to a specific entity type in the editor via query params (`/worldengine/editor?type=Items`). But the minimal version is just one link.

**Done when**: NavMenu has one WorldEngine link ("Editor") or direct-links to entity types in the editor.

---

## Task dependency map

```
0.1 (ApiServiceBase)

0.2 (EntityListPanel) ─── used by 1.5

0.3 (DTO validation) ──── used by all editor forms in 1.4
                          (low-priority — can be deferred without blocking)

0.4 (CSS cleanup) ─────── can be done anytime in parallel

Phase 1 tasks are independent of each other and can run in parallel.

1.1 (InteractionEditor)──┐
1.2 (CodexEditor)────────┤
1.3 (RegionGraphEditor)──┤── 1.5 (flatten WorldEngineEditor)
1.4 (entity editors)─────┘

Phase 2 depends on Phase 1:

2.1 (audit gaps) ──┐
                   ├── 2.2 (backfill) ──┐
                                         ├── 2.4 (delete pages)
2.3 (missing types)─────────────────────┘
                              └── 2.5 (NavMenu cleanup)
```

## Effort summary

| Phase | Tasks | Sessions | Parallelizable |
|---|---|---|---|
| 0 — Foundation | 4 | 3.5 | Partially (0.1 independent, 0.4 independent) |
| 1 — Extract editors | 5 | 5-6 | Yes (all 4 extractions are independent of each other) |
| 2 — Absorb + delete | 5 | 3.75 | Sequentially dependent |
| **Total** | **14** | **~12-13 sessions** | |
