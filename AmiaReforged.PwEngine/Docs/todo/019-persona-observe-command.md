# 019 — Dispatch player-persona observation

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: None.

## Current gap

`ObservePlayerPersona` directly calls the persistent persona repository.

## Change

Wrap observation/upsert in a command carrying captured identity, display name, and observation time. Route login/re-cache adapters through dispatch.

## Starting points

- [Subsystems/Characters/Runtime/RuntimeCharacterService.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacterService.cs)

## Acceptance checks

- [x] First observation and repeated observation preserve existing upsert behavior.
- [x] Invalid/empty identity retains its current rejection behavior.
- [x] The adapter no longer calls `Upsert`; a successful command publishes the generic event.

## Completion evidence

**Behavior decided:** Observation is now performed by dispatching `ObservePlayerPersonaCommand(cdKey, displayName, observedUtc)` through the existing `ICommandDispatcher`. The upsert logic itself is unchanged and lives in the handler, which delegates to `IPersistentPlayerPersonaRepository.Upsert`. The NWN boundary (player validity / empty CD key rejection, capturing display name) stays in `RuntimeCharacterService.ObservePlayerPersona` as a guard before dispatch, so invalid/empty identities are still rejected before any command runs. On a failed command result the service logs a warning, matching the prior behavior; the dispatcher publishes `CommandExecutedEvent<ObservePlayerPersonaCommand>` on success (the "generic event").

**Changed files:**

- `.../Characters/Runtime/Commands/ObservePlayerPersonaCommand.cs` (new) — command record carrying captured identity, display name, observation time.
- `.../Characters/Runtime/Commands/ObservePlayerPersonaCommandHandler.cs` (new) — handler that upserts via `IPersistentPlayerPersonaRepository`; service-bound via `ICommandHandlerMarker`.
- `.../Characters/Runtime/RuntimeCharacterService.cs` — `ObservePlayerPersona` now dispatches the command instead of calling `Upsert` directly; kept `async void`, rejection guards unchanged.
- `.../Characters/Runtime/Tests/InMemoryPersistentPlayerPersonaRepository.cs` (new) — recording test double for the persistent persona repository.
- `.../Characters/Runtime/Tests/ObservePlayerPersonaCommandHandlerTests.cs` (new) — handler tests (first + repeated observation, captured values forwarded).

**Verification:**

```bash
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj -c Debug
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj -c Debug \
  --filter "FullyQualifiedName~ObservePlayerPersonaCommandHandlerTests"
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj -c Debug \
  --filter "FullyQualifiedName~Characters"
```

**Result:** Build succeeded (0 errors). `ObservePlayerPersonaCommandHandlerTests` — 2 passed. Full `Characters` suite — 24 passed, 0 failed.

**Related:** `020-persona-touch-command.md` applies the same command-dispatch pattern to logout activity (`Touch`).

See [backlog scope and completion rules](README.md).

