# 044 — Dispatch knowledge learning

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

Knowledge learning persists through direct service calls; `LearnRecipeCommand` only validates recipe availability.

## Change

Add a knowledge-learning command wrapping the existing domain/service behavior and migrate independent runtime callers. Keep recipe validation separate and preserve prerequisite checks.

## Starting points

- [Subsystems/Industries/IndustryMembershipService.cs](../../Features/WorldEngine/Subsystems/Industries/IndustryMembershipService.cs)
- [Subsystems/Characters/Runtime/RuntimeCharacter.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacter.cs)

## Acceptance checks

- [ ] Eligible knowledge learning persists and publishes the existing domain event once.
- [ ] Missing prerequisites and already-known knowledge do not create another record.
- [ ] Independent learning entry points dispatch; returned learning outcomes remain usable by their callers.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

