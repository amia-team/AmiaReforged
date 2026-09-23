# 046 — Make progression-point awards dispatchable

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

Progression-point awards have no independent command boundary.

## Change

Add an award command/handler around the existing service and migrate independent external callers. Calls already inside `CraftItemHandler` may remain internal; do not force nested dispatch solely for uniform naming.

## Starting points

- [Subsystems/Industries/KnowledgeSubsystem/KnowledgeProgressionService.cs](../../Features/WorldEngine/Subsystems/Industries/KnowledgeSubsystem/KnowledgeProgressionService.cs)
- [Application/Industries/Commands/CraftItemCommand.cs](../../Features/WorldEngine/Application/Industries/Commands/CraftItemCommand.cs)

## Acceptance checks

- [ ] Tests cover threshold rollover, soft-cap behavior, and hard-cap rejection.
- [ ] A directly dispatched award produces the generic command-executed event.
- [ ] Crafting still awards the same amount exactly once; list and migrate any independent callers.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

