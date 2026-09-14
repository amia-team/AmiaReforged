# 17 — Industries & Crafting: absorb legacy page into unified editor

> Legacy: `Components/Pages/WorldEngine/Industries.razor` (2510 lines, route
> `/worldengine/industries`) with 6 sub-tabs: Overview, Knowledge Graph,
> Recipes, Workstations, Recipe Templates, Knowledge Progression.
> New editor today: Industries explorer + read-only preview tab only.
> All needed API calls already exist in `IndustryApiService`,
> `WorkstationApiService`, `RecipeTemplateApiService`.

## Steps

### 17.1 — New entity types: Workstations + RecipeTemplates

- [x] Add `Workstations`, `RecipeTemplates` to `WorldEngineEntityType`
- [x] Register catalog features (label/icon/order next to Industries) in
  `WorldEngineEditorServiceCollectionExtensions`
- [x] Explorer loaders (`LoadWorkstations`, `LoadRecipeTemplates`) +
  `LoadTabData` cases (`GetByTagAsync`) + `OnListItemClick` default tab path
  (verify it needs no special-casing unlike Codex/Interactions)
- [x] Sidebar "+ New" actions + host-context entries
  (`OpenNewWorkstationAsync`, `OpenNewRecipeTemplateAsync`), same extension
  pattern as `CoinhouseNewSidebarAction`
- [x] Check `DeploymentService.SupportedEntityTypes` — add Industries /
  Workstations / RecipeTemplates if deploy should cover them
- [x] bUnit: explorer lists render; new actions visible per entity type

**Done when:** three explorer lists browse + open (preview) tabs; New buttons
open blank tabs.

### 17.2 — `Editors/IndustryEditor.razor` (Overview + Knowledge + Recipes)

- [x] Params: `Item`, `IsCreating`, `OnSaved`, `OnDeleted`, `OnCancel`
  (same contract as `CoinhouseEditor`)
- [x] Overview section: Tag (locked on edit), Name, Description + Import/Export
  JSON buttons (`ImportJsonAsync` / `ExportJsonAsync` already on the service)
- [x] Knowledge section (nested, saved with parent): list + inline
  create/edit/remove for `IndustryKnowledgeDto` incl. prereq-tags text field
  and harvest/knowledge/crafting effect rows (port `AddHarvestEffect`,
  `AddKnowledgeEffect`, `AddCraftingModifier` logic)
- [x] Recipes section (nested): list + inline create/edit/remove for
  `IndustryRecipeDto` incl. knowledge-tags field and tool requirements
  (`AddRecipeToolRequirement`, exact-tag vs form toggle)
- [x] Delete industry with confirm; inline error/success
- [x] bUnit: renders sections, nested add/remove updates model, save fires
  `OnSaved` (mock `IndustryApiService` handler)

**Done when:** full industry lifecycle works in-tab; legacy Overview,
Knowledge Graph, and Recipes sub-tabs have no unique functionality left.

### 17.3 — `Editors/WorkstationEditor.razor`

- [x] Fields: Tag (locked on edit), Name, Appearance (dropdown incl. custom),
  industries multi-select (reuse `SearchableSelect` from Shared, port
  `_editWorkstationIndustriesText` parsing), any workstation-specific flags
  from the legacy form
- [x] Same `Item/IsCreating/OnSaved/OnDeleted/OnCancel` contract; delete confirm
- [x] Wire tab arm (replacing preview); create-tab blank cache per tab id
  (same `NewXFor(tabId)` pattern as coinhouse 16.2)
- [x] bUnit: renders fields, save + delete flows

**Done when:** workstation CRUD works in-tab; legacy Workstations sub-tab
has no unique functionality left.

### 17.4 — `Editors/RecipeTemplateEditor.razor`

- [x] Fields: Tag (locked on edit), Name, linked industry (industry picker —
  port `OpenIndustryPicker`/`FilterIndustries`/`SelectIndustryForTemplate`),
  knowledge-tags text, ingredients + products lists (`AddTemplateIngredient`,
  `AddTemplateProduct`) with item picker (port `OpenItemPicker`,
  `SelectItemForIngredient/Product`, reuse `SearchableSelect` where it fits)
- [x] Tool requirements (`AddToolRequirement`, exact-tag vs form toggle) +
  material category / item-form / tool-form dropdowns from `GetEnumsAsync`
  (port `LoadTemplateEnums`, cache per session)
- [x] "Save + Invalidate cache" (`InvalidateCacheAsync`) parity with legacy
- [x] Same editor contract; wire tab arm; create-tab blank cache
- [x] bUnit: renders sections, ingredient/product add/remove, save flow

**Done when:** template CRUD works in-tab; legacy Recipe Templates sub-tab
has no unique functionality left.

### 17.5 — Knowledge Progression (global config, no entity key)

- [x] Pinned first row in the Industries explorer ("⚙ Knowledge Progression")
  opening a special tab (`EntityKey` sentinel, e.g. `"__progression__"`)
  rendering `Editors/ProgressionEditor.razor`
- [x] Config form (`GetProgressionConfigAsync` / `UpdateProgressionConfigAsync`)
  + cap-profile list with inline create/edit/delete (`Get/Create/Update/
  DeleteCapProfileAsync` — port `StartCreateCapProfile`, `SaveCapProfile`,
  `ConfirmDeleteCapProfile` logic)
- [x] Tab arm + `LoadTabData` sentinel skip (self-loading like
  Codex/Interactions); excluded from Deploy (no EntityKey semantics)
- [x] bUnit: renders config + profiles, save flows

**Done when:** progression config + cap profiles editable in-tab; legacy
Knowledge Progression sub-tab has no unique functionality left.

### 17.6 — Delete legacy page

- [x] Parity checklist against 17.2–17.5 (fields, pickers, enums, import/export,
  cache invalidate, progression, pagination → explorer Load-more accepted as
  equivalent)
- [x] Delete `Industries.razor`; NavMenu Industries link → `worldengine/editor`
- [x] `dotnet build` clean; full `dotnet test` green (regression)

**Done when:** one Industries & Crafting UI exists; no 404s; all tests pass.
