# 015 — Verify successful dynamic-quest unclaiming

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

- [ ] A valid unclaim releases the posting claim and updates session/Codex state according to the existing contract.
- [ ] The unclaim domain event and generic command-executed event are observable.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

