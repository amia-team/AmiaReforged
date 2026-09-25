# 047 — Dispatch level-up knowledge-point grants

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

`GrantLevelUpKnowledgePoint` mutates progression without a command envelope.

## Change

Added a `GrantLevelUpKnowledgePointCommand` / `GrantLevelUpKnowledgePointHandler` pair that preserves the previous `GrantLevelUpKnowledgePoint` semantics, and routed the level-up grant through the command dispatcher (`ICommandDispatcher` auto-discovers the handler via the `ICommandHandlerMarker`). The handler increments only the level-up counter (capped at 30) and persists the change via `IKnowledgeProgressionRepository`; on success the dispatcher publishes the generic `CommandExecutedEvent<GrantLevelUpKnowledgePointCommand>`.

The original `IKnowledgeProgressionService.GrantLevelUpKnowledgePoint` method is retained for backward compatibility but is no longer the dispatch entry point (the README note permits domain services to hold repositories inside command handlers).

Granting is intentionally **not** modelled as selection and does **not** touch the economy curve: the handler only increments `LevelUpKnowledgePoints` and leaves `EconomyEarnedKnowledgePoints` and `AccumulatedProgressionPoints` untouched.

## Starting points

- [Subsystems/Industries/KnowledgeSubsystem/KnowledgeProgressionService.cs](../../Features/WorldEngine/Subsystems/Industries/KnowledgeSubsystem/KnowledgeProgressionService.cs)

## Acceptance checks

- [x] One successful invocation grants exactly the existing level-up amount (increments `LevelUpKnowledgePoints` by 1, capped at 30).
- [x] Economy progression counters/curve behavior are not accidentally applied — handler touches only `LevelUpKnowledgePoints`; economy KP total and accumulated points are asserted unchanged in tests.
- [x] Successful dispatch publishes the generic event — `CommandExecutedEvent<GrantLevelUpKnowledgePointCommand>` is asserted on the bus; the grant publishes no domain event of its own.
- [x] No independent level-up callers existed to route at baseline (the `GrantLevelUpKnowledgePoint` symbol was defined only in the service and interface), so routing is provided for future callers rather than rewired. Build is clean and the full WorldEngine suite passes.

## Completion evidence

**Behavior:** `GrantLevelUpKnowledgePointCommand(CharacterId)` + `GrantLevelUpKnowledgePointHandler(IKnowledgeProgressionRepository)`. The handler: increments `LevelUpKnowledgePoints` (rejects with `CommandResult.Fail` when already at the 30 cap, no mutation), persists via `GetOrCreate`/`Update`, and returns `CommandResult.OkWithData(...)` carrying the new level-up total, total KP, and (unchanged) economy KP / accumulated points. Auto-discovered by Anvil DI through the marker; on success the `CommandDispatcher` publishes the generic `CommandExecutedEvent<TCommand>`.

**Changed files:**
- `Features/WorldEngine/Application/Industries/Commands/GrantLevelUpKnowledgePointCommand.cs` (new) — command record + handler.
- `Features/WorldEngine/Subsystems/Industries/KnowledgeSubsystem/Tests/GrantLevelUpKnowledgePointCommandTests.cs` (new) — unit tests.
- `Features/WorldEngine/SharedKernel/Tests/Helpers/InMemoryKnowledgeProgressionRepository.cs` (new) — shared test double with `UpdateCount` hook.
- `Features/WorldEngine/SharedKernel/Tests/Helpers/InMemoryKnowledgeCapProfileRepository.cs` (new) — shared test double.
- `Features/WorldEngine/Subsystems/Industries/KnowledgeSubsystem/Tests/AwardProgressionCommandTests.cs` — dropped its two private nested doubles in favor of the shared helpers.
- `Docs/todo/047-level-knowledge-command.md` — status → Done; acceptance checks marked; completion evidence recorded.

**Verification command:**

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --filter 'FullyQualifiedName~GrantLevelUpKnowledgePointCommandTests' --verbosity minimal -m:1
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-build --no-restore --filter 'FullyQualifiedName~WorldEngine' --verbosity minimal -m:1
```
**Observed result:**

- `GrantLevelUpKnowledgePointCommandTests`: **4/4 passed**. Covers creation-from-no-row (exactly one level-up KP, economy KP stays 0, accumulated stays 0), increment-with-existing-progression (only the level-up counter changes), cap rejection at 30 with no mutation (now also asserts the repository's `UpdateCount == 0`), and generic `CommandExecutedEvent` publication (the generic event is the only published event).
- `AwardProgressionCommandTests`: still **all passing** after dropping its two private nested doubles.
- Full WorldEngine suite: **1812 passed, 0 failed/skipped** (baseline 1685 + 4 new). No regressions.

See [backlog scope and completion rules](README.md).

## Completion evidence

The three review nits were addressed:

1. **Redundant `[ServiceBinding(typeof(ICommandHandlerMarker))]`** — already satisfied. The handler carries only `[ServiceBinding(typeof(ICommandHandler<GrantLevelUpKnowledgePointCommand>))]`, matching the clean `AwardProgressionHandler` pattern; no change needed.
2. **Cap-rejection test should assert `Update()` was never called** — added an `UpdateCount` property to the shared `InMemoryKnowledgeProgressionRepository` and assert `_progressionRepository.UpdateCount == 0` on the rejected path.
3. **Duplicate test doubles** — removed the two private nested `InMemoryKnowledgeProgressionRepository` / `InMemoryKnowledgeCapProfileRepository` classes from `AwardProgressionCommandTests` and moved them to the shared `SharedKernel.Tests.Helpers` namespace as `public` classes with `Create()` factories. Both command tests now reference the shared doubles.

Build: clean, **0 errors**. All WorldEngine tests pass.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

