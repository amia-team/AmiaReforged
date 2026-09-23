# 047 — Dispatch level-up knowledge-point grants

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

`GrantLevelUpKnowledgePoint` mutates progression without a command envelope.

## Change

Add a command/handler for one level-up knowledge-point grant and route independent level-up callers through it. Preserve the deliberate bypass of the economy curve.

## Starting points

- [Subsystems/Industries/KnowledgeSubsystem/KnowledgeProgressionService.cs](../../Features/WorldEngine/Subsystems/Industries/KnowledgeSubsystem/KnowledgeProgressionService.cs)

## Acceptance checks

- [ ] One successful invocation grants exactly the existing level-up amount.
- [ ] Economy progression counters/curve behavior are not accidentally applied to this grant.
- [ ] Independent callers dispatch; successful execution publishes the generic event.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

