# Ticket 002 — Add RemoveTraitCommandHandler behavior tests

## Goal

Add direct unit coverage for:

`Features/WorldEngine/Subsystems/Traits/Application/RemoveTraitCommandHandler.cs`

Instantiate the handler directly.

## Constraints

- Do not refactor production code.
- Do not modify production behavior.
- Do not introduce new abstractions, interfaces, seams, repositories, or test infrastructure.
- Do not touch NWN/Anvil runtime objects.
- Do not add database tests, EF providers, Testcontainers, or persistence mocks.
- Use existing NUnit conventions.
- Use `InMemoryCharacterTraitRepository`.
- Do not use Moq.
- Do not expand this task into neighboring coverage gaps.

## Create

Create:

`Features/WorldEngine/Subsystems/Traits/Tests/RemoveTraitCommandHandlerTests.cs`

## Required tests

### `RemoveTrait_WhenCharacterDoesNotHaveTrait_Fails`

Arrange:

- Target character has no traits.
- Remove command requests `"brave"`.

Assert:

- `Success == false`.
- `ErrorMessage == "Character does not have trait 'brave'."`
- Repository remains empty.

### `RemoveTrait_WhenCharacterHasTrait_RemovesMatchingTrait`

Arrange target character with:

- `"brave"`
- `"strong"`

Remove `"brave"`.

Assert:

- Result succeeds.
- `"brave"` no longer exists for the character.
- `"strong"` still exists.
- Exactly one trait remains.

### `RemoveTrait_DoesNotRemoveSameTraitFromAnotherCharacter`

Arrange:

- Character A has `"brave"`.
- Character B has `"brave"`.

Execute removal for Character A.

Assert:

- Character A has no `"brave"` trait.
- Character B still has exactly one `"brave"` trait with its original ID.

Do not test repository implementation details such as calls to `Delete`.

## Acceptance

Run:

```bash
dotnet test --filter FullyQualifiedName~RemoveTraitCommandHandlerTests
```

All tests pass.

No production files are changed.
