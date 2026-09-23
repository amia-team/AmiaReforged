# 024 — Dispatch character statistics updates

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: [022 — Resolve unsupported character statistics fields](022-character-statistics-contract.md); [023 — Dispatch character statistics reads](023-character-statistics-query.md).

## Current gap

`UpdateCharacterStatsAsync` mutates and saves directly.

## Change

Add an update-statistics command/handler using task 022's supported write fields and dispatch it from the subsystem.

## Starting points

- [Subsystems/Implementations/CharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs)

## Acceptance checks

- [ ] Supported values round-trip through command and query.
- [ ] Missing records and unsupported updates have explicit tested results.
- [ ] Successful writes receive the generic command-executed event; the subsystem no longer saves directly.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

