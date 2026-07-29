# AdminPanel WorldEngine Editor Refactoring

> Refactor the monolithic `WorldEngineEditor.razor` (~5,400 lines) into a modular component architecture, then absorb and delete redundant standalone CRUD pages.
>
> Source: `IMPROVEMENT_PLAN.md` | Total effort: ~14-15 sessions (including testing)

**Current state**: Phase 0 complete, Tasks 1.1 and 1.2 complete. `WorldEngineEditor.razor` reduced from 5,379 → 2,487 lines (54%). **67 tests passing**, 0 failures.

### Known Issues

_(none currently)_

---

## Testing Strategy

> **Framework**: NUnit 4.x + bUnit + Moq + FluentAssertions (matches codebase conventions from `WorldSimulator.Tests`)
> **Project**: `AmiaReforged.AdminPanel.Tests/` (new, added to `AmiaReforged.sln`)
> **Approach**: Tests are written alongside each refactoring task — not as a separate phase. Each extraction task includes its own test sub-steps.

### What we test

| Layer | Tool | What to verify |
|---|---|---|
| **API services** | NUnit + Moq + `HttpMessageHandler` mock | `ApiServiceBase` HTTP plumbing; each service's CRUD methods send correct requests and deserialize responses |
| **EditorState** | NUnit (pure C#) | Tab open/close/dirty logic, endpoint selection, event firing |
| **DTO validation** | NUnit + `System.ComponentModel.DataAnnotations` | `[Required]`, `[StringLength]`, `[Range]` attributes reject invalid data |
| **Blazor components** | bUnit + Moq | Each extracted editor renders correctly, fires `OnSave`/`OnCancel`, handles parameter changes |
| **Integration** | bUnit | `WorldEngineEditor.razor` thin shell delegates to correct child component per entity type |

### What we don't test

- CSS class extraction (Task 0.4) — visual regression, not unit-testable
- GoldenLayout JS interop — requires full browser context
- NavMenu route cleanup (Task 2.5) — trivial link changes

---

## Phase 0 — Foundation (shared infrastructure)

> These tasks create the building blocks used by all later work. Tasks 0.1 and 0.4 are independent; 0.2 and 0.3 can run in parallel with each other.

### Task 0.0 — Set up test project `[parallel]` ✅

**Files**: `AmiaReforged.AdminPanel.Tests/AmiaReforged.AdminPanel.Tests.csproj` (new), `AmiaReforged.sln` (add reference)
**Estimate**: 0.5 session

- [x] Create `AmiaReforged.AdminPanel.Tests/` directory
- [x] Create `AmiaReforged.AdminPanel.Tests.csproj` targeting `net8.0` with packages:
  - `Microsoft.NET.Test.Sdk` 17.11.0
  - `NUnit` 4.4.0
  - `NUnit3TestAdapter` 4.6.0
  - `NUnit.Analyzers` 4.5.0
  - `FluentAssertions` 8.8.0
  - `bUnit` 1.33.3
  - `Moq` 4.20.72
  - `coverlet.collector` 6.0.0
  - `JunitXml.TestLogger` 4.1.0
- [x] Add `<ProjectReference>` to `../AmiaReforged.AdminPanel/AmiaReforged.AdminPanel.csproj`
- [x] Add project to `AmiaReforged.sln`
- [x] Create directory structure:
  ```
  Tests/
    Services/          # API service tests
    Components/        # bUnit component tests
    Models/            # DTO validation tests
  ```
- [x] Verify: `dotnet test` runs (0 tests, builds clean)

**Done when**: `dotnet build` and `dotnet test` succeed. Test project is in the solution.

---

### Task 0.1 — Extract HTTP helpers into `ApiServiceBase` `[parallel]` ✅

**Files**: `Services/ApiServiceBase.cs` (new), all 17 `Services/*ApiService.cs` (refactor)
**Estimate**: 1 session

- [x] Create `Services/ApiServiceBase.cs` with shared HTTP plumbing:
  - `ResolveEndpointAsync`, `CreateRequest`, `GetAsync<T>`, `PostAsync<T>`, `PutAsync<T>`, `DeleteRequestAsync`, `EnsureSuccessOrThrow`, `FetchExportJsonAsync`
  - Standardized `bool rawJson = false` parameter on PostAsync (replaced 5 services' `bool raw` variants)
- [x] Refactor each `*ApiService.cs` to inherit from `ApiServiceBase`, keeping only endpoint-specific methods (`GetAllAsync`, `GetByTagAsync`, `CreateAsync`, etc.)
- [x] Services refactored (17 total): ItemApiService, RegionApiService, TraitApiService, ResourceNodeApiService, InteractionApiService, IndustryApiService, LoreApiService, QuestApiService, CoinhouseApiService, DialogueApiService, OrganizationApiService, WorkstationApiService, RecipeTemplateApiService, GlyphApiService, EncounterApiService, AreaGraphApiService, DependencyGraphApiService
- [x] Deleted `EncounterApiException` (unified into `WorldEngineApiException`)
- [x] Verify: every API service compiles with `: ApiServiceBase`, no HTTP code duplicated

#### Tests for Task 0.1

- [x] Create `Tests/Services/ApiServiceBaseTests.cs`
- [x] Test `EnsureSuccessOrThrow` — throws `WorldEngineApiException` on non-success with parsed error body
- [x] Test `EnsureSuccessOrThrow` — throws with raw body when JSON parse fails
- [x] Test `EnsureSuccessOrThrow` — no throw on 2xx responses
- [x] Test `CreateRequest` — sets `X-API-Key` header and correct URI
- [x] Test `ResolveEndpointAsync` — throws when no endpoint selected
- [x] Test `ResolveEndpointAsync` — throws when endpoint not found
- [x] Test `ResolveEndpointAsync` — throws when endpoint has no API key
- [x] Test `ResolveEndpointAsync` — returns correct base URI and trimmed API key
- [x] Create `Tests/Services/ItemApiServiceTests.cs` (representative service test)
- [x] Test `GetAllAsync` — sends GET to correct URL with query params
- [x] Test `GetByTagAsync` — sends GET to `/api/worldengine/items/{tag}`
- [x] Test `CreateAsync` — sends POST with serialized body
- [x] Test `UpdateAsync` — sends PUT to correct URL
- [x] Test `DeleteAsync` — sends DELETE to correct URL
- [x] Test `ExportJsonAsync` — returns pretty-printed JSON
- [x] Mock `IHttpClientFactory` and `IWorldEngineEndpointService` using Moq + `HttpMessageHandler` mock

**Done when**: All 17 API services compile inheriting `ApiServiceBase`. `ApiServiceBaseTests` + `ItemApiServiceTests` pass (17 test cases).

---

### Task 0.2 — Extract entity list panel into reusable component ✅

**Files**: `WorldEngineEditor.razor` (remove entity list markup + state), `Components/Shared/EntityListPanel.razor` (new), `Models/WorldEngineEntityType.cs` (moved `EntityListItem` record)
**Estimate**: 1 session

- [x] Create `Components/Shared/EntityListPanel.razor` with this signature:
  ```razor
  <EntityListPanel Items="@items" HasMore="@hasMore" Loading="@loading"
                   Search="@search" OnSearch="@handler"
                   OnLoadMore="@handler" OnItemClick="@handler" />
  ```
- [x] Component renders `IReadOnlyList<EntityListItem>` as clickable rows with name + key
- [x] Component handles: search bar, paginated list, "Load more" button, loading/error/empty states
- [x] Moved `EntityListItem` record to `Models/WorldEngineEntityType.cs`
- [x] Updated `Components/_Imports.razor` with `@using AmiaReforged.AdminPanel.Components.Shared`
- [x] Remove entity list markup and state from `WorldEngineEditor.razor`
- [x] Wire `WorldEngineEditor.razor` to use the new `EntityListPanel` component

#### Tests for Task 0.2

- [x] Create `Tests/Components/EntityListPanelTests.cs` (bUnit)
- [x] Test: renders list items with name and key
- [x] Test: renders empty state when items list is empty
- [x] Test: renders loading state when `Loading=true`
- [x] Test: shows "Load more" button when `HasMore=true`, hides when false
- [x] Test: fires `OnSearch` callback when search input changes
- [x] Test: fires `OnLoadMore` when "Load more" clicked
- [x] Test: fires `OnItemClick` with correct item when row clicked
- [x] Test: search input reflects `Search` parameter

**Done when**: Entity list renders from its own component. 10 bUnit tests pass.

---

### Task 0.3 — Add validation attributes to DTOs ✅

**Files**: `Models/WorldEngineDtos.cs`
**Estimate**: 0.5 session

- [x] Add `[Required]` for mandatory fields: Tag, Name, QuestId, LoreId, Title, etc.
- [x] Add `[StringLength(max)]` for bounded fields: ResRef, Description, Keywords, etc.
- [x] Add `[Range(min, max)]` for numeric fields: BaseValue, StoredGold, RequiredCount, Xp, Gold, etc.
- [x] DTOs annotated: ItemBlueprintDto, CoinhouseDto, TraitDefinitionDto, RegionDefinitionDto, LoreDefinitionDto, QuestDefinitionDto, InteractionDefinitionDto, IndustryDefinitionDto, WorkstationDefinitionDto, OrganizationDto, DialogueTreeDto
- [x] Verify forms can use `EditForm` + `DataAnnotationsValidator` instead of manual save guards

#### Tests for Task 0.3

- [x] Create `Tests/Models/DtoValidationTests.cs`
- [x] Test: `ItemBlueprintDto` rejects empty `Tag` (`[Required]`)
- [x] Test: `ItemBlueprintDto` rejects `Tag` exceeding max length (`[StringLength]`)
- [x] Test: `ItemBlueprintDto` rejects negative `BaseValue` (`[Range]`)
- [x] Test: `CoinhouseDto` rejects empty `Tag` and `Settlement`
- [x] Test: `CoinhouseDto` rejects `StoredGold` below 0 (`[Range(0, ...)]`)
- [x] Test: `TraitDefinitionDto` rejects empty `Tag` and `Name`
- [x] Test: `LoreDefinitionDto` rejects empty `Title`
- [x] Test: `QuestDefinitionDto` rejects empty `QuestId` and `Title`
- [x] Additional validation tests across 11 DTOs

**Done when**: All DTOs have `[DataAnnotations]` attributes. 18 validation tests pass.

---

### Task 0.4 — Move inline styles to CSS classes `[parallel]` ⏸️ DEFERRED

**Files**: `wwwroot/css/admin.css`, all `.razor` files
**Estimate**: 1 session

- [ ] Audit `WorldEngineEditor.razor` for repeated inline `style="..."` patterns (currently 443+ lines with inline styles)
- [ ] Extract `.tag-badge` class for removable tag chips (industry tags, area refs, knowledge tags)
- [ ] Extract `.panel-section`, `.panel-section-title` for collapsible form sections
- [ ] Extract `.toolbar-row`, `.toolbar-spacer` for toolbar layouts
- [ ] Extract `.entity-row`, `.entity-row-name`, `.entity-row-key` for list items
- [ ] Extract `.empty-state`, `.loading-state` for status displays
- [ ] Extract `.form-row-compact` for the 3-column inline form layout
- [ ] Target patterns appearing 5+ times in `WorldEngineEditor.razor`
- [ ] Verify: no `.razor` file has more than 3 inline `style` attributes per page
- [ ] Verify: inline style count drops from 443+ to under 50 lines

**Done when**: Repeated inline styles replaced with CSS classes. Editor has <50 inline style attributes.

> **Note**: Deferred because CSS extraction is visual regression and not unit-testable. Will be revisited after all editor extractions are complete.

---

## Phase 1 — Extract editors from monolithic file

> All 4 extraction tasks (1.1–1.4) are independent and can run in parallel. Task 1.5 depends on all of them completing. Each includes bUnit tests for the extracted component.

### Task 1.1 — Extract InteractionEditor component `[parallel]` ✅

**Files**: `WorldEngineEditor.razor` (remove ~1,157 lines), `InteractionEditor.razor` (pre-existing standalone component, wired via `@ref`)
**Estimate**: 1 session

- [x] Discovered existing standalone `InteractionEditor.razor` (1,221 lines) already at root level with full GL lifecycle, script editor, save/cancel, inner types, dispose
- [x] Added `Close()` public method to standalone component for force-closing from parent
- [x] Added `<InteractionEditor @ref="_interactionEditorRef" AvailableIndustries="..." OnClosed="HandleInteractionEditorClosed" />` to parent markup
- [x] Replaced `OpenNewInteractionEditor` / `OpenInteractionEditor` with delegation to `_interactionEditorRef.OpenCreate()` / `_interactionEditorRef.OpenEdit()`
- [x] Added `HandleInteractionEditorClosed` callback to handle save/cancel results
- [x] Removed all `_ie*` state fields, inner types (79 lines)
- [x] Removed all `Ie*` methods (GL layout, script editor, callbacks, view menu, palette, validity, response/effect, save/cancel, helpers — ~650 lines)
- [x] Removed interaction markup block (~440 lines of inline GL editor markup)
- [x] Cleaned up `DisposeAsync`, `OnPanelResized`, `OnPanelRemoved` (removed interaction-specific branches)
- [x] Updated `_canDeploy`, `OpenDeployDialog`, `OnActivityBarClick` to use simplified state
- [x] Kept `RenderInteractionEditor` static method (tab-mode read-only display)
- [x] Kept `LoadTabData` interaction case

#### Tests for Task 1.1

- [x] Create `Tests/Components/InteractionEditorTests.cs` (bUnit)
- [x] Test: renders nothing by default (component starts closed)
- [x] Test: OnClosed callback is wired correctly
- [x] Test: AvailableIndustries parameter accepts a list
- [x] Test: InteractionEditorResult has expected properties and defaults
- [x] 5 bUnit tests pass

**Done when**: Interaction editor works from its own component. ~1,157 lines removed. 5 bUnit tests pass.

---

### Task 1.2 — Extract CodexEditor component `[parallel]` ✅

**Files**: `WorldEngineEditor.razor` (remove ~1,670 lines), `Components/Pages/WorldEngine/Editors/CodexEditor.razor` (new, 1,684 lines)
**Estimate**: 1 session

- [x] Created `Editors/CodexEditor.razor` with public methods `OpenNewAsync(CodexSubType)` / `OpenExistingAsync(string, CodexSubType)`
- [x] Moved all `_ce*` state fields (~38 fields) to the new component
- [x] Moved `CodexSubType` enum (made public inside component)
- [x] Moved codex editor markup (GL panels for lore + quest forms, full editor UI)
- [x] Moved all `Ce*` methods (CRUD, stage/objective CRUD, graph callbacks, save/cancel/delete)
- [x] Moved GL lifecycle (init, destroy, panel events, resize handling)
- [x] Moved `[JSInvokable]` graph editor callbacks to component
- [x] Moved `DisposeAsync` GL cleanup to component
- [x] Made static helpers `BuildStageListJson`, `BuildObjectiveListJson`, `BuildEmptyPipelineGraphJson` public for testability
- [x] Wired `WorldEngineEditor.razor` to use `@ref` + `OpenCodexEditor`/`OpenExistingCodexEditor` delegation
- [x] Updated `Components/_Imports.razor` with `Editors` using directive
- [x] Added `@using System.Text.Json` to CodexEditor

#### Tests for Task 1.2

- [x] Created `Tests/Components/CodexEditorTests.cs` (bUnit)
- [x] Test: renders nothing by default (component starts closed)
- [x] Test: OnSave callback is wired
- [x] Test: OnCancel callback is wired
- [x] Test: OnEntityListRefresh callback is wired
- [x] Test: available parameter combinations work
- [x] Test: BuildStageListJson returns valid JSON array
- [x] Test: BuildObjectiveListJson returns valid JSON array
- [x] Test: BuildEmptyPipelineGraphJson returns valid JSON object
- [x] Test: GraphHasNodes correctly identifies empty graphs
- [x] Test: CodexSubType enum has expected values
- [x] Test: CodexSubType enum values are unique
- [x] Test: DeepCopy creates independent copy of InteractionDefinitionDto
- [x] Test: DeepCopy modifications don't affect original
- [x] Test: DeepCopy for InteractionResponseDto works correctly
- [x] Test: InteractionResponseDto has expected properties
- [x] Test: EventCallback types match component parameters
- 17 bUnit tests pass

**Done when**: Codex/lore editor works from its own component. ~1,670 lines removed from parent. 17 bUnit tests pass.

---

### Task 1.3 — Extract RegionGraphEditor component `[parallel]`

**Files**: `WorldEngineEditor.razor` (remove ~2,000 lines), `Components/Pages/WorldEngine/Editors/RegionGraphEditor.razor` (new)
**Estimate**: 1-2 sessions

- [ ] Create `Editors/RegionGraphEditor.razor`
- [ ] Move all `_rg*` state fields (~40 fields)
- [ ] Move `RgPanelMode` enum and all region-graph-specific inner types
- [ ] Move region graph markup: region list, region editor, area editor, import/export, graph canvas
- [ ] Move all `Rg*` / `RegionGraph*` methods
- [ ] Move JS interop references (`_regionBridgeModule`, `_regionDotNetRef`)
- [ ] Move static arrays (`_rgClimateOptions`, `_rgQualityOptions`, `_rgPoiTypeOptions`)
- [ ] Move `RgCamelCase` JSON options
- [ ] Wire `WorldEngineEditor.razor` to use `<RegionGraphEditor>` in the switch block

#### Tests for Task 1.3

- [ ] Create `Tests/Components/RegionGraphEditorTests.cs` (bUnit)
- [ ] Test: renders region list from provided data
- [ ] Test: renders region detail form when a region is selected
- [ ] Test: climate/quality/POI dropdowns populated from static option arrays
- [ ] Test: fires save callback with updated region data
- [ ] Test: import/export buttons are present

**Done when**: Region graph works from its own component. ~2,000 lines removed. 5+ bUnit tests pass.

---

### Task 1.4 — Extract remaining entity form editors `[parallel]`

**Files**: `WorldEngineEditor.razor` (remove inline forms), 7 new `Editors/*.razor` files
**Estimate**: 2 sessions

Create each editor component with the standard pattern:
```razor
[Parameter] public TDto Item { get; set; }
[Parameter] public EventCallback<TDto> OnSave { get; set; }
[Parameter] public EventCallback OnCancel { get; set; }
```

- [ ] Create `Editors/ItemEditor.razor` — Item blueprint fields (ResRef, Tag, Name, Materials, Appearance, etc.)
- [ ] Create `Editors/ResourceNodeEditor.razor` — Node definition (Tag, PlcAppearance, Uses, Outputs, Flora/Tree props)
- [ ] Create `Editors/RegionEditor.razor` — Region definition + areas + environment (non-graph form)
- [ ] Create `Editors/TraitEditor.razor` — Trait definition (Tag, Name, Cost, Effects, restrictions)
- [ ] Create `Editors/IndustryEditor.razor` — Industry definition (knowledge tree, recipes, ingredients)
- [ ] Create `Editors/CoinhouseEditor.razor` — Coinhouse fields (Tag, Settlement, StoredGold, etc.)
- [ ] Create `Editors/DialogueEditor.razor` — Dialogue tree (uses existing `DialogueTreeEditor` sub-component)
- [ ] Wire each into the `WorldEngineEditor.razor` switch block
- [ ] Remove original inline forms from `WorldEngineEditor.razor`

#### Tests for Task 1.4

- [ ] Create `Tests/Components/ItemEditorTests.cs` (bUnit)
- [ ] Test: renders all item form fields (ResRef, Tag, Name, Materials, Appearance)
- [ ] Test: `EditForm` validates required fields before allowing save
- [ ] Test: fires `OnSave` with populated `ItemBlueprintDto`
- [ ] Test: fires `OnCancel` without saving
- [ ] Create `Tests/Components/TraitEditorTests.cs` (bUnit)
- [ ] Test: renders trait form fields (Tag, Name, Cost, Effects)
- [ ] Test: fires `OnSave` with correct DTO
- [ ] Create `Tests/Components/CoinhouseEditorTests.cs` (bUnit)
- [ ] Test: renders coinhouse fields (Tag, Settlement, StoredGold)
- [ ] Test: `StoredGold` field uses `[Range(0, ...)]` validation
- [ ] Create `Tests/Components/IndustryEditorTests.cs` (bUnit)
- [ ] Test: renders industry definition fields
- [ ] Test: fires save with correct DTO

**Done when**: All 7 editors work from their own components. 12+ bUnit tests pass across 4 editor test files.

---

### Task 1.5 — Flatten `WorldEngineEditor.razor` to a thin shell

**Files**: `WorldEngineEditor.razor` (rewrite, ~5,412 → ~300 lines)
**Estimate**: 0.5 session

Depends on: Tasks 0.2, 1.1, 1.2, 1.3, 1.4

- [ ] Verify all extracted components are working correctly
- [ ] Rewrite `WorldEngineEditor.razor` to contain only:
  - Toolbar (endpoint select, breadcrumb, deploy button)
  - Activity bar (entity type icons)
  - `<EntityListPanel>` (from Task 0.2)
  - Editor area with `@switch (tab.EntityType)` delegating to extracted components:
    - Items → `<ItemEditor>`
    - ResourceNodes → `<ResourceNodeEditor>`
    - Regions → `<RegionEditor>` (or `<RegionGraphEditor>` in graph mode)
    - Codex → `<CodexEditor>`
    - Traits → `<TraitEditor>`
    - Industries → `<IndustryEditor>`
    - Interactions → `<InteractionEditor>`
    - Coinhouses → `<CoinhouseEditor>`
    - Dialogues → `<DialogueEditor>`
    - AreaGraph → (read-only, keep inline or extract)
    - Glyphs → (link to full `/worldengine/glyphs`)
- [ ] Verify file compiles cleanly under 350 lines

#### Tests for Task 1.5

- [ ] Create `Tests/Components/WorldEngineEditorShellTests.cs` (bUnit)
- [ ] Test: renders toolbar with endpoint selector
- [ ] Test: renders activity bar with entity type icons
- [ ] Test: renders `<EntityListPanel>` in the list area
- [ ] Test: renders `<ItemEditor>` when active tab entity type is Items
- [ ] Test: renders `<TraitEditor>` when active tab entity type is Traits
- [ ] Test: renders `<InteractionEditor>` when active tab entity type is Interactions
- [ ] Test: switch block dispatches to correct child component for each entity type
- [ ] Test: no inline entity form markup remains (structural assertion)

**Done when**: `WorldEngineEditor.razor` is under 350 lines. All entity editing works. 7+ shell tests pass.

---

## Phase 2 — Absorb standalone pages into the editor, then delete them

> Phase 2 depends on Phase 1 being complete. Tasks 2.1 and 2.3 can run in parallel; 2.2 depends on 2.1; 2.4 and 2.5 depend on 2.2 and 2.3.

### Task 2.1 — Audit standalone pages for editor gaps

**Files**: `Items.razor`, `ResourceNodes.razor`, `Regions.razor`, `Traits.razor`, `Interactions.razor`, `Coinhouses.razor`
**Estimate**: 0.5 session

- [ ] Read `Items.razor` — check for: Import JSON, Export JSON, template badge, enums endpoint
- [ ] Read `ResourceNodes.razor` — check for: filtering or fields unique to standalone
- [ ] Read `Regions.razor` — verify: already superseded by editor's region graph
- [ ] Read `Traits.razor` — check for: race/class restriction lists, conflicting/prerequisite traits
- [ ] Read `Interactions.razor` — verify: already superseded by editor's interaction editor
- [ ] Read `Coinhouses.razor` — check for: EngineId parsing, persona ID string format
- [ ] Create a checklist of all features that need backfilling

**Done when**: A complete checklist exists of features missing from the unified editor.

---

### Task 2.2 — Backfill missing features into the editor

**Files**: `Editors/ItemEditor.razor`, `Editors/CoinhouseEditor.razor`, etc. (depending on audit)
**Estimate**: 1 session

Depends on: Task 2.1

- [ ] Add each missing feature from Task 2.1 audit to the appropriate `Editors/*.razor` file
- [ ] Most changes: adding form fields, import/export buttons, or validation the standalone page had

#### Tests for Task 2.2

- [ ] For each backfilled feature, add a bUnit test verifying the new form field/button exists and functions
- [ ] Example: if Import JSON was backfilled to `ItemEditor`, test that import button renders and fires correct callback

**Done when**: Every feature from the standalone pages is available in the unified editor. Backfilled features have test coverage.

---

### Task 2.3 — Add missing entity types to the editor `[parallel with 2.1]`

**Files**: `Models/WorldEngineEntityType.cs`, 3 new `Editors/*.razor` files, `WorldEngineEditor.razor`
**Estimate**: 1 session

- [ ] Add Organizations, Workstations, RecipeTemplates to `WorldEngineEntityType` enum
- [ ] Create `Editors/OrganizationEditor.razor` using `OrganizationDto` and `OrganizationApiService`
- [ ] Create `Editors/WorkstationEditor.razor` using `WorkstationDefinitionDto` and `WorkstationApiService`
- [ ] Create `Editors/RecipeTemplateEditor.razor` using `RecipeTemplateDefinitionDto` and `RecipeTemplateApiService`
- [ ] Wire each into `WorldEngineEditor.razor` switch block

#### Tests for Task 2.3

- [ ] Create `Tests/Components/OrganizationEditorTests.cs` (bUnit)
- [ ] Test: renders organization form fields
- [ ] Test: fires `OnSave` with correct `OrganizationDto`
- [ ] Create `Tests/Components/WorkstationEditorTests.cs` (bUnit)
- [ ] Test: renders workstation form fields
- [ ] Test: fires `OnSave` with correct `WorkstationDefinitionDto`
- [ ] Create `Tests/Components/RecipeTemplateEditorTests.cs` (bUnit)
- [ ] Test: renders recipe template form fields
- [ ] Test: fires `OnSave` with correct `RecipeTemplateDefinitionDto`
- [ ] Add to `WorldEngineEditorShellTests`: test switch renders each new editor for its entity type

**Done when**: Organizations, Workstations, and RecipeTemplates are editable. 6+ new bUnit tests pass.

---

### Task 2.4 — Delete standalone pages and redirect routes

**Files**: `Items.razor`, `ResourceNodes.razor`, `Regions.razor`, `Traits.razor`, `Interactions.razor`, `Coinhouses.razor`, `NavMenu.razor`
**Estimate**: 0.5 session

Depends on: Tasks 2.2, 2.3

- [ ] Delete `Items.razor`
- [ ] Delete `ResourceNodes.razor`
- [ ] Delete `Regions.razor`
- [ ] Delete `Traits.razor`
- [ ] Delete `Interactions.razor`
- [ ] Delete `Coinhouses.razor`
- [ ] Update `NavMenu.razor` to point WorldEngine links to `/worldengine/editor`
- [ ] Optionally add query-param routing: `?entity=Items` to auto-select entity type on load
- [ ] Verify: no 404s for old routes; all bookmarks still work

#### Tests for Task 2.4

- [ ] Verify: `dotnet build` succeeds after page deletion (no dangling references)
- [ ] Verify: all existing bUnit tests still pass (regression check)
- [ ] If query-param routing added: test that `/worldengine/editor?entity=Items` auto-selects Items entity type

**Done when**: All standalone pages deleted. NavMenu links go to unified editor. Build succeeds. All tests pass.

---

### Task 2.5 — NavMenu final cleanup

**Files**: `Components/Layout/NavMenu.razor` (131 lines)
**Estimate**: 0.25 session

Depends on: Task 2.4

- [ ] Simplify WorldEngine sub-nav to single "Editor" link
- [ ] Optionally add direct-links to entity types via query params (`/worldengine/editor?type=Items`)
- [ ] Verify: nav menu is clean and all links work

**Done when**: NavMenu has one WorldEngine link ("Editor") or direct-links to entity types in the editor.

---

## Summary

| Phase | Tasks | Effort | Parallelizable |
|---|---|---|---|
| 0 — Foundation | 5 | ~4 sessions | 0.0 + 0.1 + 0.4 parallel; 0.2 + 0.3 parallel |
| 1 — Extract editors | 5 | ~5-6 sessions | 1.1–1.4 all parallel; 1.5 after all |
| 2 — Absorb + delete | 5 | ~4 sessions | 2.1 + 2.3 parallel; rest sequential |
| **Total** | **15** | **~14-15 sessions** | |

### Test inventory

| Test file | Type | Tests | Created in |
|---|---|---|---|
| `ApiServiceBaseTests.cs` | NUnit (mocked HTTP) | ~8 | Task 0.1 |
| `ItemApiServiceTests.cs` | NUnit (mocked HTTP) | ~7 | Task 0.1 |
| `EntityListPanelTests.cs` | bUnit | ~7 | Task 0.2 |
| `DtoValidationTests.cs` | NUnit (DataAnnotations) | ~8 | Task 0.3 |
| `InteractionEditorTests.cs` | bUnit | ~5 | Task 1.1 |
| `CodexEditorTests.cs` | bUnit | ~4 | Task 1.2 |
| `RegionGraphEditorTests.cs` | bUnit | ~5 | Task 1.3 |
| `ItemEditorTests.cs` | bUnit | ~4 | Task 1.4 |
| `TraitEditorTests.cs` | bUnit | ~2 | Task 1.4 |
| `CoinhouseEditorTests.cs` | bUnit | ~2 | Task 1.4 |
| `IndustryEditorTests.cs` | bUnit | ~2 | Task 1.4 |
| `WorldEngineEditorShellTests.cs` | bUnit | ~7 | Task 1.5 |
| `OrganizationEditorTests.cs` | bUnit | ~2 | Task 2.3 |
| `WorkstationEditorTests.cs` | bUnit | ~2 | Task 2.3 |
| `RecipeTemplateEditorTests.cs` | bUnit | ~2 | Task 2.3 |
| **Total** | | **~73** | |
