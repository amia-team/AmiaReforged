# 020 — Dispatch player-persona activity updates

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: None.

## Current gap

`TouchPlayerPersona` writes logout activity directly.

## Change

Add an activity-touch command and dispatch it from the logout adapter. Preserve current timestamp semantics and graceful failure handling.

## Starting points

- [Subsystems/Characters/Runtime/RuntimeCharacterService.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacterService.cs)

## Acceptance checks

- [x] A known persona receives the supplied activity timestamp.
- [x] Missing/invalid identity and repository failure have explicit tested results.
- [x] Logout no longer calls the persistent repository's `Touch` directly.

## Completion evidence

**Behavior added.** New `TouchPlayerPersonaCommand(CdKey, ActivatedUtc)` command plus
`TouchPlayerPersonaCommandHandler`, which calls
`IPersistentPlayerPersonaRepository.Touch(cdKey, activatedUtc)`. Both are registered with
`[ServiceBinding]` for automatic Anvil DI discovery, matching the `ObservePlayerPersona`
pattern.

`RuntimeCharacterService.Unregister` now guards at the NWN boundary (player validity /
empty CDKey) and, when valid, dispatches `TouchPlayerPersonaCommand` with
`DateTime.UtcNow`. The service no longer calls the persistent repository's `Touch` directly;
the NWN-boundary guards stay in the service so invalid/empty identities are rejected before
any command runs. A failed command result logs a warning, matching the prior graceful-failure
behavior.

**Changed files:**

- `Features/WorldEngine/Subsystems/Characters/Runtime/Commands/TouchPlayerPersonaCommand.cs` (new)
- `Features/WorldEngine/Subsystems/Characters/Runtime/Commands/TouchPlayerPersonaCommandHandler.cs` (new)
- `Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacterService.cs` (`Unregister` dispatches the command)
- `Features/WorldEngine/Subsystems/Characters/Runtime/Tests/TouchPlayerPersonaCommandHandlerTests.cs` (new, 3 tests)
- `Features/WorldEngine/Subsystems/Characters/Runtime/Tests/InMemoryPersistentPlayerPersonaRepository.cs` (records `TouchCalls`)

**Verification.**

Command:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-restore --filter "FullyQualifiedName~TouchPlayerPersonaCommandHandlerTests" --verbosity minimal
```

Result: passed — 3/3 `TouchPlayerPersonaCommandHandlerTests` (known persona receives the
supplied timestamp; missing/invalid key and repository-failure cases return explicit
`CommandResult` failures).

Full-suite confirmation:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --no-restore -m:1
```

Result: `Passed! - Failed: 0, Passed: 2062, Skipped: 0, Total: 2062`. Build: succeeded, 0 errors.

See [backlog scope and completion rules](README.md).

