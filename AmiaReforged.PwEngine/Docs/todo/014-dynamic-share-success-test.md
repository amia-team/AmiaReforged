# 014 — Verify successful dynamic-quest sharing

Status: **Open**  
Type: **Verification**  
Audit area: **F-6 verification**  
Depends on: [010 — Publish dynamic-quest domain events on the bus](010-dynamic-quest-domain-events.md).

## Current gap

There is no command-level success test proving that sharing updates the posting/session, publishes both expected domain facts, and creates exactly one Codex entry for the invitee.

## Existing contract to preserve

`DynamicQuestService.ShareQuestAsync` currently does all of the following on success:

1. verifies the posting exists;
2. verifies the claimant owns an active claim;
3. rejects an invitee already participating;
4. adds the invitee to the posting's claimant slot;
5. saves the posting;
6. adds the invitee to the claimant's existing shared `QuestSession`;
7. publishes `QuestSharedEvent` for the claimant;
8. publishes a separate `QuestClaimedEvent` for the invitee.

The invitee `QuestClaimedEvent` is what creates the invitee's Codex entry.

Do not replace those two events with one combined event.

## Test decision

Use the same real-dispatcher/in-memory bus harness established by tasks 012–013.

The task-010 forwarder must forward the invitee's `QuestClaimedEvent` to `CodexEventProcessor`.

`QuestSharedEvent` remains bus-observable only and must not create a second claimant Codex entry.

## Arrange

1. Create/save a minimal valid template.
2. Dispatch `PostDynamicQuestCommand`.
3. Dispatch `ClaimDynamicQuestCommand` for `claimantId`.
4. Capture the resulting `postingId` and `questId`.
5. Wait until the claimant has exactly one Codex quest entry from the initial claim.
6. Create a distinct `inviteeId`.
7. Clear bus event history before the share phase so event counts are local to this action.

The claimant and invitee must be different characters.

## Act

Dispatch:

```csharp
new ShareDynamicQuestCommand
{
    ClaimantId = claimantId,
    InviteeId = inviteeId,
    PostingId = postingId,
    QuestId = questId
}
```

through the real `CommandDispatcher`.

## Required assertions

Assert all of the following:

1. Share command succeeds.
2. The posting reports the invitee as a participant.
3. `QuestSessionManager.HasSession(inviteeId, questId) == true`.
4. The claimant and invitee resolve to the same shared quest session instance if the test can inspect it directly.
5. Exactly one `QuestSharedEvent` is published:
   - `CharacterId == claimantId`
   - `InviteeId == inviteeId`
   - matching `questId`
6. Exactly one invitee `QuestClaimedEvent` is published:
   - `CharacterId == inviteeId`
   - matching `postingId`
   - matching `questId`
7. Event order is:
   - `QuestSharedEvent`
   - invitee `QuestClaimedEvent`
8. Exactly one successful `CommandExecutedEvent<ShareDynamicQuestCommand>` is published.
9. After bounded waiting, invitee Codex contains exactly one quest with `questId`.
10. Claimant Codex still contains exactly one quest with `questId`; sharing did not duplicate it.

## Invariants

- Sharing does not create a new `QuestId`; the invitee joins the claimant's existing quest/session.
- `QuestSharedEvent` does not mutate `PlayerCodex`.
- The invitee's `QuestClaimedEvent` is the only Codex-start event for the invitee.
- Do not make the invitee consume an additional claim slot unless the existing `DynamicQuestPosting` domain behavior already does so.

## Non-goals

Do not test:

- party-system eligibility beyond what `ShareQuestAsync` currently validates;
- invite acceptance UI;
- offline invitees;
- share rejection variants;
- cross-server/session persistence.

## Acceptance checks

- [ ] A valid claimant can share with a distinct eligible invitee.
- [ ] Posting and shared-session membership are updated.
- [ ] One share event and one invitee claim event are observable in the existing order.
- [ ] The invitee receives exactly one Codex quest entry.
- [ ] The claimant's Codex entry is not duplicated.

## Verification

Run the focused test first, then:

```bash
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --filter "FullyQualifiedName~Codex" --no-build --verbosity minimal
```

## Completion evidence

Record the test name and exact test command/result.

See [backlog scope and completion rules](README.md).
