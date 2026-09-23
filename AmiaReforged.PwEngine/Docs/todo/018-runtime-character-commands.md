# 018 — Dispatch runtime character cache mutations

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Characters**
Depends on: None.

## Current gap

Login, key reacquisition, and logout directly add/delete runtime repository entries.

## Change

Move runtime character add/remove operations behind command handlers. Leave NWN event capture, object construction, and thread switching in the appropriate runtime adapter.

## Starting points

- [Subsystems/Characters/Runtime/RuntimeCharacterService.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacterService.cs)

## Acceptance checks

- [ ] Login and key reacquisition dispatch registration without duplicate cached characters.
- [ ] Logout dispatches removal and preserves CharacterLeaving/CharacterReady ordering.
- [ ] The service no longer mutates `ICharacterRepository` directly; handler tests run without live NWN objects.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

