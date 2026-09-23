# 042 — Dispatch regional effect listing

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Regions**
Depends on: [039 — Add regional effect state storage](039-regional-effects-store.md).

## Current gap

`GetRegionalEffectsAsync` always returns an empty list.

## Change

Add a query over the agreed store and map effect metadata to the agreed public contract. Keep expiry mutation out of the read query.

## Starting points

- [Subsystems/Implementations/RegionSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs)

## Acceptance checks

- [ ] Applied effects are returned for their region; empty regions return an empty list.
- [ ] Expired-effect visibility follows task 038.
- [ ] The query performs no writes and the facade no longer returns an unconditional empty list.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

