# 036 — Dispatch regional chaos resolution

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Regions**
Depends on: None.

## Current gap

`GetChaosForAreaAsync` implements repository lookup and precedence inline.

## Change

Move the existing resolution into a query handler and dispatch from the subsystem.

## Starting points

- [Subsystems/Implementations/RegionSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs)

## Acceptance checks

- [ ] Tests cover area override, region default, and unregistered-area fallback.
- [ ] Case-insensitive area matching remains intact.
- [ ] The subsystem no longer directly reads the region repository for chaos.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

