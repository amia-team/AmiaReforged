# 062 — Close the legacy organization request bypass

Status: **Open**
Type: **Implementation**
Audit area: **F-4 excluded residual**
Depends on: [060 — Decide the legacy OrganizationSystem disposition](060-legacy-organization-disposition.md).

## Current gap

`SendRequest` loads an organization and mutates its inbox directly.

## Change

Follow task 060: remove the unused surface, or add a send-request command/handler and adapt legacy callers. Define and test whether inbox changes are persisted by the existing domain/store.

## Starting points

- [Subsystems/Organizations/OrganizationSystem.cs](../../Features/WorldEngine/Subsystems/Organizations/OrganizationSystem.cs)

## Acceptance checks

- [ ] A retained valid request reaches the intended inbox through dispatch.
- [ ] Missing-organization behavior is preserved and persistence is verified if required.
- [ ] The legacy method is removed or contains no direct domain/repository mutation.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

