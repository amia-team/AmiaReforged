# Subtask 001-F — Wire Trait Onboarding Yes Path to Shared Selection Window

## Goal

Complete the onboarding flow by wiring the onboarding prompt's Yes action to the shared trait-selection window opener from Story 002.

This task contains no new UI and no new selection behavior.

## Dependency

Story 002 must provide:

`TraitSelectionWindowService`

with the existing trait-selection opening behavior centralized there.

## Required Change

In `TraitOnboardingService`, the Yes callback supplied to `TraitOnboardingPromptPresenter` must call:

```csharp
TraitSelectionWindowService.Open(player)
```

or the exact equivalent API implemented by Story 002.

Do not:

- instantiate `TraitSelectionView`,
- instantiate `TraitSelectionPresenter`,
- resolve `InjectionService`,
- call `WindowDirector.OpenWindow` for the trait-selection window directly.

Those responsibilities belong to `TraitSelectionWindowService`.

## Required Behavior

When the player clicks Yes:

1. onboarding prompt closes,
2. reminder preference remains unchanged,
3. shared trait-selection opener is invoked,
4. existing Trait Selection window opens unless already open.

## Failure Behavior

If the selection window cannot be opened using the shared service:

- do not write the reminder opt-out,
- do not mark onboarding complete,
- allow the existing window-opening error behavior to handle/report the failure.

## Constraints

- Do not change the Yes/No prompt copy.
- Do not alter eligibility.
- Do not alter PC-key preference semantics.
- Do not change Trait Selection behavior.
- Do not add another window-opening service.
- Do not duplicate Story 002.

## Acceptance Criteria

- Yes routes exclusively through `TraitSelectionWindowService`.
- Yes never sets `TRAIT_SELECTION_PROMPT_DISABLED`.
- No paths do not open Trait Selection.
- Existing `./traits` and Codex access use the same window-opening service.
