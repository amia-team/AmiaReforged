# 006 — Move dialogue NPC synchronization behind events

Status: **Open**
Type: **Implementation**
Audit area: **F-1**
Depends on: None.

## Current gap

The controller directly registers, unregisters, and updates NPC dialogue bindings.

## Change

Publish definition-change events with the identifiers needed by an NPC synchronization subscriber. Move the controller helpers into that subscriber and retain NWN main-thread requirements.

## Starting points

- [API/Controllers/DialogueController.cs](../../Features/WorldEngine/API/Controllers/DialogueController.cs)
- [Subsystems/Dialogue/Application/Commands/DialogueTreeCommands.cs](../../Features/WorldEngine/Subsystems/Dialogue/Application/Commands/DialogueTreeCommands.cs)

## Acceptance checks

- [ ] Create binds matching NPCs; changing a speaker tag removes old bindings and installs new ones.
- [ ] Delete removes only bindings owned by the deleted tree.
- [ ] Handler/subscriber tests cover success and rejection; the controller no longer calls NPC hooks.

## Completion evidence

Record the chosen behavior (if a decision), changed files, exact verification command or manual procedure, and observed result here. If deferred or not applicable, link the deciding task and explain why.

See [backlog scope and completion rules](README.md).

