# 022 — Resolve unsupported character statistics fields

Status: **Open**
Type: **Decision and contract**
Audit area: **F-7 Characters**
Depends on: None.

## Current gap

Statistics report rank-ups as quests, industries joined as crafted items, and the current time as last seen; only play time is writable.

## Change

Document and implement the smallest truthful statistics contract: map supported fields correctly and remove/explicitly represent unsupported fields, or identify approved backing data. Update affected consumers before dispatch migration.

## Starting points

- [Subsystems/Implementations/CharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs)
- [Subsystems/ICharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/ICharacterSubsystem.cs)

## Acceptance checks

- [ ] Every returned field has a documented real source or explicit unavailable representation.
- [ ] Reads no longer label rank-ups as quests or industries joined as crafted items.
- [ ] The read/write contract and consumer compatibility are captured in focused tests.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

