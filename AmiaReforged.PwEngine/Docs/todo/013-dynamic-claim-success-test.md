# 013 — Verify successful dynamic-quest claiming

Status: **Done**  
Type: **Verification**  
Audit area: **F-6 verification**  
Depends on: [010 — Publish dynamic-quest domain events on the bus](010-dynamic-quest-domain-events.md).

## Current gap

Existing command-level coverage verifies rejection for a missing posting but does not prove the successful claim path, session creation, bus publication, or single Codex application.

## Test decision

Add one real-dispatcher success test using the task-010 bus path.

Use:

- `InMemoryDynamicQuestRepository`
- `InMemoryPlayerCodexRepository`
- `ObjectiveEvaluatorRegistry`
- `QuestSessionManager`
- real `CodexEventProcessor`
- the task-010 Codex forwarding subscriber
- `InMemoryEventBus`
- real dynamic-quest handlers
- real `CommandDispatcher`

Do not mock the service or repositories.

## Test wiring

Use one `InMemoryEventBus` instance for:

- `DynamicQuestService`
- `CommandDispatcher`

Subscribe the task-010 forwarder to `QuestClaimedEvent` in the test bus so the same production event path is exercised:

```text
service -> InMemoryEventBus -> forwarder -> CodexEventProcessor
```

Because `CodexEventProcessor` applies from its own channel, use a small bounded wait/poll helper for the final Codex state.

Do not put an arbitrary multi-second `Task.Delay` in the test.

## Arrange

1. Create/save a minimal valid active `DynamicQuestTemplate`.
2. Dispatch `PostDynamicQuestCommand` to create a real posting.
3. Obtain the created posting ID.
4. Clear the bus's published-event history before the claim assertion phase if doing so makes counts unambiguous.
5. Use a fresh claimant `CharacterId`.

Use a template with:

- no posting expiry;
- no claimant time limit;
- no cooldown;
- unlimited completions;
- enough claim capacity.

This keeps the test focused on the success path.

## Act

Dispatch `ClaimDynamicQuestCommand` through the real `CommandDispatcher`.

## Required assertions

Assert all of the following:

1. Claim command succeeds.
2. The result contains a non-empty generated `questId`.
3. The posting now reports `HasClaim(characterId) == true`.
4. `QuestSessionManager.HasSession(characterId, questId) == true`.
5. Exactly one matching `QuestClaimedEvent` was published on the bus.
6. Exactly one successful `CommandExecutedEvent<ClaimDynamicQuestCommand>` was published.
7. After bounded waiting for event processing, the claimant's `PlayerCodex` exists.
8. That Codex contains exactly one quest with the generated `QuestId`.
9. The Codex entry has:
   - the template/posting title and description;
   - the source template ID;
   - `QuestState.InProgress`.
10. The Codex does **not** contain a duplicate entry for the same `QuestId`.

## Exactly-once regression target

This test must fail if a future implementation both:

- directly enqueues `QuestClaimedEvent` into `CodexEventProcessor`; and
- publishes it to the bus where the forwarder enqueues it again.

That duplicate path is specifically what task 010 is preventing.

## Non-goals

Do not add assertions for:

- cooldown rejection;
- max-completion rejection;
- full/exclusive posting rejection;
- quest completion tracking;
- configured expiry behavior.

## Acceptance checks

- [ ] A valid claim persists the claim and creates the runtime quest session.
- [ ] The claim domain event is bus-observable exactly once.
- [ ] The Codex quest entry is applied exactly once.
- [ ] The generic command event is observable once.
- [ ] The test uses the real dispatcher and in-memory repositories.

## Verification

Run the focused test first, then:

```bash
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --filter "FullyQualifiedName~Codex" --no-build --verbosity minimal
```

## Completion evidence

- **Added test:**
  `AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Application.CodexPlayerStateBehavior.DynamicQuest_Claim_SuccessfulClaim_PersistsSessionEventAndCodexThroughDispatcher`
  in `Features/WorldEngine/SharedKernel/Tests/Codex/Application/CodexPlayerStateBehavior.cs`.

- **Wiring:** one shared `InMemoryEventBus` in both `DynamicQuestService` and `CommandDispatcher`;
  the real `DynamicQuestCodexEventForwarder` (task 010) subscribed to `QuestClaimedEvent` on that
  bus so the production path `service -> InMemoryEventBus -> forwarder -> CodexEventProcessor ->
  PlayerCodex` is exercised. The claim path only emits `QuestClaimedEvent`, so that is the sole
  forwarded event subscribed — the forwarder stays the only path into `CodexEventProcessor`. `CodexEventProcessor` applies from its own channel, so the final Codex
  state is confirmed with a bounded poll helper (`WaitUntilCodexQuestAsync`, 10 ms ticks up to 5 s,
  no fixed multi-second `Task.Delay`). Real `InMemoryDynamicQuestRepository`, `InMemoryPlayerCodexRepository`,
  `ObjectiveEvaluatorRegistry`, `QuestSessionManager`, and the real `CommandDispatcher` are used; nothing
  is mocked.

- **Focused run** (built, then executed):

  ```bash
  dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
    --filter "FullyQualifiedName~DynamicQuest_Claim_SuccessfulClaim_PersistsSessionEventAndCodexThroughDispatcher" \
    --verbosity minimal
  ```

  Result: `Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1`.

- **Full filter run:**

  ```bash
  dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
    --filter "FullyQualifiedName~Codex" --no-build --verbosity minimal
  ```

  Result: `Passed!  - Failed: 0, Passed: 522, Skipped: 0, Total: 522`.

- **Exactly-once confirmation:** the test clears the bus after posting, then asserts exactly one
  `QuestClaimedEvent`, exactly one successful `CommandExecutedEvent<ClaimDynamicQuestCommand>`, and a
  Codex entry that contains exactly one quest for the generated `QuestId` in `InProgress` state with the
  template/posting title, description, and source template ID. Because `RecordQuestStarted` throws when a
  quest already exists, a future implementation that both enqueued `QuestClaimedEvent` directly into
  `CodexEventProcessor` **and** published it to the bus (double forward) would fail this test.

See [backlog scope and completion rules](README.md).
