# 011 — Publish objective-resolution events on the bus

Status: **Done**  
Type: **Implementation**  
Audit area: **F-6**  
Depends on: [010 — Publish dynamic-quest domain events on the bus](010-dynamic-quest-domain-events.md).

## Current gap

`QuestObjectiveResolutionService` routes NWN/dialogue signals through `QuestSessionManager`, receives `CodexDomainEvent` results, and currently fire-and-forgets each result directly into `CodexEventProcessor`.

That bypasses the shared `IEventBus`.

Task 010 establishes the only allowed Codex aggregate application path:

```text
domain producer
  -> IEventBus
  -> selected Codex forwarding subscriber
  -> CodexEventProcessor
```

Apply the same rule here.

## Implementation decision

### Replace the direct processor dependency

`QuestObjectiveResolutionService` must inject `IEventBus` instead of `CodexEventProcessor`.

Keep these existing dependencies/roles:

- `RuntimeCharacterService` — player/NWN identity and lifecycle
- `QuestSessionManager` — objective evaluation and in-memory runtime session state
- `IPlayerCodexRepository` — login/session reconstruction only
- `IEventBus` — publication of resulting domain facts

Do not make the service call command handlers.

### Make signal publication awaitable

Replace the private direct-enqueue helper with an async bus-publishing helper, e.g.:

```csharp
private async Task RouteSignalAndPublishEventsAsync(
    CharacterId characterId,
    QuestSignal signal,
    CancellationToken ct = default)
{
    IReadOnlyList<CodexDomainEvent> events =
        _sessionManager.ProcessSignal(characterId, signal);

    foreach (CodexDomainEvent domainEvent in events)
        await _eventBus.PublishAsync(domainEvent, ct);
}
```

Use equivalent code if naming differs, but keep the behavior.

Convert the production signal entry methods to awaitable methods:

- `ProcessItemAcquiredAsync`
- `ProcessItemLostAsync`
- `ProcessDialogueNodeEnteredAsync`

There are no production callers outside this service and `DialogueNodeEnteredEventHandler` that require preserving the current `void` signatures.

Update the NWN event callbacks to await the new methods. `async void` is acceptable only at the actual NWN event-handler boundary because those delegates are event callbacks.

Update `DialogueNodeEnteredEventHandler.HandleAsync` to await/return the task from `ProcessDialogueNodeEnteredAsync`.

Do not add a new "objective signal command". Objective evaluation remains handler-internal/runtime plumbing exactly as it is now.

## Publish every event returned by `QuestSessionManager`

For a signal, preserve the event list and order returned by `QuestSessionManager.ProcessSignal`.

Potential events include:

- `ObjectiveProgressedEvent`
- `ObjectiveCompletedEvent`
- `ObjectiveFailedEvent`
- `QuestObjectiveGroupCompletedEvent`
- `QuestStageAdvancedEvent`
- `StageRewardsGrantedEvent`
- `QuestExpiredEvent`

Publish each event once, in returned order.

Do not translate them into new wrapper events.

## Codex forwarding decision

Extend the single forwarding subscriber established in task 010 only for events that have real behavior in `CodexEventProcessor`.

Forward:

- `QuestStageAdvancedEvent`
- `StageRewardsGrantedEvent`
- `QuestExpiredEvent` (already covered by task 010)

Do **not** forward these observability-only objective events into `CodexEventProcessor`:

- `ObjectiveProgressedEvent`
- `ObjectiveCompletedEvent`
- `ObjectiveFailedEvent`
- `QuestObjectiveGroupCompletedEvent`

`CodexEventProcessor` intentionally treats those four as no-ops because objective state is held by `QuestSession`. Publishing them on the shared bus is sufficient and avoids pointless aggregate load/save work.

Do not invent persisted per-objective progress in this task.

## Behavioral invariants

### Objective state

`QuestSession` remains the owner of live objective state.

Do not move evaluator state into `PlayerCodex`.

### Stage advancement

When `QuestSession` emits `QuestStageAdvancedEvent`, the existing `CodexEventProcessor` behavior remains the owner of applying that stage transition to `PlayerCodex`.

Do not separately call `SetQuestStageCommand` from the objective-resolution path. Doing both would double-advance the quest.

### Stage rewards

When `QuestSession` emits `StageRewardsGrantedEvent`, preserve the existing `CodexEventProcessor` path to `IStageRewardGranter`.

Do not grant rewards directly in `QuestObjectiveResolutionService` in addition to forwarding the event.

### Dialogue signals

`DialogueNodeEnteredEventHandler` remains a bus subscriber to `DialogueNodeEnteredEvent`.

It translates the dialogue event into a quest signal through `QuestObjectiveResolutionService`; the service then publishes resulting Codex domain events back to the shared bus.

This is not a publish loop because `DialogueNodeEnteredEvent` and the resulting objective/Codex events are different event types.

### Runtime bus timing

The production event bus is asynchronous. Do not assume a published stage-advance event has already updated persistence when `PublishAsync` returns.

## Starting points

- `Features/WorldEngine/Subsystems/Codex/Application/QuestObjectiveResolutionService.cs`
- `Features/WorldEngine/Subsystems/Codex/Application/CodexEventProcessor.cs`
- `Features/WorldEngine/Subsystems/Codex/Application/DialogueNodeEnteredEventHandler.cs`
- `Features/WorldEngine/Subsystems/Codex/Domain/Aggregates/QuestSession.cs`
- `Features/WorldEngine/Subsystems/Codex/Domain/Events/ObjectiveEvents.cs`
- `Features/WorldEngine/SharedKernel/Tests/Codex/Application/QuestObjectiveResolutionServiceTests.cs`
- `Features/WorldEngine/SharedKernel/Tests/Codex/Application/QuestObjectiveTestHelpers.cs`

## Non-goals

Do **not**:

- add an objective-signal command;
- redesign evaluator interfaces;
- persist objective counters;
- change stage-selection rules;
- move stage reward logic into the resolution service;
- make the event bus synchronous;
- directly call `CodexEventProcessor` from `QuestObjectiveResolutionService`;
- publish a second event copy from the forwarding subscriber.

## Acceptance checks

- [ ] `QuestObjectiveResolutionService` no longer depends on `CodexEventProcessor`.
- [ ] Every event returned by `QuestSessionManager.ProcessSignal` is published through `IEventBus` exactly once.
- [ ] A dialogue signal completing an objective produces bus-observable objective/group/stage events as applicable.
- [ ] `QuestStageAdvancedEvent` reaches `CodexEventProcessor` through the task-010 forwarding path and updates the Codex once.
- [ ] `StageRewardsGrantedEvent` reaches the existing reward-granter path once.
- [ ] Objective progress/completion/failure/group events are bus-observable but are not pointlessly re-applied to `PlayerCodex`.
- [ ] There is no direct `CodexEventProcessor.EnqueueEventAsync` call in production objective-resolution code.
- [ ] There is no publish/consume loop.
- [ ] Existing objective and Codex tests pass.

## Verification

Run:

```bash
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --nologo
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --filter "FullyQualifiedName~Codex" --no-build --verbosity minimal
```

Also grep `QuestObjectiveResolutionService.cs` for `CodexEventProcessor` / `EnqueueEventAsync`; neither should remain in the production signal-delivery path.

## Completion evidence

Record:

- changed files;
- final signal → bus → Codex path;
- exact event types forwarded to `CodexEventProcessor`;
- exact test command/result;
- confirmation that no objective-result event is delivered only through the old private channel.

## Completion evidence

- **Changed files:**
  - `Features/WorldEngine/Subsystems/Codex/Application/QuestObjectiveResolutionService.cs`
    — replaced `CodexEventProcessor` field with `IEventBus`; private
    `RouteSignalAndEnqueueEvents` → async `RouteSignalAndPublishEventsAsync`;
    `ProcessItemAcquired`/`ProcessItemLost`/`ProcessDialogueNodeEntered` →
    `ProcessItemAcquiredAsync`/`ProcessItemLostAsync`/`ProcessDialogueNodeEnteredAsync`;
    NWN callbacks `OnAcquireItem`/`OnUnacquireItem` now `async void` and await the new methods;
    every resulting event is published once, in returned order, via a single
    `await _eventBus.PublishAsync(domainEvent, ct);` in the loop over
    `QuestSessionManager.ProcessSignal`'s result (no per-type switch — the bus routes by
    runtime type, so concrete-type dispatch is implicit).
  - `Features/WorldEngine/Subsystems/Codex/Application/DialogueNodeEnteredEventHandler.cs`
    — `HandleAsync` now `async` and awaits `ProcessDialogueNodeEnteredAsync`.
  - `Features/WorldEngine/Subsystems/Codex/Application/DynamicQuests/DynamicQuestCodexEventForwarder.cs`
    — extended the single forwarding subscriber with
    `IEventHandler<QuestStageAdvancedEvent>` and
    `IEventHandler<StageRewardsGrantedEvent>`.

- **Final signal → bus → Codex path:**
  NWN/dialogue callback → `QuestObjectiveResolutionService.ProcessItem*Async` /
  `ProcessDialogueNodeEnteredAsync` → `QuestSessionManager.ProcessSignal` →
  `_eventBus.PublishAsync(domainEvent, ct)` →
  `DynamicQuestCodexEventForwarder` → `CodexEventProcessor`.
  Production objective-resolution code contains no direct `CodexEventProcessor` /
  `EnqueueEventAsync` reference.

- **Exact event types forwarded to `CodexEventProcessor`:** `QuestStageAdvancedEvent`,
  `StageRewardsGrantedEvent`, `QuestExpiredEvent`. **Published on the bus but not
  forwarded** (observability only, `CodexEventProcessor` no-op): `ObjectiveProgressedEvent`,
  `ObjectiveCompletedEvent`, `ObjectiveFailedEvent`, `QuestObjectiveGroupCompletedEvent`.
  Objective events are published with their concrete type so the bus is observable, but the
  forwarding subscriber does not subscribe to them, so they are never re-applied to
  `PlayerCodex`.

- **Test command/result:**
  ```
  dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --nologo   -> 0 Error(s)
  dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
    --filter "FullyQualifiedName~Codex" --no-build --verbosity minimal       -> Passed! Failed: 0, Passed: 520
  ```

- **Exactly-once confirmation:** grep of `QuestObjectiveResolutionService.cs` for
  `CodexEventProcessor` / `EnqueueEventAsync` returns none. Every event returned by
  `QuestSessionManager.ProcessSignal` is published through `IEventBus` exactly once via a
  single `await _eventBus.PublishAsync(domainEvent, ct);` in the loop; objective-result
  events are delivered only through the bus (the old private enqueue channel no longer
  exists). The per-type switch the task spec's example hinted at was not needed — the bus
  routes by runtime type, so there is no wrapper or dispatch layer.

See [backlog scope and completion rules](README.md).
