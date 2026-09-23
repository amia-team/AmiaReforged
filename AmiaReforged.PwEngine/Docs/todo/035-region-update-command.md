# 035 — Implement region facade updates through dispatch

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Regions**
Depends on: [033 — Reconcile region facade and definition models](033-region-contract.md); [034 — Implement region facade lookup and listing](034-region-read-queries.md).

## Current gap

`UpdateRegionAsync` always fails despite available application-level region commands.

## Change

Translate the reconciled facade request into the existing update command and dispatch it. Keep validation and persistence in the handler.

## Starting points

- [Subsystems/Implementations/RegionSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs)

## Acceptance checks

- [ ] Updating a known region changes supported fields and is observable through queries.
- [ ] A missing region fails without insertion.
- [ ] The facade no longer returns `Not yet implemented` and successful execution publishes the generic event.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

