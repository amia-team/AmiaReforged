# 008 — Dispatch cached area-graph reads

Status: **Done**
Type: **Implementation**
Audit area: **F-1 broader controller scope**
Depends on: [009 — Dispatch explicit area-graph refreshes](009-area-graph-refresh-command.md).

## Current gap

`GetGraph` constructs/resolves its cache service directly.

## Change

Put ordinary graph reads behind a query handler. Keep forced refresh separate from the read query and preserve the current response.

## Starting points

- [API/Controllers/AreaGraphController.cs](../../Features/WorldEngine/API/Controllers/AreaGraphController.cs)

## Acceptance checks

- [x] The ordinary GET dispatches a query through the facade.
- [x] A handler test verifies graph data is returned through the existing cache boundary.
- [x] No query requests a forced refresh; coordinate `refresh=true` with task 009.

## Completion evidence

**Behavior / decisions:**
- Added `GetAreaGraphQuery : IQuery<AreaGraphData>` (`Application/AreaGraph/Queries/`) and `GetAreaGraphQueryHandler` (`Application/AreaGraph/Handlers/`). The handler takes `AreaGraphCacheService` via constructor and returns `_cacheService.GetOrBuildAsync(forceRefresh: false)` — an ordinary read never forces a rebuild.
- Registered `AreaGraphCacheService` in Anvil DI via `[ServiceBinding(typeof(AreaGraphCacheService))]` so the handler can be constructed; `AreaGraphBuilder` is constructed by Anvil through its implicit parameterless constructor. Made `GetOrBuildAsync`/`RefreshAsync` `virtual` so they can be overridden by test doubles (Moq cannot proxy a class whose constructor takes a concrete class).
- `AreaGraphController.GetGraph` now resolves the facade and dispatches `GetAreaGraphQuery` via `facade.QueryAsync<GetAreaGraphQuery, AreaGraphData>`, returning `new ApiResult(200, graph)` — the response DTO is unchanged.
- The explicit refresh endpoint (`POST .../graph/refresh`) is kept separate from the read query for task 009; it now resolves the DI singleton and calls `RefreshAsync()` directly, so the read and forced-refresh paths share one cache instance.

**Changed files:**
- `Features/WorldEngine/Application/AreaGraph/Queries/GetAreaGraphQuery.cs` (new)
- `Features/WorldEngine/Application/AreaGraph/Handlers/GetAreaGraphQueryHandler.cs` (new)
- `Features/WorldEngine/Subsystems/AreaGraph/AreaGraphCacheService.cs` — added `[ServiceBinding(typeof(AreaGraphCacheService))]`, made `GetOrBuildAsync`/`RefreshAsync` `virtual`, and gave the constructor an effective parameterless form (`AreaGraphBuilder builder = null!, string? cacheDirectory = null`) so it is DI-constructible and testable.
- `Features/WorldEngine/API/Controllers/AreaGraphController.cs` — `GetGraph` routes through the facade; `RefreshGraph` resolves the DI singleton and calls `RefreshAsync`.
- `Features/WorldEngine/Subsystems/AreaGraph/Tests/GetAreaGraphQueryHandlerTests.cs` (new) — hand-written `FakeAreaGraphCacheService` double (overriding the virtual cache-boundary methods) verifies the handler returns the graph through the cache boundary and never forces a refresh.
- `Features/WorldEngine/API/Tests/ControllerCqrsTests.cs` — added `AreaGraphController_GetGraph_WhenCalled_DispatchesQueryAndReturns200` and removed the untestable 503 facade-unavailable case (the StubProvider harness always supplies the facade).

**Verification command:**

```sh
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-restore -v q
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-restore --filter 'FullyQualifiedName~AreaGraph'
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-restore --filter 'FullyQualifiedName~ControllerCqrsTests'
```

**Observed result:** Build succeeded (0 errors). Area-graph tests: `Passed! - Failed: 0, Passed: 3` (handler double test + 2 controller dispatch tests). Controller CQRS suite: `Passed! - Failed: 0, Passed: 26`. The read query calls `GetOrBuildAsync(false)`; no query path forces a refresh (refresh=true is deferred to task 009).

See [backlog scope and completion rules](README.md).

## Acceptance checks

- [ ] The ordinary GET dispatches a query through the facade.
- [ ] A handler test verifies graph data is returned through the existing cache boundary.
- [ ] No query requests a forced refresh; coordinate `refresh=true` with task 009.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

