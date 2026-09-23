# 045 — Dispatch industry rank advancement

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

Runtime rank-up calls the membership service directly.

## Change

Add a rank-up command/handler and migrate independent callers while retaining existing requirements and result semantics.

## Starting points

- [Subsystems/Industries/IndustryMembershipService.cs](../../Features/WorldEngine/Subsystems/Industries/IndustryMembershipService.cs)
- [Subsystems/Characters/Runtime/RuntimeCharacter.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacter.cs)

## Acceptance checks

- [ ] An eligible member advances one rank and receives the existing event once.
- [ ] Ineligible/missing membership fails without a mutation.
- [ ] Runtime rank-up traverses command dispatch.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

