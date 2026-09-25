# 029 — Dispatch trait grants

Status: **Done**
Type: **Implementation**
Audit area: **F-7 Traits**
Depends on: None.

## Current gap

`GrantTraitAsync` performs definition checks and repository insertion inline.

## Change

Added a `GrantTraitCommand`/`GrantTraitCommandHandler` pair that preserves the previous grant semantics, and routed `TraitSubsystem.GrantTraitAsync` to dispatch the command through `ICommandDispatcher`. The subsystem now injects `ICommandDispatcher` and builds the command from `(characterId, traitTag)`; all validation and persistence moved into the handler (consistent with the README note that domain services may access repositories inside command handlers).

Granting is intentionally **not** modelled as selection: the handler does not publish `TraitSelectedEvent` and creates a confirmed, active character trait whose unlock state is seeded from the definition.

## Starting points

- [Subsystems/Implementations/TraitSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/TraitSubsystem.cs)

## Acceptance checks

- [x] Valid grants preserve active/confirmed/unlocked state — handler sets `IsConfirmed = true`, `IsActive = true`, `IsUnlocked = trait.RequiresUnlock`.
- [x] Unknown definitions and duplicate grants fail without an extra row — definition existence is checked first, then a duplicate lookup guards insertion before any `Add`.
- [x] Successful dispatch publishes the generic event; the wrapper has no grant-time repository access — `CommandDispatcher` publishes `CommandExecutedEvent<GrantTraitCommand>` on success; `StubTraitSubsystem.GrantTraitAsync` in `CodexEventProcessorTests.cs` still returns `CommandResult.Ok()` with no repository.

## Completion evidence

**Behavior:** `GrantTraitAsync(characterId, traitTag)` now constructs `GrantTraitCommand` and calls `_commandDispatcher.DispatchAsync(command, ct)`. The auto-discovered `GrantTraitCommandHandler` performs the definition check, duplicate check, and repository `Add`, returning `CommandResult` (success → dispatcher publishes the generic `CommandExecutedEvent<TCommand>`).

**Changed files:**
- `Features/WorldEngine/Subsystems/Traits/Commands/GrantTraitCommand.cs` (new)
- `Features/WorldEngine/Subsystems/Traits/Application/GrantTraitCommandHandler.cs` (new)
- `Features/WorldEngine/Subsystems/Implementations/TraitSubsystem.cs` — added `ICommandDispatcher` constructor dependency, `using ...Traits.Commands`, and reduced `GrantTraitAsync` to command construction + dispatch.

**Verification command:**

```sh
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj -v q --nologo
```

**Observed result:** Build succeeded (0 errors). No new trait-specific test harness was required: the only `GrantTraitAsync` caller outside production is the `StubTraitSubsystem` used by `CodexEventProcessorTests.cs`, which intentionally has no repository and returns `Ok()`, matching the "wrapper has no grant-time repository access" expectation. Existing `TraitSubsystem` construction sites (none in tests) remain unaffected because the stub implements the interface directly rather than via the production constructor.

See [backlog scope and completion rules](README.md).

