# Story 002 — Shared Trait Selection Window Opener

## Summary

Move the existing trait-selection NUI construction, dependency injection, duplicate-window check, and window opening into one reusable service.

Both `./traits` and automatic trait onboarding must call this service.

## Existing Source

The current opening logic is in:

`Features/Chat/Commands/Player/TraitSelectionCommand.cs`

It currently checks for an existing `TraitSelectionPresenter`, creates `TraitSelectionView`, resolves/injects the presenter, and opens it through `WindowDirector`.

## Required Implementation

Create:

`Features/WorldEngine/Subsystems/Traits/Nui/TraitSelectionWindowService.cs`

Register it with Anvil DI.

Add:

```csharp
public void Open(NwPlayer player)
```

The method must:

1. Return immediately for a DM.
2. Return immediately if `TraitSelectionPresenter` is already open for that player.
3. Create `TraitSelectionView(player)`.
4. Obtain the presenter from the view.
5. Use the injected `InjectionService`.
6. Inject the presenter.
7. Open the presenter through `WindowDirector`.

Use constructor injection for dependencies available through DI.

## Update `TraitSelectionCommand`

Change:

`Features/Chat/Commands/Player/TraitSelectionCommand.cs`

so `ExecuteCommand` delegates window opening to `TraitSelectionWindowService`.

The command must retain:

- `./traits`
- its current description
- `AllowedRoles => "Player"`

It must not contain its own Scry construction or injection logic afterward.

## Consumers

This service must be used by:

- `TraitSelectionCommand`
- `TraitOnboardingService`

Do not add other consumers in this story.

## Constraints

- Do not change `TraitSelectionView`.
- Do not change `TraitSelectionPresenter`.
- Do not change `TraitSelectionModel`.
- Do not modify trait-selection rules.
- Do not add new commands.
- Do not add new UI.
- Do not change player messages.
- Do not generalize this into a generic window-opening framework.
- Do not refactor `WindowDirector`.

## Acceptance Criteria

- All trait-selection window construction/opening logic lives in `TraitSelectionWindowService`.
- `TraitSelectionCommand` delegates to that service.
- Duplicate windows remain prevented.
- DMs remain blocked from the player trait-selection window through this path.
- `./traits` still opens the same `TraitSelectionView`.
- The service is injectable into `TraitOnboardingService`.
- Existing tests pass.
