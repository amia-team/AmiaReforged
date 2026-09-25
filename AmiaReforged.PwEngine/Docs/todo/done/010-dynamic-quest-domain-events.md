# 010 — Publish dynamic-quest domain events on the bus

Status: **Done**  
Type: **Implementation**  
Audit area: **F-6**  
Depends on: None.

## Current gap

`DynamicQuestService` is already handler-internal and is reached through the dynamic-quest command handlers, but it sends its domain events directly to `CodexEventProcessor.EnqueueEventAsync(...)`.

That means `QuestPostedEvent`, `QuestClaimedEvent`, `QuestSharedEvent`, `QuestUnclaimedEvent`, and `QuestExpiredEvent` are not observable through the shared `IEventBus`.

Do **not** redesign the dynamic-quest lifecycle. This task only replaces the private event-delivery path with the shared bus while preserving the existing Codex aggregate application behavior.

## Implementation decision

Use this event path:

```text
CommandDispatcher
  -> existing dynamic-quest command handler
  -> DynamicQuestService
  -> persist/mutate dynamic-quest state
  -> IEventBus.PublishAsync(domain event)
       -> observers
       -> one Codex forwarding subscriber, when that event actually mutates PlayerCodex
            -> CodexEventProcessor.EnqueueEventAsync(event)
                 -> PlayerCodex mutation + save
```

### Required ownership

`DynamicQuestService` must depend on:

- `IDynamicQuestRepository`
- `QuestSessionManager`
- `IEventBus`

It must **stop depending directly on `CodexEventProcessor`**.

The existing command handlers in:

`Features/WorldEngine/Subsystems/Codex/Application/DynamicQuests/DynamicQuestCommands.cs`

remain thin wrappers over `DynamicQuestService`. Do not move the lifecycle logic into the handlers.

### Add one Codex forwarding subscriber

Add one small application-layer event handler, preferably beside the dynamic-quest application code, e.g.:

`Features/WorldEngine/Subsystems/Codex/Application/DynamicQuests/DynamicQuestCodexEventForwarder.cs`

Register it through the existing `IEventHandler<T>` / `IEventHandlerMarker` mechanism.

For task 010 it must forward these events to the existing `CodexEventProcessor`:

- `QuestClaimedEvent`
- `QuestUnclaimedEvent`
- `QuestExpiredEvent`

Each handler body should do only:

```csharp
return _processor.EnqueueEventAsync(@event, cancellationToken);
```

or the equivalent awaited form.

### Do not forward these two events into `CodexEventProcessor`

- `QuestPostedEvent`
- `QuestSharedEvent`

Reason:

- `QuestPostedEvent` describes posting lifecycle state and has no `PlayerCodex` mutation.
- `QuestSharedEvent` is observability/audit information for the claimant. The invitee receives a separate `QuestClaimedEvent`, which is the event that creates the invitee's Codex quest entry.

Do not add fake/no-op aggregate mutations for these events. In particular, do not make `QuestPostedEvent` create/save an otherwise empty `PlayerCodex`.

Task 011 may extend the same forwarder for objective-derived events that actually require existing `CodexEventProcessor` behavior.

## Publication order invariants

Preserve the current state transition order. Publish only after the corresponding state change has succeeded.

### Post

1. Validate active template.
2. Create posting.
3. `SavePostingAsync`.
4. Publish `QuestPostedEvent`.
5. Return posting.

### Claim

1. Validate posting/template/cooldown/completion limits.
2. Mutate and save the posting claim.
3. Create the shared quest session.
4. Publish `QuestClaimedEvent`.
5. Return the generated `QuestId`.

### Share

1. Validate claimant/invitee.
2. Mutate and save the posting.
3. Add invitee to the existing shared session.
4. Publish `QuestSharedEvent`.
5. Publish the invitee's `QuestClaimedEvent`.
6. Return success.

Keep that event order: **shared first, invitee claimed second**.

### Unclaim

1. Remove the participant from the posting.
2. Save the posting.
3. Remove the participant from the session.
4. Publish `QuestUnclaimedEvent`.
5. Return success.

### Expiry tick

1. `QuestSessionManager.TickDeadlines(now)` determines/removes due sessions and returns expiry events.
2. Publish each returned `QuestExpiredEvent` through `IEventBus`.
3. Remove expired postings through `IDynamicQuestRepository.RemoveExpiredPostingsAsync`.
4. Return success.

Do not add a second publication path.

## Event-bus timing invariant

The production `AnvilEventBusService` is asynchronous: `PublishAsync` enqueues the event and returns before subscribers necessarily finish.

Therefore:

- command success means the domain mutation and event publication succeeded;
- it does **not** guarantee that `CodexEventProcessor` has already persisted the resulting aggregate change by the time the command returns;
- do not add synchronous waiting to production code merely to make tests easier.

Tests may use `InMemoryEventBus` plus bounded waiting for the processor result.

## Exactly-once application invariant

For the events forwarded into `CodexEventProcessor`, there must be exactly one producer path:

```text
DynamicQuestService -> IEventBus -> DynamicQuestCodexEventForwarder -> CodexEventProcessor
```

The following is forbidden:

```text
DynamicQuestService -> CodexEventProcessor
DynamicQuestService -> IEventBus -> forwarder -> CodexEventProcessor
```

That would apply `QuestClaimedEvent` twice and can cause duplicate-quest failures.

After this task, production dynamic-quest lifecycle code must contain no direct call to:

```csharp
_eventProcessor.EnqueueEventAsync(...)
```

## Generic command-event invariant

Do not manually publish `CommandExecutedEvent<TCommand>`.

`CommandDispatcher` already publishes it once after a successful handler result. Preserve that behavior unchanged.

## Starting points

- `Features/WorldEngine/Subsystems/Codex/Application/DynamicQuests/DynamicQuestCommands.cs`
- `Features/WorldEngine/Subsystems/Codex/Application/DynamicQuestService.cs`
- `Features/WorldEngine/Subsystems/Codex/Application/CodexEventProcessor.cs`
- `Features/WorldEngine/Subsystems/Codex/Domain/Events/DynamicQuestEvents.cs`
- `Features/WorldEngine/Services/AnvilEventBusService.cs`
- `Features/WorldEngine/SharedKernel/Events/InMemoryEventBus.cs`

## Non-goals

Do **not**:

- redesign `CodexEventProcessor`;
- replace its channel;
- make the production event bus synchronous;
- introduce a new command for each domain event;
- alter claim/cooldown/completion rules;
- alter posting/session persistence;
- change event payload shapes unless compilation requires it;
- implement dynamic-quest completion tracking (`RecordCompletionAsync` remains separate/deferred);
- fix configured `ExpiryBehavior` propagation in this task.

## Acceptance checks

- [ ] `DynamicQuestService` injects `IEventBus` and no longer injects `CodexEventProcessor`.
- [ ] Successful post publishes exactly one `QuestPostedEvent`.
- [ ] Successful claim publishes exactly one `QuestClaimedEvent`.
- [ ] Successful share publishes exactly one `QuestSharedEvent` and exactly one invitee `QuestClaimedEvent`, in that order.
- [ ] Successful unclaim publishes exactly one `QuestUnclaimedEvent`.
- [ ] Expiration publishes each generated `QuestExpiredEvent` exactly once.
- [ ] `QuestClaimedEvent`, `QuestUnclaimedEvent`, and `QuestExpiredEvent` reach `CodexEventProcessor` only through the bus forwarding subscriber.
- [ ] `QuestPostedEvent` and `QuestSharedEvent` are bus-observable but are not forwarded into `CodexEventProcessor`.
- [ ] A claim creates one Codex quest entry, not two.
- [ ] Generic `CommandExecutedEvent<TCommand>` publication remains dispatcher-owned and occurs once on successful commands.
- [ ] Existing Codex tests still pass.

## Verification

Run:

```bash
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --nologo
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --filter "FullyQualifiedName~Codex" --no-build --verbosity minimal
```

Also grep the production Codex application code and confirm that dynamic-quest lifecycle methods no longer directly enqueue events into `CodexEventProcessor`.

## Completion evidence

- **Changed files:**
  - `Features/WorldEngine/Subsystems/Codex/Application/DynamicQuestService.cs` — dropped the
    `CodexEventProcessor` dependency (constructor + field) and removed the dual `EmitDomainEventAsync`
    helper entirely. All five lifecycle methods publish through `IEventBus` only, in the documented
    state-transition order; `TickExpirationsAsync` publishes each `QuestExpiredEvent` on the bus.
    (The intermediate `PublishDomainEventAsync` pass-through wrapper was later deleted per code review
    — call sites now invoke `_eventBus.PublishAsync(...)` directly, so there is no extra layer.)
  - `Features/WorldEngine/Subsystems/Codex/Application/DynamicQuests/DynamicQuestCodexEventForwarder.cs`
    (new) — single Codex forwarding subscriber registered via `IEventHandlerMarker`.
  - `Features/WorldEngine/SharedKernel/Tests/Codex/Application/CodexPlayerStateBehavior.cs` — dropped
    the removed `CodexEventProcessor` argument from the `QuestService` test helper.
  - `Features/WorldEngine/Subsystems/Codex/Application/DynamicQuests/DynamicQuestCommands.cs` —
    unchanged (thin wrappers, as required).

- **Final event path:**
  `DynamicQuestService -> IEventBus -> DynamicQuestCodexEventForwarder -> CodexEventProcessor`.
  Production dynamic-quest lifecycle code contains no direct `_eventProcessor.EnqueueEventAsync(...)`.

- **Forwarding subscriber handles:** `QuestClaimedEvent`, `QuestUnclaimedEvent`, `QuestExpiredEvent`.
  **Not forwarded** (bus-observable only): `QuestPostedEvent`, `QuestSharedEvent`.

- **Test command/result:**
  ```
  dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --nologo   -> 0 Error(s)
  dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
    --filter "FullyQualifiedName~Codex" --no-build --verbosity minimal       -> Passed! Failed: 0, Passed: 520
  dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
    --filter "FullyQualifiedName~WorldEngine" --no-build --verbosity minimal -m:1 -> Passed! Failed: 0, Passed: 1856
  ```

- **Exactly-once confirmation:** grep of `DynamicQuestService.cs` for `EnqueueEventAsync` / `_eventProcessor`
  returns none. `QuestClaimedEvent` (and the other forwarded events) reach `CodexEventProcessor` only through the
  bus forwarding subscriber; they are never both directly enqueued and bus-forwarded.

See [backlog scope and completion rules](README.md).
