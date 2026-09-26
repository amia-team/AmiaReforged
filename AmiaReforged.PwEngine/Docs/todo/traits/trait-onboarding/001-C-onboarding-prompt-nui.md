# Subtask 001-C — Add Trait Onboarding Prompt NUI

## Goal

Add the dedicated Scry popup used to ask whether the player wants to select traits.

This task implements only the popup and its exact interaction behavior.

## Create

Under:

`Features/WorldEngine/Subsystems/Traits/Nui/`

create:

- `TraitOnboardingPromptView.cs`
- `TraitOnboardingPromptPresenter.cs`

Do not reuse or modify `SimplePopupView`.

Do not reuse or modify `ConfirmationPopupView`.

## Required Window

Title:

```text
Select Traits
```

Body:

```text
You have not selected traits for this character. Would you like to select them now?
```

Controls:

- checkbox labeled `Don't show this reminder again`
- `Yes` button
- `No` button

The window must remain small and modal-like. Do not add trait lists, trait descriptions, budget information, or category controls.

## Presenter Inputs

The presenter must receive explicit callbacks/actions for:

- Yes
- No with suppression requested

Do not make the presenter query trait repositories.

Do not make the presenter decide eligibility.

## Yes Behavior

When Yes is clicked:

1. close the onboarding prompt,
2. invoke the Yes callback,
3. do not write any preference value.

## No Behavior

When No is clicked:

1. read the checkbox,
2. if unchecked:
   - close the prompt,
   - invoke no suppression callback,
   - persist nothing;
3. if checked:
   - close the prompt,
   - invoke the suppression callback.

The suppression callback is responsible for writing the preference.

## Window Close Behavior

If the player closes the window through the normal close control:

- close the window,
- do not invoke Yes,
- do not invoke suppression,
- persist nothing.

## Checkbox Default

The checkbox must default to:

```text
false
```

## Constraints

- Do not open `TraitSelectionView` directly from this presenter.
- Do not access `ds_pckey` from the view.
- Do not query `ICharacterTraitRepository`.
- Do not calculate budgets.
- Do not add a generic popup framework.
- Do not alter existing generic popup classes.
- Do not add an automatic timeout.

## Acceptance Criteria

- Prompt contains the exact title, body, checkbox label, Yes, and No controls.
- Yes invokes only the Yes action.
- No + unchecked persists nothing.
- No + checked invokes only the suppression action.
- Window close persists nothing.
- No trait-selection business logic exists in the prompt.
