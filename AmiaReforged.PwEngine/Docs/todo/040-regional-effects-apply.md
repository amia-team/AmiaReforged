# 040 — Dispatch regional effect application

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Regions**
Depends on: [039 — Add regional effect state storage](039-regional-effects-store.md).

## Current gap

`ApplyRegionalEffectAsync` always fails.

## Change

Implement an apply command/handler using the agreed store and definition validation. Route the subsystem through dispatch; use bus subscribers for agreed runtime side effects.

## Starting points

- [Subsystems/Implementations/RegionSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs)

## Acceptance checks

- [ ] A valid application persists and emits the agreed event.
- [ ] Unknown region/effect and duplicate application match task 038's contract.
- [ ] The subsystem no longer contains the apply stub.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

