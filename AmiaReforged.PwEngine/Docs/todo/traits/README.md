# Traits Story 001 Packet

This packet contains the redefined Story 001 and its six bounded implementation subtasks.

## Files

- `001-story-trait-onboarding.md`
- `001-A-onboarding-eligibility.md`
- `001-B-prompt-preference.md`
- `001-C-onboarding-prompt-nui.md`
- `001-D-starting-area-orchestrator.md`
- `001-E-codex-select-traits-action.md`
- `001-F-wire-yes-to-selection-window.md`

## Intended execution order

1. 001-A
2. 001-B
3. 001-C
4. Story 002 from the larger Traits packet: shared trait-selection window opener
5. 001-D
6. 001-E
7. 001-F

Story 001 must not use `NwModule.Instance.OnClientEnter`. The onboarding trigger is the starting-area `OnEnter` event.
