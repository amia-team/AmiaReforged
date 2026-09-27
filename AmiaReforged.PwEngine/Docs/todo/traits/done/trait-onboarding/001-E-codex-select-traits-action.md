# Subtask 001-E — Add Select Traits Action to Codex Traits Tab

## Goal

Make the onboarding opt-out message true by adding a manual `Select Traits` action to the player Codex Traits tab.

The Codex must not implement trait selection itself.

## Existing Files

Modify the existing player Codex NUI only as required:

- `Features/WorldEngine/Subsystems/Codex/Nui/Player/PlayerCodexView.cs`
- `Features/WorldEngine/Subsystems/Codex/Nui/Player/PlayerCodexPresenter.cs`

## Required UI

Add a button labeled exactly:

```text
Select Traits
```

The button must be visible when the active Codex tab is:

```csharp
CodexTab.Traits
```

It must not be visible on:

- Knowledge
- Quests
- Notes
- Reputation
- Economy

## Required Action

Clicking `Select Traits` must open the existing player trait-selection window through:

`TraitSelectionWindowService`

Do not construct `TraitSelectionView` directly.

Do not add trait-selection logic to `PlayerCodexPresenter`.

## Window Behavior

If the trait-selection window is already open, rely on `TraitSelectionWindowService` to prevent a duplicate.

The Codex may remain open. Do not add automatic Codex closing behavior.

## Constraints

- Do not modify Codex trait query semantics.
- Do not modify `CodexTraitEntry`.
- Do not make Codex traits the source of truth for gameplay traits.
- Do not add trait selection fields into the Codex.
- Do not add a second trait-selection UI.
- Do not expose DM-only trait definitions.
- Do not change other Codex tabs.

## Acceptance Criteria

- Traits tab visibly offers `Select Traits`.
- Other tabs do not show the action.
- Clicking the action opens the existing trait-selection NUI.
- Duplicate trait-selection windows are prevented by the shared opener.
- Existing Codex trait display remains unchanged.
