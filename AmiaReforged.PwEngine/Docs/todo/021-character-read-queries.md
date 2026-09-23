# 021 — Dispatch character and context lookups

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: None.

## Current gap

Character/context lookups read the repository outside query dispatch.

## Change

Add/reuse a character lookup query and route subsystem reads and repository-backed runtime lookup through it. For synchronous context APIs, migrate callers to async or document a safe compatibility boundary; avoid blocking NWN-thread continuations.

## Starting points

- [Subsystems/Implementations/CharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs)
- [Subsystems/Characters/Runtime/RuntimeCharacterService.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacterService.cs)

## Acceptance checks

- [ ] Existing and missing character cases preserve their documented results/exceptions.
- [ ] Knowledge and industry context lookups use the same query path.
- [ ] Migrated public lookup methods have no direct repository access.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

