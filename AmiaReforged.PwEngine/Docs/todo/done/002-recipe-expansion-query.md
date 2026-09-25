# 002 — Dispatch recipe expansion reads

Status: **Done**
Type: **Implementation**
Audit area: **F-1**
Depends on: None.

## Current gap

`GetExpanded` calls `RecipeTemplateExpander` outside query dispatch.

## Change

Add a query/handler for expanded recipes and route the endpoint through the facade.

## Starting points

- [API/Controllers/RecipeTemplateController.cs](../../Features/WorldEngine/API/Controllers/RecipeTemplateController.cs)

## Acceptance checks

- [x] The controller forwards the template tag and cancellation token.
- [x] Handler tests cover an existing template and a missing template.
- [x] Response shape is preserved and the endpoint has no direct expander call.

## Decision

The expanded-recipe read stays a **query** (read-only) backed by the existing
`RecipeTemplateExpander` cache, mirroring task `001` (item expansion). The handler
simply forwards the template tag — no new domain semantics. `GetExpandedRecipesForTemplate`
was made `virtual` so the handler can be unit-tested with a mocked expander. The
endpoint's response DTO is unchanged.

## Changed files

- `Application/Industries/Queries/RecipeTemplateQueries.cs` — new `GetExpandedRecipesQuery(string TemplateTag) : IQuery<List<Recipe>>` and `GetExpandedRecipesQueryHandler` (registered via `[ServiceBinding]`).
- `Subsystems/Industries/RecipeTemplateExpander.cs` — `GetExpandedRecipesForTemplate` marked `virtual` (testability only).
- `API/Controllers/RecipeTemplateController.cs` — `GetExpanded` now routes through `ResolveFacade()` / `QueryAsync<GetExpandedRecipesQuery, List<Recipe>>` with `ctx.CancellationToken`; direct `RecipeTemplateExpander` resolution removed. Response DTO unchanged.
- `Application/Industries/Tests/GetExpandedRecipesQueryHandlerTests.cs` — new handler tests: forwards tag, populated results, empty list.
- `API/Tests/ControllerCqrsTests.cs` — two controller tests: dispatches the template tag through the query, and returns 200 with empty recipes for a missing template.

## Verification

```sh
dotnet build AmiaReforged.PwEngine.csproj --no-restore -v q
dotnet test AmiaReforged.PwEngine.csproj --no-build \
  --filter 'FullyQualifiedName~GetExpandedRecipesQueryHandlerTests|FullyQualifiedName~ControllerCqrsTests' \
  --verbosity minimal -m:1
dotnet test AmiaReforged.PwEngine.csproj --no-build \
  --filter 'FullyQualifiedName~WorldEngine' --verbosity minimal -m:1
```

Observed result: build succeeds; the focused filter reports `Failed: 0, Passed: 15`
(recipe handler + item handler + controller CQRS tests); the full WorldEngine suite
reports `Failed: 0, Passed: 1694` (1685 baseline + 4 from task 001 + 5 new here).

See [backlog scope and completion rules](README.md).

