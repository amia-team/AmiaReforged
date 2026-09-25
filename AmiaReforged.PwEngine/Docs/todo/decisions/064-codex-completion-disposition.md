# 064 — Record the deferred dynamic-quest completion policy

Status: **Open**
Type: **Decision**
Audit area: **F-6 intentional deferral**
Depends on: None.

## Current gap

`RecordCompletionAsync` has no production callers and no command; this was explicitly deferred, not accidentally omitted.

## Change

Confirm whether completion tracking is intentionally dormant or should be connected to quest completion for cooldown/count enforcement. Do not add an unused command just to match the original proposal.

## Starting points

- [Subsystems/Codex/Application/DynamicQuestService.cs](../../Features/WorldEngine/Subsystems/Codex/Application/DynamicQuestService.cs)

## Acceptance checks

- [ ] Record current callers and the behavior of completion-count/cooldown checks.
- [ ] If dormant, document the explicit limitation and reason.
- [ ] If activation is selected, add separate implementation and success-test todos with the required event/command boundary before closing this decision.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

