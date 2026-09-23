# 056 — Implement history and last-harvest queries

Status: **Open**
Type: **Implementation**
Audit area: **F-5 documented limitation**
Depends on: [054 — Add the agreed harvest history store](054-harvest-history-store.md).

## Current gap

`GetHarvestHistoryAsync` and `GetLastHarvestTimeAsync` return unconditional empty/null values.

## Change

Add the two read queries over the history store and dispatch from the subsystem.

## Starting points

- [Subsystems/Implementations/HarvestingSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/HarvestingSubsystem.cs)

## Acceptance checks

- [ ] History obeys the requested limit and agreed ordering.
- [ ] Last-harvest lookup isolates the character/node pair and returns null when absent.
- [ ] Both reads are side-effect-free and return recorded events through the public wrapper.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

