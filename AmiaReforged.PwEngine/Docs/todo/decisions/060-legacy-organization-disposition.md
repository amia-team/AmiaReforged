# 060 — Decide the legacy OrganizationSystem disposition

Status: **Open**
Type: **Decision**
Audit area: **F-4 explicitly excluded residual**
Depends on: None.

## Current gap

The legacy API remains repository-direct and includes an empty ban method; it was explicitly excluded from the original F-4 fix.

## Change

Inventory production consumers and choose removal of unused APIs or migration of still-used registration, inbox, and hierarchy operations. Identify the ban method as unsupported rather than silently treating it as implemented.

## Starting points

- [Subsystems/Organizations/OrganizationSystem.cs](../../Features/WorldEngine/Subsystems/Organizations/OrganizationSystem.cs)

## Acceptance checks

- [ ] Record every production caller, or evidence that none exist.
- [ ] Choose removal/migration separately for registration, inbox, hierarchy reads, and the empty ban API.
- [ ] Tasks 061–063 are unblocked or marked not applicable; any retained ban feature gets its own concrete follow-up.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

