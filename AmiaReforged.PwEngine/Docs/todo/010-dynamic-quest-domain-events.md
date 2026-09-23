# 010 — Publish dynamic-quest domain events on the bus

Status: **Open**
Type: **Implementation**
Audit area: **F-6**
Depends on: None.

## Current gap

Dynamic-quest handlers dispatch commands, but their domain events only enter the private Codex channel.

## Change

Publish post/claim/share/unclaim/expiry domain events through `IEventBus` and arrange exactly one application to the Codex aggregate. Preserve the channel if useful internally; do not both directly enqueue and subscribe the same event.

## Starting points

- [Subsystems/Codex/Application/DynamicQuests/DynamicQuestCommands.cs](../../Features/WorldEngine/Subsystems/Codex/Application/DynamicQuests/DynamicQuestCommands.cs)
- [Subsystems/Codex/Application/DynamicQuestService.cs](../../Features/WorldEngine/Subsystems/Codex/Application/DynamicQuestService.cs)
- [Subsystems/Codex/Application/CodexEventProcessor.cs](../../Features/WorldEngine/Subsystems/Codex/Application/CodexEventProcessor.cs)

## Acceptance checks

- [ ] Bus subscribers observe the expected domain events on successful operations.
- [ ] Each event changes the Codex at most once and publication timing relative to persistence is explicit.
- [ ] Generic `CommandExecutedEvent` publication remains intact; existing Codex tests pass.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

