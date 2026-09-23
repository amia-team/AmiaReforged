# 050 — Separate progression reads from initialization

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

Progression read methods use `GetOrCreate`, so blindly wrapping them as queries would preserve hidden writes.

## Change

Provide side-effect-free progression/cap/cost queries for independent callers. Move required initialization into the existing registration/award write path or a narrow initialization command.

## Starting points

- [Subsystems/Industries/KnowledgeSubsystem/KnowledgeProgressionService.cs](../../Features/WorldEngine/Subsystems/Industries/KnowledgeSubsystem/KnowledgeProgressionService.cs)
- [Subsystems/Industries/KnowledgeSubsystem/IKnowledgeProgressionRepository.cs](../../Features/WorldEngine/Subsystems/Industries/KnowledgeSubsystem/IKnowledgeProgressionRepository.cs)

## Acceptance checks

- [ ] Querying a missing character's progression does not insert a row.
- [ ] Existing progression, cap, and next-point cost results are preserved.
- [ ] Independent callers dispatch queries; initialization is explicit and tested.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

