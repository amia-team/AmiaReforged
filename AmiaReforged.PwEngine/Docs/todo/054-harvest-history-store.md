# 054 — Add the agreed harvest history store

Status: **Open**
Type: **Implementation**
Audit area: **F-5 documented limitation**
Depends on: [053 — Define harvest history semantics and retention](053-harvest-history-contract.md).

## Current gap

No history repository backs the public harvest-history methods.

## Change

Implement the repository and any schema selected in task 053, including the selected duplicate-event policy.

## Starting points

- [Subsystems/Implementations/HarvestingSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/HarvestingSubsystem.cs)

## Acceptance checks

- [ ] Repository tests verify character/node isolation and timestamp ordering.
- [ ] Duplicate identities and retention follow the contract.
- [ ] The selected storage lifetime is verified independently of the event recorder.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

