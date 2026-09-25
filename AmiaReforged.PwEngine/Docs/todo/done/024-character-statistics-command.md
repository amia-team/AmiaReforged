# 024 — Dispatch character statistics updates

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: [022 — Resolve unsupported character statistics fields](022-character-statistics-contract.md); [023 — Dispatch character statistics reads](023-character-statistics-query.md).

## Current gap

`UpdateCharacterStatsAsync` mutates and saves directly.

## Change

Added an update-statistics command/handler implementing task 022's supported write field (`PlayTime`) and dispatched it from the subsystem.

### New files

- `Features/WorldEngine/Subsystems/Characters/Commands/UpdateCharacterStatsCommand.cs` — `UpdateCharacterStatsCommand(CharacterId, int PlayTime) : ICommand`.
- `Features/WorldEngine/Subsystems/Characters/Commands/UpdateCharacterStatsCommandHandler.cs` — updates only `PlayTime`; fails when the record is missing; leaves all other stored counters untouched.

`CharacterSubsystem.UpdateCharacterStatsAsync` now calls `_commands.DispatchAsync<UpdateCharacterStatsCommand>(...)` and no longer calls `SaveChanges` directly. A successful dispatch publishes the generic `CommandExecutedEvent<UpdateCharacterStatsCommand>`.

## Starting points

- [Subsystems/Implementations/CharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs)

## Acceptance checks

- [ ] Supported values round-trip through command and query.
- [ ] Missing records and unsupported updates have explicit tested results.
- [ ] Successful writes receive the generic command-executed event; the subsystem no longer saves directly.

## Completion evidence

- Changed files:
  - New command + handler (see above).
  - `Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs` — `UpdateCharacterStatsAsync` now dispatches and no longer calls `SaveChanges` directly.
- Tests: `Features/WorldEngine/Subsystems/Characters/Commands/Tests/UpdateCharacterStatsCommandHandlerTests.cs` — covers a play-time round-trip (command then query through a shared repository), a missing record (`Fail`, no save), that unsupported counters are left untouched, and that a dispatched successful write publishes the generic `CommandExecutedEvent<UpdateCharacterStatsCommand>` via the `CommandDispatcher` + `InMemoryEventBus`.
- Verification command:
  ```sh
  dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-build --no-restore --filter 'FullyQualifiedName~WorldEngine' --verbosity minimal -m:1
  ```
- Observed result: build succeeded; **1801 passed, 0 failed**. Acceptance checks met: supported values round-trip through command and query; missing records and unsupported updates have explicit tested results; successful writes receive the generic command-executed event and the subsystem no longer saves directly.

See [backlog scope and completion rules](README.md).

