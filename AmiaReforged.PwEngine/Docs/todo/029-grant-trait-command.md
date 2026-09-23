# 029 — Dispatch trait grants

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Traits**
Depends on: None.

## Current gap

`GrantTraitAsync` performs definition checks and repository insertion inline.

## Change

Add a grant command/handler preserving current grant semantics and dispatch it from the subsystem. Reuse existing trait domain behavior where compatible; granting is not automatically equivalent to selection.

## Starting points

- [Subsystems/Implementations/TraitSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/TraitSubsystem.cs)

## Acceptance checks

- [ ] Valid grants preserve active/confirmed/unlocked state.
- [ ] Unknown definitions and duplicate grants fail without an extra row.
- [ ] Successful dispatch publishes the generic event; the wrapper has no grant-time repository access.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

