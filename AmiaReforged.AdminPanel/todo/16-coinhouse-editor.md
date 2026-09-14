# 16 — Coinhouse editor: absorb legacy page into unified editor

> Legacy: `Components/Pages/WorldEngine/Coinhouses.razor` (432 lines, route
> `/worldengine/coinhouses`). New editor today: explorer list (tag only) +
> read-only preview tab. Goal: full parity, then delete the legacy page.

## Feature inventory (legacy → new home)

| # | Legacy feature | New home |
|---|---|---|
| 1 | Table: Tag / Settlement / StoredGold / Accounts / Deposits / Credits | Explorer row stays tag-only (matches every other type); full stats move into the editor tab |
| 2 | Search + Prev/Next pagination with counts | Explorer already has search + Load-more; keep explorer pattern, no page numbers |
| 3 | + New Coinhouse → create modal (Tag editable, Settlement default 1, EngineId blank = auto-generate, StoredGold, PersonaIdString) | `CoinhouseEditor` create mode in a tab (EntityKey null), opened from a sidebar "New" action |
| 4 | Edit modal (Tag locked, EngineId GUID-validated, read-only stats row) | `CoinhouseEditor` edit mode in a tab |
| 5 | Delete confirm modal with account-count warning | Delete button + inline confirm in the tab |
| 6 | Success/error alerts | Inline status in the tab (matches Codex/Interaction pattern) |

## Steps

### 16.1 — Build `Editors/CoinhouseEditor.razor`

- [x] Params: `[Parameter] CoinhouseDto? Item` (null = create),
  `EventCallback<CoinhouseDto> OnSave`, `EventCallback<string> OnDelete`,
  `EventCallback OnCancel`, `EventCallback OnSavedRefresh` (list refresh)
- [x] `EditForm` + `DataAnnotationsValidator` (DTO already has
  `[Required]`/`[StringLength]`/`[Range]` from todo 0.3)
- [x] Fields: Tag (disabled unless creating), Settlement (min 1),
  Engine ID as text with GUID-or-blank validation (blank = server auto-generates),
  Stored Gold (min 0), Persona ID String (`Coinhouse:tag` hint)
- [x] Read-only stats row in edit mode: Accounts / Total Deposits / Total Credits
- [x] Save → `CreateAsync`/`UpdateAsync`; Delete → inline confirm showing
  account count warning (mirrors legacy modal text); errors inline
- [x] bUnit: renders fields, Tag locked in edit mode, invalid GUID blocks save,
  `OnSave` fires with DTO, delete confirm flow

**Done when:** component creates/edits/deletes against the API in isolation;
4+ bUnit tests pass.

### 16.2 — Wire into the editor tab switch

- [x] `WorldEngineEditor` tab arm for `Coinhouses` renders `<CoinhouseEditor>`
  instead of the `RenderCoinhouseEditor` read-only preview (delete that method
  from `EntityPreviews.cs`)
- [x] Tab data path already loads via `GetByTagAsync` — reuse; refresh list +
  reload tab data after save; mark tab dirty on field change
- [x] Deploy: no change (Interactions-style tab path already covers it)

**Done when:** clicking a coinhouse opens a working editor tab; save refreshes
the explorer list.

### 16.3 — Create flow (sidebar "New" action)

- [x] `EditorFramework/Extensions/CoinhouseNewSidebarAction.razor` (same shape
  as `CodexNewSidebarAction` / `InteractionNewSidebarAction`), visible for
  `Coinhouses` with endpoint selected
- [x] Opens tab with `EntityKey = null`, title "New Coinhouse"; editor renders
  blank form (Settlement default 1)
- [x] After create, tab re-keys to the new tag (or closes + selects the new row)

**Done when:** + New works without leaving the editor; created row appears.

### 16.4 — Delete legacy page

- [x] Verify parity checklist (create/edit/delete/validation/stats) against 16.1
- [x] Delete `Coinhouses.razor`; route `/worldengine/coinhouses` → redirect to
  `/worldengine/editor` (or NavMenu link retarget if no redirect infra)
- [x] `dotnet build` clean; full `dotnet test` green (regression)

**Done when:** one coinhouse UI exists; no 404s; all tests pass.
