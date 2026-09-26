# Story 001 — Trait Selection Onboarding Prompt

## Summary

Add a simple onboarding workflow for player characters that have not completed trait selection.

When a non-DM player character enters the server starting area, the game checks whether that character has any confirmed trait selections. If not, and the player has not disabled the reminder for that character, show a small prompt asking whether they would like to select traits.

The prompt does not force trait selection.

## Player Flow

The required player flow is:

```text
Join with any character, new or existing
        │
        ▼
Character enters the starting area
        │
        ▼
Game checks whether trait selection has been completed
        │
        ├── confirmed trait exists
        │       └── do nothing
        │
        └── no confirmed trait exists
                │
                ▼
        Check reminder preference on PC key
                │
                ├── reminder disabled
                │       └── do nothing
                │
                └── reminder enabled/default
                        │
                        ▼
                Open "Select Traits?" prompt
                        │
                        ├── YES
                        │       └── open Trait Selection window
                        │
                        └── NO
                                │
                                ├── "Don't show again" unchecked
                                │       └── close prompt; persist nothing
                                │
                                └── "Don't show again" checked
                                        ├── persist reminder opt-out
                                        ├── close prompt
                                        └── tell player they can open
                                            trait selection from the Codex
```

## Trigger

Use:

```csharp
NwModule.Instance.StartingLocation.Area.OnEnter
```

Do not use:

```csharp
NwModule.Instance.OnClientEnter
```

The starting-area event is the required boundary because `Player.LoginCreature` cannot be assumed to exist during module client-enter handling.

Existing code already uses this pattern in `PlayerToolsService`.

## Eligibility Rule

A character is considered to have completed trait selection when the character has at least one confirmed trait:

```csharp
bool hasCompletedTraitSelection =
    characterTraits.Any(t => t.IsConfirmed);
```

If at least one confirmed trait exists:

- do not show the onboarding prompt.

If no confirmed trait exists:

- the character is eligible for the onboarding prompt unless reminders were disabled.

Do not use remaining trait budget as the onboarding completion check.

Do not require `characterTraits.Count == 0`.

## Reminder Preference

Persist the per-character reminder preference on the character's `ds_pckey` item using:

```text
TRAIT_SELECTION_PROMPT_DISABLED
```

Semantics:

```text
missing / 0 = reminders enabled
1           = reminders disabled
```

Only this action writes `1`:

```text
player clicks No
AND
"Don't show this reminder again" is checked
```

The following actions must not change the preference:

- Yes
- No without the checkbox
- closing the popup using the window close control

## Prompt

Create a dedicated Traits onboarding prompt.

Do not reuse `SimplePopupView`; its existing ignore-button behavior is hard-coded to the unrelated `ignore_caster_forge` PC-key variable.

Do not reuse `ConfirmationPopupView`; it does not provide the required checkbox.

Required prompt content:

Title:

```text
Select Traits
```

Body:

```text
You have not selected traits for this character. Would you like to select them now?
```

Controls:

```text
[ ] Don't show this reminder again

[ Yes ] [ No ]
```

### Yes

- Close the prompt.
- Open the existing trait-selection window.
- Do not modify `TRAIT_SELECTION_PROMPT_DISABLED`.

### No, checkbox unchecked

- Close the prompt.
- Persist nothing.
- The player is eligible to see the reminder again the next time the character enters through the starting area.

### No, checkbox checked

- Set `TRAIT_SELECTION_PROMPT_DISABLED = 1` on `ds_pckey`.
- Close the prompt.
- Send:

```text
Trait selection reminders disabled. You can select traits at any time from the Traits section of your Codex.
```

### Window close control

Closing the prompt without choosing Yes or No is equivalent to No without the checkbox:

- close only,
- persist nothing.

## Character Identity

Use the existing PC-key utilities.

Do not duplicate PC-key parsing.

Use:

```csharp
PcKeyUtils.GetPcKey(player)
```

where a `CharacterId` is needed.

If the player has no valid PC key:

- do not show the trait onboarding prompt,
- do not throw.

## Codex Requirement

The player-facing message says trait selection can be opened from the Codex.

Therefore the Codex Traits tab must provide a visible `Select Traits` action that opens the existing trait-selection window.

Do not make the Codex itself implement trait selection.

The Codex action is only another entry point into the existing trait-selection NUI.

## Shared Trait-Selection Window Opening

Do not duplicate construction/injection/opening of `TraitSelectionView`.

Story 002 provides `TraitSelectionWindowService`.

Both:

- `./traits`
- Story 001 onboarding/Codex actions

must use that shared service.

## Scope

Story 001 includes only:

- eligibility determination,
- per-character reminder preference,
- onboarding prompt NUI,
- starting-area orchestration,
- Codex Traits-tab action,
- wiring the prompt Yes path into the shared trait-selection window service.

## Non-Goals

Do not:

- force a player to select traits,
- automatically select traits,
- prompt based on remaining free trait points,
- redesign trait selection,
- alter confirmation rules,
- alter trait budget rules,
- add a new database table,
- add a general preferences subsystem,
- use module `OnClientEnter`,
- refactor unrelated character initialization,
- change DM trait workflows,
- change AdminPanel trait definition management.

## Acceptance Criteria

- A new or existing non-DM character with no confirmed traits is prompted on entering the starting area.
- A character with at least one confirmed trait is not prompted.
- Yes opens the existing trait-selection window.
- No without opt-out persists nothing.
- No with opt-out persists `TRAIT_SELECTION_PROMPT_DISABLED = 1` on `ds_pckey`.
- A character with the opt-out set is not prompted on future starting-area entries.
- Closing the prompt persists nothing.
- The Codex Traits tab contains a `Select Traits` action.
- The Codex action opens the same existing trait-selection NUI.
- No code in this story subscribes to `NwModule.Instance.OnClientEnter`.
- No new database persistence mechanism is introduced.
