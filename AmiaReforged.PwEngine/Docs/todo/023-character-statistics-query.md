# 023 — Dispatch character statistics reads

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: [022 — Resolve unsupported character statistics fields](022-character-statistics-contract.md).

## Current gap

`GetCharacterStatsAsync` reads statistics directly.

## Change

Add a statistics query/handler using the contract from task 022 and make the subsystem a dispatch wrapper.

## Starting points

- [Subsystems/Implementations/CharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs)

## Acceptance checks

- [ ] Tests cover a known character and a missing statistics record.
- [ ] The query performs no writes and returns the agreed truthful projection.
- [ ] The subsystem forwards cancellation and no longer reads the statistics repository directly.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

