# 036 — Dispatch regional chaos resolution

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Regions**
Depends on: None.

## Current gap

`GetChaosForAreaAsync` implements repository lookup and precedence inline.

## Change

Move the existing resolution into a query handler and dispatch from the subsystem.

## Starting points

- [Subsystems/Implementations/RegionSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs)

## Acceptance checks

- [ ] Tests cover area override, region default, and unregistered-area fallback.
- [ ] Case-insensitive area matching remains intact.
- [ ] The subsystem no longer directly reads the region repository for chaos.

## Decision

The inline chaos resolution was moved verbatim into a new query handler. The subsystem now only
dispatches; it no longer reads the region repository for chaos. Resolution order is unchanged:
area-level `Environment.Chaos` override (case-insensitive area match) takes precedence over the
region `DefaultChaos`, which itself falls back to `ChaosState.Default` for unregistered areas.

## Changed files

- `Features/WorldEngine/Subsystems/Regions/Queries/GetChaosForAreaQuery.cs` (new) —
  `GetChaosForAreaQuery(string AreaResRef) : IQuery<ChaosState>`.
- `Features/WorldEngine/Subsystems/Regions/Application/GetChaosForAreaQueryHandler.cs` (new) —
  resolves override -> region default -> default; bound via `IQueryHandler`.
- `Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs` — `GetChaosForAreaAsync`
  now dispatches the query; added `Subsystems.Regions.Queries` using.
- `Features/WorldEngine/Subsystems/Regions/Tests/RegionSubsystemReadBehaviorTests.cs` — wired the
  new chaos handler into the test dispatcher; added area-override, region-default, unregistered-
  area, and case-insensitivity coverage.

## Verification

Command:

```bash
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --nologo
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --filter "FullyQualifiedName~Region"
```

Result: build succeeded (0 errors); 71 region tests passed. Area override wins over the region
default; the region default is used with no area override; unregistered areas return
`ChaosState.Default`; area matching is case-insensitive; and the subsystem resolves chaos solely
through the dispatched query.

See [backlog scope and completion rules](README.md).
