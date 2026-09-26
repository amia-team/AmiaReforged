# Subtask 001-B — Add Trait Onboarding Prompt Preference

## Goal

Add the exact per-character preference used to suppress future trait-selection onboarding reminders.

Store the preference on the character's existing `ds_pckey`.

## Variable

Use exactly:

```text
TRAIT_SELECTION_PROMPT_DISABLED
```

Values:

```text
0 or missing = reminders enabled
1            = reminders disabled
```

## Required Implementation

Create:

`Features/WorldEngine/Subsystems/Traits/Nui/TraitOnboardingPreference.cs`

Expose:

```csharp
public static bool IsDisabled(NwItem pcKey)
public static void Disable(NwItem pcKey)
```

Behavior:

```csharp
IsDisabled(pcKey)
```

returns true only when the local integer is `1`.

```csharp
Disable(pcKey)
```

sets the local integer to `1`.

Do not add an Enable method in this task.

## Required Usage Contract

The preference is changed only when the onboarding prompt receives:

```text
No + "Don't show this reminder again" checked
```

These actions must never change the preference:

- Yes
- No with checkbox unchecked
- closing the prompt window

## Constraints

- Do not add a database table.
- Do not add a general preferences service.
- Do not store the setting on `NwCreature`.
- Do not use a different variable name.
- Do not change existing Codex preferences.
- Do not reuse `ignore_caster_forge`.
- Do not modify `SimplePopupPresenter`.

## Acceptance Criteria

- `TRAIT_SELECTION_PROMPT_DISABLED` is the only local variable used for this preference.
- Disabled state is persisted on `ds_pckey`.
- Missing/zero reads as enabled.
- Value `1` reads as disabled.
- No unrelated preference code is changed.
