# 013 — Verify successful dynamic-quest claiming

Status: **Open**  
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

Record the test name and exact test command/result.

See [backlog scope and completion rules](README.md).
