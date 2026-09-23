# 014 — Verify successful dynamic-quest sharing

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

- [ ] A valid claimant can share with an eligible invitee and the invitee receives the quest.
- [ ] The expected share/claim events are observable and each resulting entry is applied once.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

