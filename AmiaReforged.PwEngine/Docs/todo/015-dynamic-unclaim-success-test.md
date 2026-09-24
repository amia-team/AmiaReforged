# 015 — Verify successful dynamic-quest unclaiming

Status: **Open**  
Type: **Verification**  
Audit area: **F-6 verification**  
Depends on: [010 — Publish dynamic-quest domain events on the bus](010-dynamic-quest-domain-events.md).

## Current gap

There is no real-dispatcher success test proving the existing unclaim contract across posting state, runtime session state, bus events, and Codex state.

## Existing contract to preserve

A successful `DynamicQuestService.UnclaimQuestAsync` currently:

1. loads the posting;
2. removes the character either as the primary claimant or as a shared participant via `DynamicQuestPosting.Unclaim`;
3. saves the posting;
4. removes that character from the shared quest session;
5. publishes `QuestUnclaimedEvent`.

`CodexEventProcessor` handles `QuestUnclaimedEvent` with:

```csharp
codex.RemoveQuest(questId, occurredAt);
```

Therefore the current contract is **remove the dynamic quest entry from that character's Codex**, not mark it `Abandoned`.

Do not change that semantic in this verification task.

## Test decision

Use the same real-dispatcher/in-memory event-bus harness established by tasks 012–014.

## Arrange

1. Create/save a valid active template.
2. Dispatch post.
3. Dispatch claim for `characterId`.
4. Capture `postingId` and generated `questId`.
5. Wait until the character's Codex contains exactly one quest with `questId`.
6. Confirm the session exists.
7. Clear bus event history before the unclaim phase.

## Act

Dispatch:

```csharp
new UnclaimDynamicQuestCommand
{
    CharacterId = characterId,
    PostingId = postingId,
    QuestId = questId
}
```

through the real `CommandDispatcher`.

## Required assertions

Assert all of the following:

1. Command succeeds.
2. The posting no longer reports the character as a participant/claimant.
3. `QuestSessionManager.HasSession(characterId, questId) == false`.
4. Exactly one matching `QuestUnclaimedEvent` is published.
5. Exactly one successful `CommandExecutedEvent<UnclaimDynamicQuestCommand>` is published.
6. After bounded waiting for `CodexEventProcessor`, the character's Codex no longer contains `questId`.
7. No second/duplicate `QuestUnclaimedEvent` is produced by the forwarding path.

## Shared-session invariant

If the implementation under test uses a shared session, removing one member must only unregister/remove that member.

Do not destroy the quest session for remaining party members.

A separate shared-member test is optional only if it can be added without broadening this card; the required test may use a single claimant.

## Non-goals

Do not:

- change unclaim into `QuestAbandonedEvent`;
- change posting slot semantics;
- redesign `QuestSessionManager.RemoveFromSession`;
- add history/audit persistence;
- test every rejection branch.

## Acceptance checks

- [ ] A valid unclaim releases the posting participation.
- [ ] The character is removed from the runtime session.
- [ ] The existing contract removes that quest from the character's Codex.
- [ ] Exactly one unclaim domain event is bus-observable.
- [ ] Exactly one generic command event is bus-observable.

## Verification

Run the focused test first, then:

```bash
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --filter "FullyQualifiedName~Codex" --no-build --verbosity minimal
```

## Completion evidence

Record the test name and exact test command/result.

See [backlog scope and completion rules](README.md).
