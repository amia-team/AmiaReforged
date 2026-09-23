# 031 — Dispatch trait definition and ownership reads

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Traits**
Depends on: None.

## Current gap

Definition lookup/list, character trait list, and ownership checks all read repositories directly.

## Change

Reuse existing trait queries where their result contracts match; add missing query projections for `GetTraitAsync`, `GetAllTraitsAsync`, `GetCharacterTraitsAsync`, and `HasTraitAsync`.

## Starting points

- [Subsystems/Implementations/TraitSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/TraitSubsystem.cs)

## Acceptance checks

- [ ] Tests cover missing definitions, empty ownership, and inactive traits in `HasTraitAsync`.
- [ ] Existing public projections remain compatible.
- [ ] These four subsystem methods only dispatch/map results; they do not read repositories.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

