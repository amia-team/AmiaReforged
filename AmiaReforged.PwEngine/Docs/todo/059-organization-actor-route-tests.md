# 059 — Verify actor forwarding at the HTTP boundary

Status: **Open**
Type: **Verification**
Audit area: **F-4 verification**
Depends on: [058 — Document and select trusted organization actor semantics](058-organization-actor-contract.md).

## Current gap

Current controller tests verify self-removal, but not explicit `actedBy` forwarding or malformed actor behavior.

## Change

Add route-level tests using the contract selected in task 058, alongside existing subsystem/handler authorization tests.

## Starting points

- [API/Tests/ControllerCqrsTests.cs](../../Features/WorldEngine/API/Tests/ControllerCqrsTests.cs)
- [API/Controllers/OrganizationController.cs](../../Features/WorldEngine/API/Controllers/OrganizationController.cs)

## Acceptance checks

- [ ] A valid explicit actor reaches `RemoveMemberCommand.RemovedBy` unchanged.
- [ ] Absent and malformed actor inputs produce the selected documented outcomes.
- [ ] Handler authorization rejection maps to the intended HTTP response.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

