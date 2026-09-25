# Ticket 004 — Cover TraitBudget value-object operations

## Goal

Add missing direct coverage for the value-object operations in:

`Features/WorldEngine/Subsystems/Traits/TraitBudget.cs`

Do not change `TraitBudget`.

## Constraints

- Do not refactor production code.
- Do not modify production behavior.
- Do not introduce validation that does not already exist.
- Do not touch NWN/Anvil runtime objects.
- Do not add persistence tests.
- Use existing NUnit conventions.
- Do not duplicate the basic default-budget tests already present in `BackgroundTraitTests`.
- Do not expand this task into neighboring coverage gaps.

## Create

Create:

`Features/WorldEngine/Subsystems/Traits/Tests/TraitBudgetTests.cs`

## Required tests

### `CanAfford_WhenCostEqualsAvailablePoints_ReturnsTrue`

Arrange:

- `EarnedPoints = 1`
- `SpentPoints = 1`

This produces:

- `TotalPoints = 3`
- `AvailablePoints = 2`

Assert `budget.CanAfford(2)` is `true`.

### `CanAfford_WhenCostExceedsAvailablePoints_ReturnsFalse`

Using the same budget, assert `CanAfford(3)` is `false`.

### `WithSpentPoints_ReplacesSpentPointsAndPreservesEarnedPoints`

Arrange:

- `EarnedPoints = 3`
- `SpentPoints = 1`

Call:

```csharp
WithSpentPoints(4)
```

Assert returned budget:

- `EarnedPoints = 3`
- `SpentPoints = 4`

Also assert the original remains:

- `EarnedPoints = 3`
- `SpentPoints = 1`

### `AfterSpending_AddsToExistingSpentPointsAndPreservesEarnedPoints`

Arrange:

- `EarnedPoints = 2`
- `SpentPoints = 1`

Call:

```csharp
AfterSpending(3)
```

Assert returned budget:

- `EarnedPoints = 2`
- `SpentPoints = 4`

Original remains unchanged.

### `WithEarnedPoints_AddsToExistingEarnedPointsAndPreservesSpentPoints`

Arrange:

- `EarnedPoints = 2`
- `SpentPoints = 1`

Call:

```csharp
WithEarnedPoints(3)
```

Assert returned budget:

- `EarnedPoints = 5`
- `SpentPoints = 1`

Original remains unchanged.

Do not add speculative validation for negative earned/spent values. Current production code permits them.

## Acceptance

Run:

```bash
dotnet test --filter FullyQualifiedName~TraitBudgetTests
```

All tests pass.

No production files are changed.
