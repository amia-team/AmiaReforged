# 017 — Dispatch persistent character registration

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: None.

## Current gap

Area-entry registration performs persistence directly, while the subsystem registration method always fails.

## Change

Add a registration command/handler covering create and missing-persona backfill. Make the area-enter adapter supply required identity/name data and dispatch. Reconcile the ID-only subsystem signature explicitly; do not invent unavailable registration data.

## Starting points

- [Subsystems/Characters/CharacterRegistrationService.cs](../../Features/WorldEngine/Subsystems/Characters/CharacterRegistrationService.cs)
- [Subsystems/Implementations/CharacterSubsystem.cs](../../Features/WorldEngine/Subsystems/Implementations/CharacterSubsystem.cs)

## Acceptance checks

- [ ] Tests cover new registration, existing registration, and persona backfill without duplication.
- [ ] The NWN adapter retains missing-key/DM checks but performs no repository writes.
- [ ] The public subsystem registration contract is either usable via dispatch or explicitly retired with callers updated.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

