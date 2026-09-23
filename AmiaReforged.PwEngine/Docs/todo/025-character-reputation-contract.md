# 025 — Define organization reputation storage and semantics

Status: **Open**
Type: **Decision**
Audit area: **F-7 Characters**
Depends on: None.

## Current gap

The repository always returns a default reputation and exposes no mutation; character reputation adjustment is unsupported.

## Change

Choose the authoritative organization-reputation store and missing-record/adjustment semantics. Distinguish this API from Codex faction reputation; only share storage if identities and semantics actually match.

## Starting points

- [Subsystems/Characters/IReputationRepository.cs](../../Features/WorldEngine/Subsystems/Characters/IReputationRepository.cs)
- [Subsystems/Implementations/CharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs)

## Acceptance checks

- [ ] Record the target identity mapping, retention, default, and allowed adjustment behavior.
- [ ] Identify the concrete repository changes and any schema migration required.
- [ ] Resolve whether this public feature is implemented or explicitly retired; do not silently retain a fake success/default.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

