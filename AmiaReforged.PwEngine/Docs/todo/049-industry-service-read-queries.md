# 049 — Dispatch independent membership and knowledge reads

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

Membership and knowledge read methods remain independently callable outside queries.

## Change

Inventory independent read callers, reuse existing membership/knowledge queries, and add only missing projections. Keep repository-backed reads used internally by handlers internal.

## Starting points

- [Subsystems/Industries/IndustryMembershipService.cs](../../Features/WorldEngine/Subsystems/Industries/IndustryMembershipService.cs)
- [Subsystems/Characters/Runtime/RuntimeCharacter.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacter.cs)

## Acceptance checks

- [ ] Each independent membership/knowledge read maps to a named query.
- [ ] Tests preserve missing membership, knowledge filtering, and learning-eligibility results.
- [ ] Record migrated callers and any intentionally handler-internal methods.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

