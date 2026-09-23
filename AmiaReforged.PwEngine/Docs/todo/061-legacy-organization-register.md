# 061 — Close the legacy organization registration bypass

Status: **Open**
Type: **Implementation**
Audit area: **F-4 excluded residual**
Depends on: [060 — Decide the legacy OrganizationSystem disposition](060-legacy-organization-disposition.md).

## Current gap

`Register` inserts into the repository without command dispatch.

## Change

Follow task 060: remove the unused legacy registration surface, or adapt it to the existing create command while preserving duplicate/result semantics.

## Starting points

- [Subsystems/Organizations/OrganizationSystem.cs](../../Features/WorldEngine/Subsystems/Organizations/OrganizationSystem.cs)

## Acceptance checks

- [ ] No independent registration caller inserts through `OrganizationSystem`.
- [ ] If retained, duplicate and successful registration tests verify dispatch and result compatibility.
- [ ] If removed, all consumers compile and the obsolete method is absent.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

