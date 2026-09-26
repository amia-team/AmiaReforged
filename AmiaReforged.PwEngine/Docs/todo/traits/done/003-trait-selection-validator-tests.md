# Ticket 003 — Cover TraitSelectionValidator race, class, duplicate, and budget rules

## Goal

Add explicit tests for currently under-covered behavior in:

`Features/WorldEngine/Subsystems/Traits/TraitSelectionValidator.cs`

The tests must protect the existing selection rules, including the intentional rule that the budget is not enforced during selection.

## Constraints

- Do not refactor production code.
- Do not modify production behavior.
- Do not introduce new abstractions or helper layers.
- Do not touch NWN/Anvil runtime objects.
- Do not add persistence tests.
- Use existing NUnit conventions.
- Use the existing `TestCharacterInfo`.
- Do not duplicate existing prerequisite, conflict, unlock, or deselection tests from `BackgroundTraitTests`.
- Do not expand this task into adjacent coverage work.

## Create

Create:

`Features/WorldEngine/Subsystems/Traits/Tests/TraitSelectionValidatorTests.cs`

Use:

`Features/WorldEngine/Subsystems/Traits/Tests/TestCharacterInfo.cs`

Use `TraitBudget.CreateDefault()` unless a test explicitly requires another budget.

## Required tests

### `CanSelect_WhenTraitAlreadySelected_ReturnsFalse`

- Trait tag: `"brave"`.
- Existing `CharacterTrait` also has `"brave"`.
- Otherwise-valid Human/Fighter character.
- Assert `false`.

### `CanSelect_WhenRaceIsInAllowedRaces_ReturnsTrue`

- `AllowedRaces = ["Human"]`
- Character race `"Human"`
- No other restrictions.
- Assert `true`.

### `CanSelect_WhenRaceIsNotInAllowedRaces_ReturnsFalse`

- `AllowedRaces = ["Elf"]`
- Character race `"Human"`
- Assert `false`.

### `CanSelect_WhenRaceIsForbidden_ReturnsFalse`

- `ForbiddenRaces = ["Human"]`
- Human character.
- Assert `false`.

### `CanSelect_WhenRaceIsBothAllowedAndForbidden_ReturnsFalse`

- `AllowedRaces = ["Human"]`
- `ForbiddenRaces = ["Human"]`
- Human character.
- Assert `false`.

### `CanSelect_WhenAnyCharacterClassIsAllowed_ReturnsTrue`

- `AllowedClasses = ["Wizard"]`
- Character classes: `"Fighter", "Wizard"`
- Assert `true`.

### `CanSelect_WhenNoCharacterClassIsAllowed_ReturnsFalse`

- `AllowedClasses = ["Wizard"]`
- Character classes: `"Fighter", "Rogue"`
- Assert `false`.

### `CanSelect_WhenAnyCharacterClassIsForbidden_ReturnsFalse`

- `ForbiddenClasses = ["Wizard"]`
- Character classes: `"Fighter", "Wizard"`
- Assert `false`.

### `CanSelect_WhenClassIsBothAllowedAndForbidden_ReturnsFalse`

- `AllowedClasses = ["Wizard"]`
- `ForbiddenClasses = ["Wizard"]`
- Character includes `"Wizard"`.
- Assert `false`.

### `CanSelect_WhenTraitCostsMoreThanAvailableBudget_StillReturnsTrue`

Arrange:

- Otherwise-valid unrestricted trait.
- `PointCost = 10`.
- Default budget has only 2 available points.
- No existing selections.

Assert:

- `TraitSelectionValidator.CanSelect(...)` returns `true`.

This test explicitly protects the current design: budget rejection occurs during confirmation, not selection.

## Acceptance

Run:

```bash
dotnet test --filter FullyQualifiedName~TraitSelectionValidatorTests
```

All listed tests pass.

No production files are changed.
