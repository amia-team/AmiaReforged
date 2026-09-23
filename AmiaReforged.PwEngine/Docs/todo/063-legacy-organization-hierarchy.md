# 063 — Close legacy organization hierarchy read bypasses

Status: **Open**
Type: **Implementation**
Audit area: **F-4 excluded residual**
Depends on: [060 — Decide the legacy OrganizationSystem disposition](060-legacy-organization-disposition.md).

## Current gap

`ParentFor` and `SubordinateOrganizationsFor` query the repository directly.

## Change

Follow task 060: remove unused methods or route them through hierarchy queries. Specify whether subordinate means one chain or all descendants before preserving/replacing the existing traversal.

## Starting points

- [Subsystems/Organizations/OrganizationSystem.cs](../../Features/WorldEngine/Subsystems/Organizations/OrganizationSystem.cs)

## Acceptance checks

- [ ] Retained hierarchy queries cover missing parents and multiple children according to the selected contract.
- [ ] Reads do not mutate persistence; cycle behavior is explicit if traversal is retained.
- [ ] Legacy hierarchy methods are removed or use dispatch.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

