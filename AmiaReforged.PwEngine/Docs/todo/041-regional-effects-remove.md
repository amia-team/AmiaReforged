# 041 — Dispatch regional effect removal

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Regions**
Depends on: [039 — Add regional effect state storage](039-regional-effects-store.md).

## Current gap

`RemoveRegionalEffectAsync` always fails.

## Change

Implement a removal command/handler and dispatch from the subsystem, including any agreed runtime cleanup event.

## Starting points

- [Subsystems/Implementations/RegionSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs)

## Acceptance checks

- [ ] An applied effect is removed from the correct region.
- [ ] Missing-effect behavior matches task 038 and does not affect another region.
- [ ] The subsystem no longer contains the removal stub.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

