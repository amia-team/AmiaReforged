# 043 — Route runtime industry enrollment through the existing command

Status: **Open**
Type: **Implementation**
Audit area: **F-7 Industries**
Depends on: None.

## Current gap

`RuntimeCharacter` still invokes direct membership insertion, despite the new enrollment command.

## Change

Migrate independent runtime enrollment entry points to `EnrollInIndustryCommand`. Compare existing service initialization behavior before replacing it; preserve required defaults and events without double insertion.

## Starting points

- [Subsystems/Characters/Runtime/RuntimeCharacter.cs](../../Features/WorldEngine/Subsystems/Characters/Runtime/RuntimeCharacter.cs)
- [Subsystems/Industries/IndustryMembershipService.cs](../../Features/WorldEngine/Subsystems/Industries/IndustryMembershipService.cs)
- [Application/Industries/Commands/EnrollInIndustryCommand.cs](../../Features/WorldEngine/Application/Industries/Commands/EnrollInIndustryCommand.cs)

## Acceptance checks

- [ ] Runtime enrollment traverses the dispatcher and creates one membership.
- [ ] Duplicate/unknown character/unknown industry behavior is explicit and tested.
- [ ] Independent callers no longer use direct `AddMembership` to bypass dispatch.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

