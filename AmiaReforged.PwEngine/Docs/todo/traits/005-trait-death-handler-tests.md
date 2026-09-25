# Ticket 005 — Cover remaining TraitDeathHandler lifecycle branches

## Goal

Cover the remaining pure trait lifecycle behavior in:

`Features/WorldEngine/Subsystems/Traits/TraitDeathHandler.cs`

Existing tests already cover:

- `Persist`
- `ResetOnDeath`
- Hero reactivation
- `Permadeath` killed by Hero
- `Permadeath` killed by non-Hero

Do not duplicate those cases.

## Constraints

- Do not refactor production code.
- Do not modify production behavior.
- Do not introduce new abstractions, seams, or event harnesses.
- Do not touch NWN/Anvil runtime objects.
- Do not add NWN death-event tests.
- Use the existing in-memory trait repositories.
- Use existing NUnit conventions.
- Do not expand this task into neighboring coverage gaps.

## Modify

Add the following tests to:

`Features/WorldEngine/Subsystems/Traits/Tests/BackgroundTraitTests.cs`

Keep them inside the existing death/lifecycle region.

## Required tests

### `TraitWithRemoveOnDeathBehavior_IsDeletedOnDeath`

Arrange:

- Trait definition `"temporary"` with `DeathBehavior.RemoveOnDeath`.
- Character owns a confirmed, active `"temporary"` trait.

Call `ProcessDeath`.

Assert:

- Return value is `false`.
- Character no longer has `"temporary"`.

### `RemoveOnDeath_RemovesOnlyTheMatchingTrait`

Arrange one character with:

- `"temporary"` using `RemoveOnDeath`
- `"brave"` using `Persist`

Call `ProcessDeath`.

Assert:

- `"temporary"` is gone.
- `"brave"` remains active.
- Exactly one character trait remains.

### `ProcessDeath_WhenCharacterTraitDefinitionIsMissing_LeavesTraitUnchanged`

Arrange:

- Character repository contains `"orphaned"`.
- Trait repository does not contain `"orphaned"`.

Call `ProcessDeath`.

Assert:

- Return value is `false`.
- `"orphaned"` remains in the character repository.
- Its `IsActive` value is unchanged.
- Its `CustomData` value is unchanged.

### `ReactivateResettableTraits_DoesNotReactivateNonResettableTrait`

Arrange:

- Definition `"brave"` uses `Persist`.
- Character's `"brave"` record is inactive.

Call `ReactivateResettableTraits`.

Assert `"brave"` remains inactive.

### `ReactivateResettableTraits_WhenDefinitionIsMissing_LeavesTraitInactive`

Arrange:

- Inactive `"orphaned"` character trait.
- No matching definition.

Call `ReactivateResettableTraits`.

Assert it remains inactive.

## Acceptance

Run:

```bash
dotnet test --filter FullyQualifiedName~BackgroundTraitTests
```

All existing and new tests pass.

No production files are changed.
