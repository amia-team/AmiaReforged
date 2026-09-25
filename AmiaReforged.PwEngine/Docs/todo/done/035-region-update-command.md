# 035 — Implement region facade updates through dispatch

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Regions**
Depends on: [033 — Reconcile region facade and definition models](033-region-contract.md); [034 — Implement region facade lookup and listing](034-region-read-queries.md).

## Current gap

`UpdateRegionAsync` always fails despite available application-level region commands.

## Change

Translate the reconciled facade request into the existing update command and dispatch it. Keep validation and persistence in the handler.

## Starting points

- [Subsystems/Implementations/RegionSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs)

## Acceptance checks

- [ ] Updating a known region changes supported fields and is observable through queries.
- [ ] A missing region fails without insertion.
- [ ] The facade no longer returns `Not yet implemented` and successful execution publishes the generic event.

## Decision

The facade-facing partial `UpdateRegionCommand` (`Subsystems` namespace, fields
`RegionTag/Name/Description/Type`) is translated into the application persistence command
`Application.Regions.Commands.UpdateRegionCommand` (`Tag` + full `RegionDefinition`). The current
definition is loaded (via the existing `GetRegionDefinitionQuery`) only to merge partial changes
onto it; the application handler owns existence validation and persistence. The missing-region
case is therefore still rejected by the handler (without insertion), and the generic
`CommandExecutedEvent` is published by the dispatcher on success.

## Changed files

- `Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs` — injected
  `ICommandDispatcher`; implemented `UpdateRegionAsync` (translate + merge + dispatch); added
  `SharedKernel.ValueObjects` using for `RegionTag`.
- `Features/WorldEngine/Subsystems/Regions/Tests/RegionSubsystemReadBehaviorTests.cs` — constructor
  now wires an `ICommandDispatcher` (real `CommandDispatcher` + `UpdateRegionHandler`); added update
  coverage and a capturing event bus.

### Post-review follow-ups (independent `reviewer` run, all checklist items PASS)

- `RegionSubsystem.cs` now rejects an empty/whitespace `RegionTag` with `CommandResult.Fail("Region tag
  cannot be empty")` before dispatch, so `UpdateRegionAsync` returns `Fail` instead of throwing on
  `new RegionTag(...)`. Covered by `UpdateRegionAsync_WithEmptyTag_ReturnsFailWithoutThrowing`.
- The three `new UpdateRegionCommand(...)` test calls are fully-qualified
  (`...Subsystems.UpdateRegionCommand`) to remove the previous argument-based disambiguation between
  the facade and application command types.

## Verification

Command:

```bash
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --nologo
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --filter "FullyQualifiedName~Region"
```

Result: build succeeded (0 errors); 72 region tests passed (was 57 in task 033; +14 covering the new
update and chaos-dispatch behavior, +1 empty-tag guard after independent review). Updating a known
region renames it and preserves untouched fields; an unknown tag fails and leaves the repository
empty; an empty tag fails with a validation message instead of throwing; success publishes the
generic `CommandExecutedEvent<UpdateRegionCommand>`.

See [backlog scope and completion rules](README.md).
