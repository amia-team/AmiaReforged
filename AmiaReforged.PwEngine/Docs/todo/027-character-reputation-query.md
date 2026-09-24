# 027 — Dispatch organization reputation reads

Status: **Not applicable**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: [026 — Implement the agreed organization reputation store](026-character-reputation-storage.md); [025 — Define organization reputation storage and semantics](025-character-reputation-contract.md).

## Current gap

`GetReputationAsync` uses the placeholder repository directly.

## Change

Add a read query over the agreed store and route the subsystem method through the query dispatcher.

## Starting points

- [Subsystems/Implementations/CharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs)

## Acceptance checks

- [ ] Known and missing reputation cases match task 025's contract.
- [ ] Reading does not create or update persistence.
- [ ] The subsystem has no direct reputation read.

## Completion evidence

**Not applicable — decided by [025](025-character-reputation-contract.md).** The `GetReputationAsync(CharacterId, OrganizationId)` method was removed entirely from `ICharacterSubsystem`/`CharacterSubsystem`; there is no subsystem reputation read to route through a query dispatcher. See [026](026-character-reputation-storage.md).

See [backlog scope and completion rules](README.md).

