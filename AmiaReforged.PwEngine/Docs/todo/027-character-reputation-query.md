# 027 — Dispatch organization reputation reads

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: [026 — Implement the agreed organization reputation store](026-character-reputation-storage.md).

## Current gap

`GetReputationAsync` uses the placeholder repository directly.

## Change

Add a read query over the agreed store and route the subsystem method through the query dispatcher.

## Starting points

- [Subsystems/Implementations/CharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs)

## Acceptance checks

- [ ] Known and missing reputation cases match task 025's contract.
- [ ] Reading does not create or update persistence.
- [ ] The subsystem has no direct reputation read.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

