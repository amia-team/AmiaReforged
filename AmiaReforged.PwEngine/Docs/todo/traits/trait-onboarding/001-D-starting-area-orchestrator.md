# Subtask 001-D — Add Starting-Area Trait Onboarding Orchestrator

## Goal

Add the runtime service that decides whether to show the trait onboarding prompt when a character enters the server starting area.

This task must use area `OnEnter`, not module client-enter.

## Create

Create:

`Features/WorldEngine/Subsystems/Traits/TraitOnboardingService.cs`

Register it through the existing Anvil service mechanism.

## Event Subscription

In the service constructor, subscribe exactly to:

```csharp
NwArea entryArea = NwModule.Instance.StartingLocation.Area;
entryArea.OnEnter += OnEnter;
```

Do not subscribe to:

```csharp
NwModule.Instance.OnClientEnter
```

Do not add any other login event subscription.

## OnEnter Guards

For each `AreaEvents.OnEnter` event:

1. Require the entering object to be a player-controlled login character.
2. Resolve its `NwPlayer`.
3. Return for DMs.
4. Require `player.LoginCreature` to be non-null.
5. Find the existing `ds_pckey` in the login creature inventory.
6. If no PC key exists, return.
7. Resolve the character GUID with `PcKeyUtils.GetPcKey(player)`.
8. If the GUID is empty, return.

Do not manually parse the PC-key item name.

## Eligibility Check

Load current character traits through the existing character-trait repository/service.

Pass them to:

```csharp
TraitOnboardingEligibility.ShouldPrompt(...)
```

If it returns false:

- return.

## Preference Check

Call:

```csharp
TraitOnboardingPreference.IsDisabled(pcKey)
```

If true:

- return.

## Duplicate Prompt Check

Before opening the prompt, use `WindowDirector` to ensure a `TraitOnboardingPromptPresenter` is not already open for the player.

If already open:

- return.

## Open Prompt

Create/open the dedicated `TraitOnboardingPromptView`.

Wire callbacks as follows.

### Yes callback

Delegate to Story 001-F / `TraitSelectionWindowService`.

Do not construct `TraitSelectionView` in this service.

### Suppression callback

Call:

```csharp
TraitOnboardingPreference.Disable(pcKey)
```

Then send exactly:

```text
Trait selection reminders disabled. You can select traits at any time from the Traits section of your Codex.
```

## Error Handling

A failure to load traits or resolve the character must not crash the area event.

Log the failure and return.

Do not teleport the player or interrupt character registration.

## Constraints

- No `NwModule.Instance.OnClientEnter`.
- No new character-registration flow.
- No budget-based prompt decision.
- No database preference persistence.
- No direct construction of the trait-selection NUI.
- No automatic trait selection.
- No changes to `CharacterRegistrationService`.
- No changes to `PlayerToolsService`.
- No changes to trait confirmation rules.

## Acceptance Criteria

- Entering the starting area triggers the eligibility check.
- DMs are ignored.
- Missing/invalid PC keys are ignored safely.
- Confirmed-trait characters are not prompted.
- Unconfirmed/no-trait characters are prompted unless opted out.
- Opted-out characters are not prompted.
- Duplicate onboarding prompts do not open.
- Module `OnClientEnter` is not used anywhere in this implementation.
