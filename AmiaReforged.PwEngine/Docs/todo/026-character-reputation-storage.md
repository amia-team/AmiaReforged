# 026 — Implement the agreed organization reputation store

Status: **Open**
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

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

