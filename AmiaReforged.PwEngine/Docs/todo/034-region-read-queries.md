# 034 — Implement region facade lookup and listing

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Regions**
Depends on: [033 — Reconcile region facade and definition models](033-region-contract.md).

## Current gap

`GetRegionAsync` and `GetAllRegionsAsync` always return null/empty.

## Change

Route both methods through existing region queries and map the result using task 033's contract.

## Starting points

- [Subsystems/Implementations/RegionSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/RegionSubsystem.cs)

## Acceptance checks

- [ ] A registered region is returned by tag and included in the list.
- [ ] Unknown tag and empty repository return the agreed absence values.
- [ ] The facade no longer returns unconditional stubs.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

