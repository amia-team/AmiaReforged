# 001 — Dispatch item expansion reads

Status: **Done**
Type: **Implementation**
Audit area: **F-1**
Depends on: None.

## Current gap

`GetExpanded` resolves `ItemBlueprintExpander` directly.

## Change

Add an expansion query and handler, and route this endpoint through `ResolveFacade()` and `QueryAsync`. Preserve its response DTO.

## Starting points

- [API/Controllers/ItemController.cs](../../Features/WorldEngine/API/Controllers/ItemController.cs)

## Acceptance checks

- [ ] A controller test verifies the query receives the requested template tag.
- [ ] Handler tests cover populated and empty expansion results.
- [ ] The endpoint no longer resolves or calls the expander directly.

## Decision

The expansion read stays a **query** (read-only) backed by the existing
`ItemBlueprintExpander` cache. The handler simply forwards the template tag;
no new domain semantics are invented. `GetExpandedItemsForTemplate` was made
`virtual` so the handler can be unit-tested with a mocked expander.

## Changed files

- `Application/Items/Queries/GetExpandedItemDefinitionsQuery.cs` — new query carrying `TemplateTag`.
- `Application/Items/Handlers/GetExpandedItemDefinitionsQueryHandler.cs` — new handler; resolves the expander and forwards the tag.
- `API/Controllers/ItemController.cs` — `GetExpanded` now routes through `ResolveFacade()` / `QueryAsync<GetExpandedItemDefinitionsQuery, ...>`; direct `ItemBlueprintExpander` resolution removed. Response DTO (array of `ToDto`) unchanged.
- `Subsystems/Items/ItemBlueprintExpander.cs` — `GetExpandedItemsForTemplate` marked `virtual` (testability only).
- `Application/Items/Tests` handler test + `API/Tests/ControllerCqrsTests.cs` controller test added.

## Verification

```sh
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-restore -v q
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-build \
  --filter 'FullyQualifiedName~GetExpandedItemDefinitionsQueryHandlerTests|FullyQualifiedName~ControllerCqrsTests' \
  --verbosity minimal -m:1
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-build \
  --filter 'FullyQualifiedName~WorldEngine' --verbosity minimal -m:1
```

Observed result: build succeeds; the focused filter reports `Failed: 0, Passed: 10`;
the full WorldEngine suite reports `Failed: 0, Passed: 1689` (1685 baseline + 4 new).

## Acceptance checks

- [x] A controller test verifies the query receives the requested template tag.
  (`ControllerCqrsTests.ItemController_GetExpanded_WhenCalled_DispatchesTemplateTagThroughQuery` asserts `q.TemplateTag == tag`.)
- [x] Handler tests cover populated and empty expansion results.
  (`GetExpandedItemDefinitionsQueryHandlerTests` — forwards tag, populated list, empty list.)
- [x] The endpoint no longer resolves or calls the expander directly.

See [backlog scope and completion rules](README.md).

