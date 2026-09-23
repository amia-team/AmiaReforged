# 028 — Dispatch organization reputation adjustments

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: [026 — Implement the agreed organization reputation store](026-character-reputation-storage.md); [027 — Dispatch organization reputation reads](027-character-reputation-query.md).

## Current gap

`AdjustReputationAsync` always returns an unsupported failure.

## Change

Add an adjustment command/handler carrying character, organization, delta, and reason. Persist through the agreed store and dispatch from the subsystem.

## Starting points

- [Subsystems/Implementations/CharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs)

## Acceptance checks

- [ ] Positive and negative adjustments round-trip through task 027's query.
- [ ] Rejected adjustments do not mutate state; reason and identities reach the handler.
- [ ] Success publishes the generic command-executed event and any agreed domain event.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

