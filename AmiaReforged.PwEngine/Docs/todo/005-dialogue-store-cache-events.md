# 005 — Remove the dialogue controller's concrete handler lookup

Status: **Open**
Type: **Implementation**
Audit area: **F-1 / F-2**
Depends on: None.

## Current gap

`TryInvalidateStoreCache` resolves `ExecuteDialogueActionHandler` directly.

## Change

Have the store-cache owner react to successful dialogue-definition changes through the event bus, then remove the controller lookup. Keep cache ownership narrow.

## Starting points

- [API/Controllers/DialogueController.cs](../../Features/WorldEngine/API/Controllers/DialogueController.cs)
- [Subsystems/Dialogue/Application/Commands/ExecuteDialogueActionHandler.cs](../../Features/WorldEngine/Subsystems/Dialogue/Application/Commands/ExecuteDialogueActionHandler.cs)

## Acceptance checks

- [ ] Successful relevant changes invalidate the store cache via the event subscriber.
- [ ] Rejected changes do not trigger invalidation.
- [ ] No controller resolves `ExecuteDialogueActionHandler`; document/test asynchronous cache freshness.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

