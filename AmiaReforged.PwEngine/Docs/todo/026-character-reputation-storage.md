# 026 — Implement the agreed organization reputation store

Status: **Not applicable**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: [025 — Define organization reputation storage and semantics](025-character-reputation-contract.md).

## Current gap

No working authoritative read/write store backs character organization reputation.

## Change

Implement only the repository behavior selected in task 025, with migrations if required. If the feature is retired there, document why this task is not applicable.

## Starting points

- [Subsystems/Characters/IReputationRepository.cs](../../Features/WorldEngine/Subsystems/Characters/IReputationRepository.cs)

## Acceptance checks

- [ ] Repository contract tests round-trip adjustments and isolate character/organization pairs.
- [ ] Missing-record and repeated-adjustment semantics match the decision.
- [ ] Retention is verified at the chosen store lifetime boundary.

## Completion evidence

**Not applicable — decided by [025](025-character-reputation-contract.md).** Task 025 retired the organization-reputation feature (keyed by `OrganizationId` GUID) in favor of the Codex faction-reputation store (keyed by `FactionId` string); the two identities do not match, so no store was implemented. The stub `IReputationRepository`/`ReputationRepository` were deleted and no schema migration was required.

See [backlog scope and completion rules](README.md).

