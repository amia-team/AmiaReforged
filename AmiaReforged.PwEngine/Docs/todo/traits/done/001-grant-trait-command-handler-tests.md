# Ticket 001 — Add GrantTraitCommandHandler behavior tests

** DONE **

## Goal

Add direct unit coverage for the domain behavior implemented by:

`Features/WorldEngine/Subsystems/Traits/Application/GrantTraitCommandHandler.cs`

Do not test the command dispatcher. Instantiate `GrantTraitCommandHandler` directly.

## Constraints

- Do not refactor production code.
- Do not modify production behavior.
- Do not introduce new abstractions, interfaces, seams, repositories, or test infrastructure.
- Do not touch NWN/Anvil runtime objects.
- Do not add database tests, EF providers, Testcontainers, or persistence mocks.
- Use existing NUnit conventions.
- Use `InMemoryTraitRepository` and `InMemoryCharacterTraitRepository`.
- Do not use Moq.
- Do not expand this task into neighboring coverage gaps.

## Create

Create:

`Features/WorldEngine/Subsystems/Traits/Tests/GrantTraitCommandHandlerTests.cs`

## Required tests

### `GrantTrait_WhenDefinitionDoesNotExist_FailsWithoutCreatingCharacterTrait`

Arrange:

- Empty `InMemoryTraitRepository`.
- Empty `InMemoryCharacterTraitRepository`.
- A new character ID.
- Command using trait tag `"missing_trait"`.

Assert:

- `CommandResult.Success` is `false`.
- `ErrorMessage` is `"Trait 'missing_trait' does not exist."`
- The character trait repository remains empty for that character.

### `GrantTrait_WhenCharacterAlreadyHasTrait_FailsWithoutCreatingDuplicate`

Arrange:

- Definition `"brave"` exists.
- Character repository already contains one `CharacterTrait` with tag `"brave"` for the target character.

Execute another grant for `"brave"`.

Assert:

- `Success` is `false`.
- Error message identifies that the character already has `"Brave"`.
- Repository still contains exactly one `"brave"` trait for the character.
- The original trait ID remains present.

### `GrantTrait_WhenValid_CreatesConfirmedActiveCharacterTrait`

Arrange a `"brave"` definition with:

- `RequiresUnlock = false`

Capture `DateTime.UtcNow` immediately before and after `HandleAsync`.

Assert:

- Result succeeds.
- Exactly one character trait is created.
- `CharacterId` equals the command character.
- `TraitTag.Value == "brave"`.
- `Id != Guid.Empty`.
- `IsConfirmed == true`.
- `IsActive == true`.
- `IsUnlocked == false`.
- `DateAcquired` falls between the captured before/after timestamps.

### `GrantTrait_WhenDefinitionRequiresUnlock_SeedsIsUnlockedTrue`

Arrange a `"hero"` definition with:

- `RequiresUnlock = true`

Assert the created character trait has:

- `IsConfirmed == true`
- `IsActive == true`
- `IsUnlocked == true`

Do not add additional grant scenarios.

## Acceptance

Run:

```bash
dotnet test --filter FullyQualifiedName~GrantTraitCommandHandlerTests
```

All tests pass.

No production files are changed.
