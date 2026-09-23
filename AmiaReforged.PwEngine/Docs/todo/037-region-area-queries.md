# 037 — Dispatch area membership and region-tag lookups

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Regions**
Depends on: None.

## Current gap

`IsAreaInRegion` and `GetRegionTagForArea` bypass the query dispatcher.

## Change

Add/reuse area-to-region queries and route these methods through them. Migrate synchronous consumers safely or define a compatible non-blocking boundary.

## Starting points

- [Subsystems/Implementations/RegionSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs)

## Acceptance checks

- [ ] Tests cover registered and unregistered areas and matching semantics.
- [ ] Existing callers compile and retain expected absence behavior.
- [ ] The subsystem methods no longer access repositories directly or block asynchronous NWN work.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

