# 034 — Implement region facade lookup and listing

Status: **Completed**
Type: **Implementation**
Audit area: **F-7 Regions**
Depends on: [033 — Reconcile region facade and definition models](033-region-contract.md).

## Current gap

`GetRegionAsync` and `GetAllRegionsAsync` always return null/empty.

## Change

Route both methods through existing region queries and map the result using task 033's contract.

## Starting points

- [Subsystems/Implementations/RegionSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs)

## Acceptance checks

- [ ] A registered region is returned by tag and included in the list.
- [ ] Unknown tag and empty repository return the agreed absence values.
- [ ] The facade no longer returns unconditional stubs.

## Completion evidence

**Chosen behavior.** Both methods now route through the existing CQRS queries and map the result
with task 033's contract:

- `GetRegionAsync(tag)` → `_queryDispatcher.DispatchAsync<GetRegionDefinitionQuery, RegionDefinition?>`
  (case-insensitive by tag). Unknown tag ⇒ query returns null ⇒ method returns null.
- `GetAllRegionsAsync()` → `_queryDispatcher.DispatchAsync<SearchRegionDefinitionsQuery, List<RegionDefinition>>`
  with `SearchTerm = null` (returns all). Empty repository ⇒ empty list.
- Mapping via private `ToRegionInfo` (task 033 defaults: `Description ?? ""`, `Type ?? RegionType.Special`).

The facade no longer returns unconditional stubs. `IQueryDispatcher` is injected (no DI cycle: it
resolves handlers from the container); `RegionSubsystem` is bound only via `[ServiceBinding]`, never
constructed by hand.

## Changed files

- `Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs` — implemented the two read
  methods, added `IQueryDispatcher` dependency and `ToRegionInfo`.
- `Features/WorldEngine/Subsystems/Regions/Tests/RegionSubsystemReadBehaviorTests.cs` — new end-to-end
  read-path tests (real `QueryDispatcher` + real region handlers + `InMemoryRegionRepository`).

## Verification

```bash
dotnet build AmiaReforged.sln --nologo
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --filter "FullyQualifiedName~Region"
```

Result: build succeeded (0 errors); 63 region tests passed (57 pre-existing + 6 new).
The new tests cover: known-tag projection, case-insensitive tag lookup, unknown tag → null,
listing includes registered regions, empty repository → empty list, and the contract defaults
(`Description == ""`, `Type == RegionType.Special`).

Manual procedure to confirm acceptance:
1. `UpsertRegionCommand` a region with `Name`/`Description`/`Type` and at least one area.
2. `GetRegionAsync(tag)` returns that region's `RegionInfo` (tag lookup); an unknown tag returns null.
3. `GetAllRegionsAsync()` includes the registered region; an empty repository returns an empty list.

(Note: write path `UpdateRegionAsync` is covered by the follow-up task 035.)

See [backlog scope and completion rules](README.md).

