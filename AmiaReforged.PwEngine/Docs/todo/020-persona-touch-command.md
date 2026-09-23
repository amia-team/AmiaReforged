# 020 — Dispatch player-persona activity updates

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: None.

## Current gap

`TouchPlayerPersona` writes logout activity directly.

## Change

Add an activity-touch command and dispatch it from the logout adapter. Preserve current timestamp semantics and graceful failure handling.

## Starting points

- [Subsystems/Characters/Runtime/RuntimeCharacterService.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacterService.cs)

## Acceptance checks

- [ ] A known persona receives the supplied activity timestamp.
- [ ] Missing/invalid identity and repository failure have explicit tested results.
- [ ] Logout no longer calls the persistent repository's `Touch` directly.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

