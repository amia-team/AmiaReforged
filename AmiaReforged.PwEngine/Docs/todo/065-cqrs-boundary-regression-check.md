# 065 — Guard the migrated CQRS boundaries

Status: **Open**
Type: **Verification**
Audit area: **Cross-cutting verification**
Depends on: None.

## Current gap

Selected happy-path tests do not detect new direct repository/handler consumers or prove all audited endpoints dispatch.

## Change

Add a focused architecture regression check for the migrated controllers/wrappers, with an explicit narrow exception list for remaining decision-gated work. Use semantic inspection or a deterministic source check rather than fragile counts of matching files.

## Starting points

- [API/Tests/ControllerCqrsTests.cs](../../Features/WorldEngine/API/Tests/ControllerCqrsTests.cs)
- [SharedKernel/Tests/Commands/CommandDispatcherBehavior.cs](../../Features/WorldEngine/SharedKernel/Tests/Commands/CommandDispatcherBehavior.cs)

## Acceptance checks

- [ ] An intentionally introduced direct repository or concrete-handler dependency makes the check fail.
- [ ] All migrated data endpoints are mapped to facade/dispatcher coverage; infra-only endpoints are explicitly identified.
- [ ] Exceptions name the linked open todo and cannot silently make F-7 appear complete.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

