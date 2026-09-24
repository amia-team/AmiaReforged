# 016 — Verify expiration of an active dynamic quest

Status: **Open**  
Type: **Verification**  
Audit area: **F-6 verification**  
Depends on: [010 — Publish dynamic-quest domain events on the bus](010-dynamic-quest-domain-events.md).

## Current gap

The existing command test proves only that an empty expiration tick returns success.

There is no real-dispatcher test proving that an actually expired active session emits one expiry event, updates Codex state, removes the runtime session, and stays idempotent on a repeated tick.

## Important scope decision: test the behavior that exists

The current runtime expiry path does **not** propagate the template/posting's configured `ExpiryBehavior` into `QuestSession`.

`QuestSessionManager.TickDeadlines(...)` currently emits:

```csharp
new QuestExpiredEvent(..., ExpiryBehavior.Fail)
```

for expired sessions.

Therefore this task verifies the existing **default `Fail` expiry path only**.

Do not use this verification card as authorization to redesign expiry behavior propagation for `Remove` or `Cooldown`.

If configured per-template expiry behavior is required later, create a separate implementation task that explicitly carries `ExpiryBehavior` into the runtime session and Codex entry.

## Test decision

Make the test deterministic without sleeping for a real time limit.

Do **not** create a one-millisecond quest and wait for wall-clock time.

Instead arrange an already-expired runtime session directly with `QuestSessionManager.CreateSharedSession(...)` using a deadline in the past.

The command under test must still be the real:

```csharp
ExpireDynamicQuestsCommand
```

through the real `CommandDispatcher`.

## Test wiring

Use:

- `InMemoryDynamicQuestRepository`
- `InMemoryPlayerCodexRepository`
- `QuestSessionManager`
- real `CodexEventProcessor`
- task-010 event forwarder
- `InMemoryEventBus`
- real `DynamicQuestService`
- real `ExpireDynamicQuestsHandler`
- real `CommandDispatcher`

Subscribe the task-010 forwarder for `QuestExpiredEvent`.

## Arrange

1. Create a `CharacterId` and `QuestId`.
2. Seed the character's `PlayerCodex` with one in-progress `CodexQuestEntry` using that `QuestId`.
3. Save the Codex to `InMemoryPlayerCodexRepository`.
4. Create a runtime shared session for that same character/quest with:
   - any valid/empty objective list suitable for construction;
   - `deadline` strictly in the past;
   - `createdAt` before the deadline.
5. Confirm `QuestSessionManager.HasSession(characterId, questId) == true`.
6. Use an otherwise empty `InMemoryDynamicQuestRepository`; posting cleanup is not the focus of this test.
7. Clear the bus event history immediately before the first expiration command.

This bypasses claim setup intentionally so the test controls the deadline without changing production clock APIs.

## Act — first tick

Dispatch `ExpireDynamicQuestsCommand` through `CommandDispatcher`.

## First-tick assertions

Assert:

1. Command succeeds.
2. The expired runtime session is removed.
3. Exactly one matching `QuestExpiredEvent` is bus-observable.
4. That event uses `ExpiryBehavior.Fail`.
5. Exactly one successful `CommandExecutedEvent<ExpireDynamicQuestsCommand>` is published for this dispatch.
6. After bounded waiting for `CodexEventProcessor`, the quest entry is in the state produced by `PlayerCodex.RecordQuestExpired(..., ExpiryBehavior.Fail, ...)` — currently `QuestState.Failed`.

Do not assert `Expired` state for `ExpiryBehavior.Fail`; `PlayerCodex` maps `Fail` to `Failed`.

## Act — second tick

Clear only the bus event-history collection if necessary for easy counting, then dispatch another `ExpireDynamicQuestsCommand`.

## Second-tick assertions

Assert:

1. Second command also succeeds.
2. No new `QuestExpiredEvent` is emitted because the session was removed during the first tick.
3. A generic successful `CommandExecutedEvent<ExpireDynamicQuestsCommand>` is still expected for the second command itself.
4. Codex remains in the same terminal state and is not transitioned/applied again.

The idempotence requirement applies to the **domain expiry transition**, not to the generic command-executed event emitted for each command invocation.

## Invariants

- `QuestSessionManager.TickDeadlines` owns detection and runtime session removal.
- `DynamicQuestService.TickExpirationsAsync` publishes the returned domain events through `IEventBus`.
- The task-010 forwarder is the only path from `QuestExpiredEvent` to `CodexEventProcessor`.
- `CodexEventProcessor` owns the existing `PlayerCodex.RecordQuestExpired` mutation.
- Repeating a tick after the session is gone must not emit another domain expiry event.

## Non-goals

Do not:

- add an injectable clock just for this test;
- add `Thread.Sleep` or long `Task.Delay`;
- redesign expiry behavior propagation;
- implement `ExpiryBehavior.Remove` or `ExpiryBehavior.Cooldown` runtime wiring;
- test posting-duration cleanup in this card;
- alter completion/cooldown tracking.

## Acceptance checks

- [ ] A genuinely expired runtime session is exercised through the real expiry command.
- [ ] Exactly one `QuestExpiredEvent` is published on the first tick.
- [ ] The current default `Fail` behavior produces a failed Codex quest.
- [ ] The runtime session is removed.
- [ ] Repeating the tick emits no second domain expiry event.
- [ ] Generic command-executed events remain one-per-successful-dispatch.

## Verification

Run the focused test first, then:

```bash
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --filter "FullyQualifiedName~Codex" --no-build --verbosity minimal
```

## Completion evidence

Record:

- the test name;
- how the past deadline was arranged;
- first- and second-tick event counts;
- exact test command/result.

See [backlog scope and completion rules](README.md).
