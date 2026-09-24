# 039 — Add regional effect state storage

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Regions**
Depends on: [038 — Define the regional effects feature contract](038-regional-effects-contract.md).

## Current gap

Regional effects have no backing state store.

## Change

Implement the store selected in task 038 without adding controller or subsystem business logic.

## Starting points

- [Subsystems/IRegionSubsystem.cs](../../Features/WorldEngine/Subsystems/IRegionSubsystem.cs)

## Acceptance checks

- [ ] Contract tests round-trip an effect and isolate regions.
- [ ] Duplicate/removal/expiry representation matches the decision.
- [ ] Storage lifetime and any migration are verified.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

