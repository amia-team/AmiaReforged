# 028 — Dispatch organization reputation adjustments

Status: **Not applicable**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: [026 — Implement the agreed organization reputation store](026-character-reputation-storage.md); [027 — Dispatch organization reputation reads](027-character-reputation-query.md); [025 — Define organization reputation storage and semantics](025-character-reputation-contract.md).

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

**Not applicable — decided by [025](025-character-reputation-contract.md).** The `AdjustReputationAsync` method was removed from `ICharacterSubsystem`/`CharacterSubsystem`; there is no adjustment to dispatch. Real reputation mutation lives in the Codex `AdjustReputationCommand`, which is untouched. See [026](026-character-reputation-storage.md) and [027](027-character-reputation-query.md).

See [backlog scope and completion rules](README.md).

