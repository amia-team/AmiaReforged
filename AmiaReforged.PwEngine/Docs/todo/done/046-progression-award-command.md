# 046 — Make progression-point awards dispatchable

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

Progression-point awards had no independent command boundary — `CraftItemHandler` called `KnowledgeProgressionService.AwardProgressionPoints` directly.

## Change

Added an award command/handler around the existing service and migrated the one independent caller (`CraftItemHandler`). The domain logic in the service is unchanged.

### Decision

- **Kept `IKnowledgeProgressionService.AwardProgressionPoints`** as the domain service; the new handler delegates to it (allowed — it is no longer an independent entry point once `CraftItemHandler` is migrated).
- **`AwardProgressionCommand`** (record) + **`AwardProgressionHandler`** (`[ServiceBinding(typeof(ICommandHandler<AwardProgressionCommand>))]`) injected with `IKnowledgeProgressionService` and `ICommandDispatcher`. Fails (`CommandResult.Fail`) on `Points <= 0`; otherwise delegates to the service and returns a flat `resultData` dict (`knowledgePointsEarned`, `newTotalKnowledgePoints`, `newEconomyKnowledgePointTotal`, `progressionPointsRemaining`, `progressionPointsRequired`, `isAtSoftCap`, `isAtHardCap`, `message`) so the migrated `CraftItemHandler` reads the same keys back unchanged.
- **`CraftItemHandler`** now injects `ICommandDispatcher`, drops the `_progressionService` field, and routes the award through `await _commandDispatcher.DispatchAsync(new AwardProgressionCommand{...})`, reading the returned data back into the same `resultData` keys. Awards exactly once.
- On success the dispatcher publishes the generic `CommandExecutedEvent<AwardProgressionCommand>` (no explicit `IEventBus` needed on the handler).

## Starting points

- [Subsystems/Industries/KnowledgeSubsystem/KnowledgeProgressionService.cs](../../Features/WorldEngine/Subsystems/Industries/KnowledgeSubsystem/KnowledgeProgressionService.cs)
- [Application/Industries/Commands/CraftItemCommand.cs](../../Features/WorldEngine/Application/Industries/Commands/CraftItemCommand.cs)

## Acceptance checks

- [x] Tests cover threshold rollover, soft-cap behavior, and hard-cap rejection.
- [x] A directly dispatched award produces the generic command-executed event.
- [x] Crafting still awards the same amount exactly once; independent callers listed and migrated.

## Completion evidence

### Changed files
- **New:** `Features/WorldEngine/Application/Industries/Commands/AwardProgressionCommand.cs` — `AwardProgressionCommand` record + `AwardProgressionHandler` (injects `IKnowledgeProgressionService` + `ICommandDispatcher`; fails on `Points <= 0`; returns flat data dict).
- **Modified:** `Features/WorldEngine/Application/Industries/Commands/CraftItemCommand.cs` — `CraftItemHandler` now dispatches `AwardProgressionCommand` via `ICommandDispatcher`; removed `_progressionService` field/injection; preserves the original `resultData` keys.
- **New (test):** `Features/WorldEngine/Subsystems/Industries/KnowledgeSubsystem/Tests/AwardProgressionCommandTests.cs` — in-memory doubles (`IKnowledgeProgressionRepository`, `IKnowledgeCapProfileRepository`, `IWorldConfigProvider`, `InMemoryEventBus`), plus stubs for `ICraftingProcessor`/`IProficiencyProgressionService`; covers threshold rollover, soft-cap, hard-cap rejection, zero-points fail, generic `CommandExecutedEvent` on direct dispatch, and crafting awarding exactly once.

### Independent callers audited
Only `CraftItemHandler` (`CraftItemCommand.cs`) called `AwardProgressionPoints`. The other hits for `ProgressionPointsAwarded` are the `CraftingResult`/`Recipe` property (not the service method); controllers/mappers/presenter consume that property and are unaffected. No further migration needed.

### Verification

```
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --filter 'FullyQualifiedName~AwardProgression' --verbosity minimal -m:1
```

Observed: `Passed! - Failed: 0, Passed: 7, Skipped: 0, Total: 7` (threshold rollover, below-threshold accumulate, soft-cap, hard-cap rejection, zero-points fail, generic `CommandExecutedEvent<AwardProgressionCommand>` + `KnowledgePointEarnedEvent`, craft awards exactly once).

Full suite: `dotnet test ... --filter 'FullyQualifiedName~WorldEngine'` → `Passed! - Failed: 0, Passed: 1800, Skipped: 0, Total: 1800`.

### Notes
- Default curve (BaseCost=100, Exponential 1.15, soft=100, hard=150): awarding 300 KP rolls over to 2 economy KP with 85 progression points remaining (costs 100 then 115).
- `ProgressionResult.Blocked(...)` returns a fresh result with `NewEconomyKnowledgePointTotal = 0` (it does not echo the pre-existing total); the handler passes the service's data through unchanged, so the hard-cap test asserts `0`, not the seeded total.
- Test-dispatcher wiring: a self-referential `ICommandDispatcher` built from a **local** variable triggers a CS0165 "unassigned local variable"; the working form reuses the `SetUp` `_dispatcher` field (which already includes the award handler).

See [backlog scope and completion rules](README.md).

