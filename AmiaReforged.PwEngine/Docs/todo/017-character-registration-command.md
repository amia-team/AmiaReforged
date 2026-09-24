# 017 — Dispatch persistent character registration

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: None.

## Current gap

Area-entry registration performs persistence directly, while the subsystem registration method always fails.

## Change

Add a registration command/handler covering create and missing-persona backfill. Make the area-enter adapter supply required identity/name data and dispatch. Reconcile the ID-only subsystem signature explicitly; do not invent unavailable registration data.

## Starting points

- [Subsystems/Characters/CharacterRegistrationService.cs](../../Features/WorldEngine/Subsystems/Characters/CharacterRegistrationService.cs)
- [Subsystems/Implementations/CharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs)

## Acceptance checks

- [ ] Tests cover new registration, existing registration, and persona backfill without duplication.
- [ ] The NWN adapter retains missing-key/DM checks but performs no repository writes.
- [ ] The public subsystem registration contract is either usable via dispatch or explicitly retired with callers updated.

## Completion evidence

**Status: Completed** (commit `8a2dc286a` — "Dispatch persistent character registration").

### Decision
Registration is now event-driven and command-dispatched rather than persisted directly from the area-enter adapter or via the (always-failing) subsystem method.
- The NWN adapter (`CharacterRegistrationService`) keeps the missing-PC-key / DM / login checks but only **builds and dispatches** a `RegisterCharacterCommand`; it performs no repository writes.
- `RegisterCharacterCommandHandler` owns all persistence: it **creates** a new `PersistedCharacter` when none exists, **backfills** a missing/whitespace `PersonaIdString` on an existing record, and is a **no-op** otherwise. It never overwrites names or CD key on an existing character.
- The old `ICharacterSubsystem.RegisterCharacterAsync` (which always returned `CommandResult.Fail`) is **retired** — removed from both the interface and `CharacterSubsystem`, along with its now-unused `CharacterRegistrationService` dependency.

### Changed files
- `Features/WorldEngine/Subsystems/Characters/Application/RegisterCharacterCommand.cs` (new)
- `Features/WorldEngine/Subsystems/Characters/Application/RegisterCharacterCommandHandler.cs` (new)
- `Features/WorldEngine/Subsystems/Characters/Application/Tests/RegisterCharacterCommandHandlerTests.cs` (new)
- `Features/WorldEngine/Subsystems/Characters/CharacterRegistrationService.cs` (dispatches command; no repo writes)
- `Features/WorldEngine/Subsystems/ICharacterSubsystem.cs` (removed `RegisterCharacterAsync`)
- `Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs` (removed failing impl + dependency)
- `Database/.../InMemoryPersistentCharacterRepository.cs` (test support: `AddedCharacters`, `PersonaIdUpdates`)

### Acceptance checks
- [x] Tests cover new registration, existing-registration no-op, and persona backfill (incl. null/whitespace) without duplication — `RegisterCharacterCommandHandlerTests` (6 tests).
- [x] NWN adapter retains missing-key/DM checks and performs no repository writes (verified by `Service_Dependencies_AreCommandDispatcherNotRepository` test + source).
- [x] Public subsystem registration contract explicitly retired; callers updated (no remaining references to `RegisterCharacterAsync`).

### Verification
```
dotnet build AmiaReforged.PluginTests/AmiaReforged.PluginTests.csproj -c Debug
```
Result: **Build succeeded, 0 warnings, 0 errors.** (Unit tests run under the NWN.Anvil.TestRunner inside the game client and are not executed headless.)

See [backlog scope and completion rules](README.md).

