# 006 — Move dialogue NPC synchronization behind events

Status: **Split**
Type: **Umbrella**
Audit area: **F-1**
Depends on: See child tasks.

## Disposition

This task is intentionally split because the original card combined architecture, NWN integration, ownership semantics, controller cleanup, and verification.

Do not implement this umbrella directly.

Complete the following child tasks instead:

- [006A — Add a dialogue NPC synchronization boundary](006A-dialogue-npc-synchronizer-boundary.md)
- [006B — Synchronize dialogue NPCs from successful command events](006B-dialogue-npc-event-subscriber.md)
- [006C — Remove NPC synchronization from DialogueController](006C-dialogue-controller-cleanup.md)
- [006D — Decide shared SpeakerTag ownership semantics](006D-dialogue-speaker-tag-ownership-contract.md)
- [006E — Verify dialogue NPC synchronization end-to-end](006E-dialogue-npc-sync-verification.md)

## Important implementation direction

Reuse the existing successful-command event flow:

```text
CommandDispatcher
  -> CommandExecutedEvent<CreateDialogueTreeCommand>
  -> CommandExecutedEvent<UpdateDialogueTreeCommand>
  -> CommandExecutedEvent<DeleteDialogueTreeCommand>
```

Do **not** add a redundant second event family unless a concrete missing requirement is discovered.

Task 005 already established this pattern for dialogue store-cache invalidation.

## Completion condition

Mark this umbrella **Done** only when 006A–006E are complete and their evidence sections show passing verification.

## Completion evidence

- Child task statuses:
  - 006A:
  - 006B:
  - 006C:
  - 006D:
  - 006E:
- Final verification command:
- Final observed result:
- Any follow-up work intentionally deferred:
