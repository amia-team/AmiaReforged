# 011 — Publish objective-resolution events on the bus

Status: **Open**
Type: **Implementation**
Audit area: **F-6**
Depends on: [010 — Publish dynamic-quest domain events on the bus](010-dynamic-quest-domain-events.md).

## Current gap

Objective resolution directly enqueues its resulting domain events in the private processor.

## Change

Route resolved objective/stage/quest events through the bus using the single aggregate-application path established in task 010. Keep objective evaluation handler-internal; a new signal command is not required merely to rename the existing flow.

## Starting points

- [Subsystems/Codex/Application/QuestObjectiveResolutionService.cs](../../Features/WorldEngine/Subsystems/Codex/Application/QuestObjectiveResolutionService.cs)
- [Subsystems/Codex/Application/CodexEventProcessor.cs](../../Features/WorldEngine/Subsystems/Codex/Application/CodexEventProcessor.cs)
- [Subsystems/Codex/Application/DialogueNodeEnteredEventHandler.cs](../../Features/WorldEngine/Subsystems/Codex/Application/DialogueNodeEnteredEventHandler.cs)

## Acceptance checks

- [ ] A dialogue signal that completes an objective produces observable bus events.
- [ ] The aggregate transition and any rewards occur once, without a publish/consume loop.
- [ ] No objective-result event is delivered only by a private enqueue.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

