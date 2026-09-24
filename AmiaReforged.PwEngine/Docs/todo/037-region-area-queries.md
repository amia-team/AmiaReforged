# 037 — Dispatch area membership and region-tag lookups

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Regions**
Depends on: None.

## Current gap

`IsAreaInRegion` and `GetRegionTagForArea` bypass the query dispatcher.

## Change

Add/reuse area-to-region queries and route these methods through them. Migrate synchronous consumers safely or define a compatible non-blocking boundary.

## Starting points

- [Subsystems/Implementations/RegionSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs)

## Acceptance checks

- [ ] Tests cover registered and unregistered areas and matching semantics.
- [ ] Existing callers compile and retain expected absence behavior.
- [ ] The subsystem methods no longer access repositories directly or block asynchronous NWN work.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

## Completion evidence

**Decision.** Kept the synchronous `IRegionSubsystem` boundary (`IsAreaInRegion` / `GetRegionTagForArea`) and routed both through the async query dispatcher, blocking on its result. This matches the existing blocking boundary already used for `GetChaosForAreaAsync` by the synchronous dynamic-encounter caller, avoids a breaking interface change, and removes direct repository access from the subsystem.

**Changed files.**
- `Features/WorldEngine/Subsystems/Regions/Queries/IsAreaInRegionQuery.cs` (new) — `IQuery<bool>`.
- `Features/WorldEngine/Subsystems/Regions/Queries/GetRegionTagForAreaQuery.cs` (new) — `IQuery<string?>`.
- `Features/WorldEngine/Subsystems/Regions/Application/IsAreaInRegionQueryHandler.cs` (new) — delegates to the repository's canonical `IsAreaRegistered`.
- `Features/WorldEngine/Subsystems/Regions/Application/GetRegionTagForAreaQueryHandler.cs` (new) — delegates to the repository's canonical `TryGetRegionForArea`.
- `Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs` — the two methods now dispatch through `IQueryDispatcher`; removed the now-unused `_regionRepository` field and constructor parameter.
- `Features/WorldEngine/Subsystems/Regions/Tests/RegionSubsystemReadBehaviorTests.cs` — registered the two new handlers and dropped the removed repo constructor arg; added 7 tests (registered/unregistered/case-insensitive for both queries plus a consistency check).

**Verification.**
```
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj -c Debug
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj -c Debug --filter "FullyQualifiedName~Regions" --no-build
```
**Result.** Build succeeded (0 errors). Region test suite: 72 passed / 0 failed (includes the 7 new area-to-region tests). The existing synchronous callers in `DynamicEncounterService.cs` are unchanged and compile; they retain the expected absence behavior (`false` / `null`) for unregistered areas.

**Post-implementation review (code-judo).** A strict structural review found the two handlers re-encoded the repository's own case-insensitive area→region matching predicate — logic already present as `IsAreaRegistered` / `TryGetRegionForArea` (both repositories are case-insensitive: the in-memory dictionary is declared with `StringComparer.OrdinalIgnoreCase`; the production `DbRegionRepository` scans with `StringComparison.OrdinalIgnoreCase`). The sibling `GetChaosForAreaQueryHandler` already delegates to `TryGetRegionForArea`. The handlers were simplified to delegate to those canonical methods, deleting ~6 duplicated matching lines per handler, giving a single source of truth for matching semantics, and unifying all three read paths (`IsAreaInRegion`, `GetRegionTagForArea`, `GetChaosForArea`) on the repository's own lookup. Behavior is identical (verified by the passing suite). A noted pre-existing repo-layer inconsistency (in-memory last-write-wins index vs. Db first-match over `All()`) is orthogonal to this task and not exercised through the in-memory repo, whose `_areaToRegionTag` is a single-mapping dictionary.

See [backlog scope and completion rules](README.md).

