# 006B — Synchronize dialogue NPCs from successful command events

Status: **Done**

## Completion

Implemented and tested. See below.

- **`DialogueNpcSynchronizationHandler.cs`** — implements `IEventHandler<CommandExecutedEvent<CreateDialogueTreeCommand>>`, `Update`, `Delete`, and `IEventHandlerMarker`; injects `IDialogueNpcSynchronizer?` (`[Inject]`, `internal` for testability); each `HandleAsync` has a defensive `if (!@event.Result.Success) return;` check. Create skips registration on null/empty/whitespace tag; Update forwards the immutable tree ID and the new tag (null forwarded, not discarded); Delete forwards only the tree ID. Logs at Info (Debug for the skip case). Registered via `[ServiceBinding(typeof(DialogueNpcSynchronizationHandler))]` only.
- **`DialogueNpcSynchronizationHandlerTests.cs`** (`Features/WorldEngine/Subsystems/Dialogue/Tests/`) — 8 tests: create+tag registers once, create without tag does not register, update forwards tree ID/new tag, update with null tag forwards null, delete unregisters once, defensive ignore of failed-result events, a dispatcher-level rejection test (real `CommandDispatcher` + `InMemoryEventBus` + `EventRecorder`-style `bus.PublishedEvents`), and an interface check. Uses a lightweight `RecordingSynchronizer` fake (no real `NwCreature`).

### Rejection proof
The test `RejectedCommand_ThroughDispatcher_PublishesNoEvent_AndSubscriberNeverInvoked` dispatches a `CreateDialogueTreeCommand` through a real `CommandDispatcher` seeded with a stub handler returning `CommandResult.Fail(...)`. It asserts `result.Success == false`, `bus.PublishedEvents` has count 0, and the `RecordingSynchronizer` recorded no register call — proving a failed command never publishes the success event and therefore never reaches the subscriber.

### Observed result
`Passed! - Failed: 0, Passed: 8, Skipped: 0, Total: 8`.

### Deferred observations
- `Synchronizer` is `internal` (with `[Inject]`) so tests can set it directly; this is safe because the PwEngine assembly is also the test assembly.
- **Binding fix applied during completion:** the handler originally carried a second `[ServiceBinding(typeof(IDialogueNpcSynchronizer))]` even though it does not implement that interface. Anvil rejects duplicate service bindings for the same type at startup, so the erroneous binding was removed. The sole `IDialogueNpcSynchronizer` binding now lives on `DialogueNpcHook` (the true implementation, per task 006A). This left `DialogueNpcHook` unchanged, as 006B non-goals require.

### Post-review fixes (thermo-nuclear reviewer, APPROVE-WITH-FOLLOW-UPS)
- **Misleading Update log (concrete defect, fixed):** the Update log bound the new tag to both the `{OldTag}` and `{NewTag}` placeholders, so a real `npc_old → npc_new` change logged as `(tag 'npc_new' → 'npc_new')`. The subscriber does not own the old tag (the synchronizer's registry does), so the `{OldTag}` placeholder was removed; the log now shows only the accurate new tag plus the correct unregistered/registered counts.
- **False consistency claim (fixed):** the class summary stated the defensive `Result.Success` guard was "consistent with the store-cache handler." That handler (`RecipeTemplateCacheInvalidationHandler`) has no such guard. The accurate sibling is `ExecuteDialogueActionHandler` (same subsystem, same `CommandExecutedEvent` pattern, does use the guard). The summary now references `ExecuteDialogueActionHandler`. The guard itself was kept: it is required by the `FailedResultEvent_IsDefensivelyIgnored_ForAllEventTypes` test and matches the subsystem's own sibling (the dispatcher cannot publish a non-success event, so the guarded path is unreachable in practice).

Type: **Implementation**
Audit area: **F-1**
Depends on: **006A**

## Goal

Move dialogue-definition NPC synchronization behind the existing WorldEngine event path.

After this task, successful dialogue-tree create/update/delete commands should enqueue event-driven NPC synchronization without the command handlers or event subscribers depending directly on the API controller.

## Existing architecture to reuse

The write path already does this:

```text
DialogueController
  -> IWorldEngineFacade.ExecuteAsync(...)
  -> CommandDispatcher
  -> dialogue CRUD ICommandHandler<T>
  -> successful CommandResult
  -> CommandExecutedEvent<TCommand>
```

`CommandDispatcher` publishes `CommandExecutedEvent<TCommand>` only when:

```csharp
result.Success == true
```

Production handling is performed asynchronously by:

```text
Features/WorldEngine/Services/AnvilEventBusService.cs
```

Task 005 already uses this pattern for dialogue store-cache invalidation in:

```text
Subsystems/Dialogue/Application/Commands/ExecuteDialogueActionHandler.cs
```

## Required event types

Reuse these existing generic events:

```csharp
CommandExecutedEvent<CreateDialogueTreeCommand>
CommandExecutedEvent<UpdateDialogueTreeCommand>
CommandExecutedEvent<DeleteDialogueTreeCommand>
```

Do **not** add:

```text
DialogueTreeCreatedEvent
DialogueTreeUpdatedEvent
DialogueTreeDeletedEvent
DialogueDefinitionChangedEvent
```

unless a missing requirement is found that the command event cannot carry.

The command objects already contain the needed identifiers.

## Required subscriber

Add a dedicated event handler, recommended name:

```text
DialogueNpcSynchronizationHandler
```

Recommended location:

```text
AmiaReforged.PwEngine/
  Features/WorldEngine/
    Subsystems/Dialogue/Application/
      DialogueNpcSynchronizationHandler.cs
```

It should implement:

```csharp
IEventHandler<CommandExecutedEvent<CreateDialogueTreeCommand>>,
IEventHandler<CommandExecutedEvent<UpdateDialogueTreeCommand>>,
IEventHandler<CommandExecutedEvent<DeleteDialogueTreeCommand>>,
IEventHandlerMarker
```

and be registered with Anvil DI using the repository's established event-handler pattern.

## Exact behavior

### Create event

Input:

```csharp
CommandExecutedEvent<CreateDialogueTreeCommand>
```

Use:

```csharp
@event.Command.Tree.DialogueTreeId
@event.Command.Tree.SpeakerTag
```

Behavior:

```text
SpeakerTag null/empty/whitespace
    -> no NPC registration call

SpeakerTag present
    -> synchronizer.RegisterAsync(
           speakerTag,
           dialogueTreeId,
           cancellationToken)
```

Do not re-query the database merely to recover values already carried by the command.

### Update event

Input:

```csharp
CommandExecutedEvent<UpdateDialogueTreeCommand>
```

Use:

```csharp
@event.Command.DialogueTreeId
@event.Command.Tree.SpeakerTag
```

Behavior:

```text
always call synchronizer.UpdateAsync(
    dialogueTreeId,
    newSpeakerTag,
    cancellationToken)
```

The synchronizer/hook already owns the old-tag lookup through its tree/tag registry.

Do not duplicate old-tag tracking in the event subscriber.

### Delete event

Input:

```csharp
CommandExecutedEvent<DeleteDialogueTreeCommand>
```

Use:

```csharp
@event.Command.DialogueTreeId
```

Behavior:

```text
synchronizer.UnregisterAsync(
    dialogueTreeId,
    cancellationToken)
```

Do not re-query the deleted database row after deletion.

The runtime registry exists precisely so deletion can identify the previously registered tag from the tree ID.

## Success/rejection semantics

`CommandDispatcher` emits `CommandExecutedEvent<TCommand>` only after a successful command.

Therefore the normal runtime subscriber should never receive a failed command execution event.

Still, if `CommandExecutedEvent<TCommand>.Result` exposes `Success`, defensively ignoring a non-success event is acceptable and consistent with the store-cache handler.

Do not make the subscriber responsible for deciding whether persistence succeeded.

## Logging

The event subscriber should log enough context to diagnose synchronization, but avoid duplicating all logs already emitted by `DialogueNpcHook`.

Useful fields:

```text
event type
dialogueTreeId
speakerTag when relevant
registered/unregistered counts returned by synchronizer
```

Do not treat "0 NPCs matched" as a command failure. A valid tree can exist before an NPC with that tag is currently spawned.

## Threading rule

The subscriber runs from the asynchronous production event bus.

It must **not** directly touch NWN object APIs.

Correct:

```text
background event handler
  -> IDialogueNpcSynchronizer
  -> DialogueNpcHook
  -> NwTask.SwitchToMainThread()
  -> NwObject / NwCreature access
```

Incorrect:

```text
background event handler
  -> NwObject.FindObjectsWithTag(...)
```

## Tests required by this task

Add focused subscriber tests using a fake or mock `IDialogueNpcSynchronizer`.

Do not construct real `NwCreature` objects for these tests.

### Required create cases

1. successful create + speaker tag:
   - register called exactly once;
   - correct speaker tag forwarded;
   - correct tree ID forwarded.

2. successful create + null/empty speaker tag:
   - register not called.

### Required update cases

3. successful update:
   - update called exactly once;
   - correct immutable route/command tree ID forwarded;
   - new speaker tag forwarded;
   - null new tag is forwarded, not discarded.

### Required delete case

4. successful delete:
   - unregister called exactly once with deleted tree ID.

### Rejection behavior

Add a dispatcher-level or equivalent test showing a rejected dialogue command does not publish the successful command event and therefore cannot trigger synchronization.

Preferred test shape:

```text
failed command handler result
  -> CommandDispatcher.DispatchAsync
  -> no CommandExecutedEvent<TCommand>
  -> synchronization subscriber not invoked
```

Do not fake a failed event path that cannot occur in production and then claim that proves rejection behavior.

## Non-goals

Do not:

- remove controller calls yet — 006C owns that;
- change `DialogueNpcHook` ownership semantics;
- add a database uniqueness constraint;
- add polling or retries;
- wait synchronously for event completion in the controller;
- make HTTP success depend on subscriber completion;
- alter `AnvilEventBusService`;
- introduce a second domain-event family.

## Failure modes to avoid

### 1. Publishing events from the CRUD handlers manually

Do not add:

```csharp
await _eventBus.PublishAsync(...)
```

inside `CreateDialogueTreeHandler`, `UpdateDialogueTreeHandler`, or `DeleteDialogueTreeHandler` for the same successful-command notification.

`CommandDispatcher` already publishes the event.

### 2. Controller publishes the event

The controller should never publish synchronization events.

### 3. Subscriber re-implements hook logic

The subscriber is orchestration only.

It should not know about:

```text
_treeToTag
we_dialogue_tree
NwCreature
HookCreature
UnhookCreature
```

### 4. Subscriber queries the database for deleted tree state

Deletion happens before the event.

Use tree ID plus the synchronizer's runtime registry.

## Acceptance checks

- [ ] One dedicated subscriber handles create/update/delete successful command events.
- [ ] Create with a speaker tag calls register.
- [ ] Create without a speaker tag does nothing.
- [ ] Update forwards tree ID and new tag.
- [ ] Delete forwards only the deleted tree ID.
- [ ] Subscriber depends on `IDialogueNpcSynchronizer`, not concrete `DialogueNpcHook`.
- [ ] Subscriber does not use NWN APIs directly.
- [ ] Subscriber is discoverable through `IEventHandlerMarker`.
- [ ] Rejected commands do not trigger synchronization.
- [ ] No redundant dialogue-definition event family is added.

## Suggested changed files

Likely:

```text
Features/WorldEngine/Subsystems/Dialogue/Application/
  DialogueNpcSynchronizationHandler.cs
```

plus one or more test files.

Possible test location:

```text
Features/WorldEngine/Subsystems/Dialogue/Tests/
```

Follow existing test folder conventions if they differ.

## Suggested verification commands

Targeted:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --filter 'FullyQualifiedName~DialogueNpcSynchronization' \
  --verbosity minimal \
  -m:1
```

Then WorldEngine regression suite:

```sh
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj \
  --no-build \
  --no-restore \
  --filter 'FullyQualifiedName~WorldEngine' \
  --verbosity minimal \
  -m:1
```

Structural check:

```sh
grep -Rni "CommandExecutedEvent<.*DialogueTreeCommand" \
  AmiaReforged.PwEngine/Features/WorldEngine/Subsystems/Dialogue
```

## Completion evidence

### Subscriber

Record:

- class name;
- event interfaces implemented;
- synchronizer dependency;
- exact create/update/delete mapping.

### Rejection proof

Describe the exact test proving failed commands do not cause synchronization.

### Changed files

```text
- ...
```

### Verification command

```sh
...
```

### Observed result

Record exact test counts/result.

### Deferred observations

List unrelated issues noticed but not changed.
