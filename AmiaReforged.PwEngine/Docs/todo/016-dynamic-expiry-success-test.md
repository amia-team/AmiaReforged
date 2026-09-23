# 016 — Verify expiration of an active dynamic quest

Status: **Open**
Type: **Verification**
Audit area: **F-6 verification**
Depends on: [010 — Publish dynamic-quest domain events on the bus](010-dynamic-quest-domain-events.md).

## Current gap

The new command-level tests cover missing inputs or empty expiration, not this successful flow.

## Change

Add a focused real-dispatcher test with in-memory repositories and controlled event processing/time. Assert final state rather than only mocked calls.

## Starting points

- [SharedKernel/Tests/Codex/Application/CodexPlayerStateBehavior.cs](../../Features/WorldEngine/SharedKernel/Tests/Codex/Application/CodexPlayerStateBehavior.cs)

## Acceptance checks

- [ ] An expired posting/session is processed according to its configured expiry behavior.
- [ ] Expected expiry events are observable; repeating the tick does not duplicate terminal transitions.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

