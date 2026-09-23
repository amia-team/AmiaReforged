# 030 — Dispatch trait removal

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Traits**
Depends on: None.

## Current gap

`RemoveTraitAsync` locates and deletes character traits directly.

## Change

Add a revoke/remove command/handler and route the subsystem method through dispatch.

## Starting points

- [Subsystems/Implementations/TraitSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/TraitSubsystem.cs)

## Acceptance checks

- [ ] Removing an owned trait persists removal.
- [ ] Removing an absent trait preserves the current failure contract.
- [ ] Success publishes the generic event and no removal repository logic remains in the subsystem.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

