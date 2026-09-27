# 18 — Item editor: absorb legacy `Items.razor` into unified editor

> Legacy: `Components/Pages/WorldEngine/Items.razor` (1204 lines, route
> `/worldengine/items`, `@rendermode InteractiveServer`). New editor today:
> explorer list + **read-only** preview (`RenderItemEditor` in
> `WorldEngineEditor.EntityPreviews.cs`). Goal: full CRUD parity (create/edit,
> materials picker, appearance/weapon parts, local vars, variants, import,
> export, delete), then delete the legacy page.

## Feature inventory (legacy → new home)

| # | Legacy feature | New home |
|---|---|---|
| 1 | Table: ResRef / Tag / Name / Item Category / Materials / Base Value | Explorer row stays tag+name (matches every other type); extra columns move into the editor tab |
| 2 | Search + Prev/Next pagination with counts | Explorer already has search + Load-more; keep explorer pattern, drop page numbers |
| 3 | + New Item → create modal (ResRef, Tag editable, BaseValue=1, WeightIncreaseConstant=-1) | `ItemEditor` create mode in a tab (`EntityKey` null), opened from sidebar "New" action |
| 4 | Edit modal (Tag locked, full form: appearance, weapon parts, local vars, variants) | `ItemEditor` edit mode in a tab |
| 5 | Import JSON modal (upload .json/.zip + paste, server `ImportJsonAsync`, succeeded/failed counts) | Same import UI inside the editor tab |
| 6 | Export JSON button (`ExportJsonAsync` → `adminPanelDownloadFile` JS interop) | Export button in the editor tab |
| 7 | Delete confirm modal | Delete button + inline confirm in the tab |

## API surface (already present — no service changes)

`ItemApiService` (extends `ApiServiceBase`) already exposes everything:
`GetAllAsync`, `GetByTagAsync`, `GetEnumsAsync`, `CreateAsync`, `UpdateAsync`,
`DeleteAsync`, `ImportJsonAsync`, `ExportJsonAsync`, `GetExpandedAsync`.
DTOs in `Models/WorldEngineDtos.cs` already carry `[Required]`/`[StringLength]`/
`[Range]`. Items feature is registered (order 100, `bi-box-seam`) and already in
`DeploymentService.SupportedEntityTypes`. `OnListItemClick` already routes Items
through the default tab path — no special-casing needed.

## Steps

### 18.1 — Build `Editors/ItemEditor.razor`

- [ ] Params: `[Parameter] ItemBlueprintDto? Item` (null = create),
  `[Parameter] bool IsCreating`, `EventCallback<ItemBlueprintDto> OnSaved`,
  `EventCallback<string> OnDeleted`, `EventCallback OnCancel`. Same contract as
  `CoinhouseEditor`.
- [ ] `EditForm` + `DataAnnotationsValidator` (no manual save guards).
- [ ] Base fields: ResRef, ItemTag (disabled unless creating), Name,
  Description, ItemForm (dropdown from `GetEnumsAsync`), BaseItemType (int),
  BaseValue (min 0), WeightIncreaseConstant, IsTemplate checkbox.
- [ ] Materials: searchable multi-select — port `AddMaterial`, `RemoveMaterial`,
  `RefreshFilteredMaterials`, `OnMaterialSearchInput`,
  `DismissMaterialDropdownDelayed`, `GetMaterialLabel`, `_materialSearch`,
  `_showMaterialDropdown`, `_filteredMaterials`.
- [ ] Appearance: `AddAppearance`/`RemoveAppearance`, `AddWeaponParts`/
  `RemoveWeaponParts`, `OnSimpleModelChanged` — `AppearanceDataDto` +
  `WeaponPartDataDto` (model/type/color fields).
- [ ] Local Variables: add/remove; type-aware value set (`SetLocalVarValue`),
  display (`GetLocalVarDisplayValue`) — Int / Json / String.
- [ ] Variants: add/remove (`AddVariant`/`RemoveVariant`); per-variant material
  dropdown, BaseValueOverride, nested appearance + weapon parts
  (`OnVariant*` handlers).
- [ ] Delete confirm inline (mirrors coinhouse).
- [ ] Import modal: `OnImportFileSelected` (`.json`/`.zip`, up to 100 files),
  `ProcessJsonFileItems`, `ProcessZipItems`, `ParseJsonItems`, `RunImport`
  (`ImportJsonAsync` → `_importResult` succeeded/failed/errors).
- [ ] Export: `ExportItems` (`ExportJsonAsync` + `DownloadFileAsync` →
  `JS.InvokeVoidAsync("adminPanelDownloadFile", ...)`; helper is defined globally
  in `App.razor`, so it works).
- [ ] Verify: `dotnet build` clean; parity checklist against 18.1 feature table.

### 18.2 — Wire into the editor tab switch

- [ ] Add `else if (activeTab.EntityType == WorldEngineEntityType.Items)` block to
  `WorldEngineEditor.razor` (create → `NewItemFor`, loading/error arms, edit →
  `_tabData` `ItemBlueprintDto`), mirroring the Coinhouse block (~lines 700–739).
- [ ] `LoadTabData` Items case already returns `ItemApi.GetByTagAsync` — reuse.
- [ ] Delete `RenderItemEditor` and the `WorldEngineEntityType.Items` arm from
  `WorldEngineEditor.EntityPreviews.cs`. Keep `RenderEntityEditor` — Region,
  Glyph and Interaction still render through it.
- [ ] Refresh list + reload/re-key tab after save; mark tab dirty on change.

### 18.3 — Create flow (sidebar "New" action)

- [ ] `EditorFramework/Extensions/ItemNewSidebarAction.razor` (same shape as
  `CoinhouseNewSidebarAction`, binds `Context.OpenNewItemAsync`).
- [ ] Register it in `WorldEngineEditorServiceCollectionExtensions`
  (`SidebarHeaderActions`, order 100, `RequiresEndpoint`).
- [ ] Add `OpenNewItemAsync` to `WorldEngineEditorHostContext`.
- [ ] New partial `WorldEngineEditor.Items.cs` mirroring `WorldEngineEditor.Crafting.cs`:
  `_newItemDtos` cache, `NewItemFor(tabId)`, `OpenNewItemTab()`,
  `HandleItemSaved(tabId, saved)` (re-key create tab onto new tag),
  `HandleItemDeleted(tabId, tag)`.
- [ ] Remove `_newItemDtos` entry in `CloseTab`.

### 18.4 — Delete legacy page

- [ ] Parity checklist against 18.1–18.3 (fields, materials picker, appearance,
  local vars, variants, import .json/.zip, export, delete, validation).
- [ ] Delete `Items.razor`; retarget NavMenu `/worldengine/items` link →
  `/worldengine/editor` (or add a redirect if infra exists).
- [ ] `dotnet build` clean.

**Done when:** one item UI exists; no 404s; build green.

## Caveats

- **No test project exists in `AmiaReforged.AdminPanel`.** The `todo/16` and
  `todo/17` files claim "4+ bUnit tests pass", but there is no test csproj and no
  bUnit reference anywhere in the repo — those claims are not verifiable here.
  Verification for 18 is therefore: parity checklist + `dotnet build` + manual
  smoke test. To get automated coverage, add a bUnit test project first (out of
  scope for this migration step).
- **Do not conflate with the PwEngine item editor.** `AmiaReforged.PwEngine` has
  its own `ItemEditorPresenter` / `ItemEditorView` (`IDmWindow`) — a separate
  server-side Dungeon Master tool. This migration is AdminPanel-only.
- **`DismissMaterialDropdownDelayed` uses `Task.Delay(200)` + `InvokeAsync`.**
  Port as-is for parity; simplify only if desired.
- **Export/Import are raw-JSON + JS interop**, not typed DTO round-trips — keep
  the same approach rather than inventing a new contract.
