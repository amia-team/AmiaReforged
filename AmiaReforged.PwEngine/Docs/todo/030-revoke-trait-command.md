# 030 — Dispatch trait removal

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Traits**
Depends on: None.

## Current gap

`RemoveTraitAsync` locates and deletes character traits directly.

## Change

Add a revoke/remove command/handler and route the subsystem method through dispatch.

## Starting points

- [Subsystems/Implementations/TraitSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/TraitSubsystem.cs)

## Acceptance checks

- [ ] Removing an owned trait persists removal.
- [ ] Removing an absent trait preserves the current failure contract.
- [ ] Success publishes the generic event and no removal repository logic remains in the subsystem.

## Completion evidence

**Behavior:** `RemoveTraitAsync` now dispatches a `RemoveTraitCommand` to the command bus. The `RemoveTraitCommandHandler` locates the character's trait selection via `ICharacterTraitRepository.GetByCharacterId`, fails with the same contract message if the character does not hold it, otherwise deletes the selection (`ICharacterTraitRepository.Delete`) and returns `Ok()`. The dispatcher publishes the generic `CommandExecutedEvent` on success. No removal logic remains in the subsystem.

**Changed files:**
- `Features/WorldEngine/Subsystems/Traits/Commands/RemoveTraitCommand.cs` (new)
- `Features/WorldEngine/Subsystems/Traits/Application/RemoveTraitCommandHandler.cs` (new)
- `Features/WorldEngine/Subsystems/Implementations/TraitSubsystem.cs` (routed `RemoveTraitAsync` through `_commandDispatcher.DispatchAsync`)

**Verification:** `dotnet build` was not runnable (no shell access to `dotnet` in this environment). Manual/CI verification: confirm the project builds; assert `RemoveTraitCommandHandler` is discovered via `[ServiceBinding(typeof(ICommandHandler<RemoveTraitCommand>))]` + marker; assert the subsystem no longer references `_characterTraitRepository.Delete`/`GetByCharacterId` for removal. See [backlog scope and completion rules](README.md).

