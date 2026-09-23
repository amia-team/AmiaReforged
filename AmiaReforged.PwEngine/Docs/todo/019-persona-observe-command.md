# 019 — Dispatch player-persona observation

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: None.

## Current gap

`ObservePlayerPersona` directly calls the persistent persona repository.

## Change

Wrap observation/upsert in a command carrying captured identity, display name, and observation time. Route login/re-cache adapters through dispatch.

## Starting points

- [Subsystems/Characters/Runtime/RuntimeCharacterService.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacterService.cs)

## Acceptance checks

- [ ] First observation and repeated observation preserve existing upsert behavior.
- [ ] Invalid/empty identity retains its current rejection behavior.
- [ ] The adapter no longer calls `Upsert`; a successful command publishes the generic event.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

