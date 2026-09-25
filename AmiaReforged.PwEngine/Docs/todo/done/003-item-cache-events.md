# 003 — Invalidate item expansion caches through events

Status: **Completed**
Type: **Implementation**
Audit area: **F-1**
Depends on: None.

## Current gap

Item cache invalidation is owned by HTTP controllers, so other command callers can miss it.

## Change

Move invalidation to an event subscriber for successful item definition changes. Remove controller invalidation calls. Define the read-after-write behavior explicitly because the production event bus is asynchronous.

## Starting points

- [API/Controllers/ItemController.cs](../../Features/WorldEngine/API/Controllers/ItemController.cs)
- [Application/Items/Commands/ItemDefinitionCommands.cs](../../Features/WorldEngine/Application/Items/Commands/ItemDefinitionCommands.cs)

## Acceptance checks

- [x] Successful upsert/delete commands invalidate through the subscriber; rejected commands do not.
- [x] Import uses the same mechanism without a second controller-owned invalidation path.
- [x] A test covers cache freshness under the chosen asynchronous contract.

## Completion evidence

### Chosen behavior (decision)

- Invalidation moved to a new event subscriber, `Application/Items/Events/ItemDefinitionCacheInvalidationHandler.cs`,
  implementing `IEventHandler<CommandExecutedEvent<UpsertItemDefinitionCommand>>` and
  `IEventHandler<CommandExecutedEvent<DeleteItemDefinitionCommand>>`, binding as `IEventHandlerMarker`.
  It injects `ItemBlueprintExpander` and calls `Invalidate()` on each event.
- It reacts to `CommandExecutedEvent<T>`, which the `CommandDispatcher` publishes automatically and only
  for **successful** commands — rejected commands never publish the event and therefore never invalidate.
- All four controller call sites (`Create`, `Update`, `Delete`, `Import`) that called `InvalidateExpander()`
  had the call removed along with the helper. `Import` flows through the dispatcher, so it reuses the same
  single path — there is no second controller-owned invalidation path.

**Read-after-write contract (asynchronous bus):** the production bus (`AnvilEventBusService`) queues events
and runs subscribers on a background thread, so invalidation is **eventually consistent** — a read issued
immediately after a successful command may still see the previously expanded concrete items until the
background processor runs the subscriber. Correctness does not depend on ordering because the command
handler commits the new/deleted blueprint to the repository **synchronously before** the event is published;
when the subscriber eventually runs, `Invalidate()` re-expands from already-fresh state. Callers that must
observe the change synchronously should await an explicit invalidation rather than rely on the event.

### Changed files

- `Features/WorldEngine/Application/Items/Events/ItemDefinitionCacheInvalidationHandler.cs` (new)
- `Features/WorldEngine/API/Controllers/ItemController.cs` (removed 4 `InvalidateExpander()` calls + helper)
- `Features/WorldEngine/Subsystems/Items/InMemoryItemDefinitionRepository.cs` (added `RemoveByTag` test helper)
- `Features/WorldEngine/Subsystems/Items/Tests/ItemDefinitionCacheInvalidationHandlerTests.cs` (new test)

### Verification

Command:

```
dotnet build AmiaReforged.PwEngine.csproj -c Debug
dotnet test AmiaReforged.PwEngine.csproj --no-build --filter "FullyQualifiedName~ItemDefinitionCacheInvalidationHandlerTests"
```

Observed result:

- Build succeeded (0 errors).
- `ItemDefinitionCacheInvalidationHandlerTests`: **4/4 passed** —
  - `SuccessfulUpsert_ThroughDispatcher_InvalidatesRealExpander`: successful upsert publishes exactly one
    event and the expanded cache reflects the new blueprint (1 → 2 items).
  - `DeleteEvent_InvalidatesExpander_ThenCacheReflectsDeletion`: delete event invalidates and the cache
    no longer contains the deleted blueprint's items (1 → 0).
  - `RejectedCommand_ThroughDispatcher_DoesNotPublishEventOrInvalidate`: a rejected command publishes no
    event and leaves the cache untouched.
  - `Freshness_WhenSubscriberRuns_ReadsAlreadyCommittedState`: documents/asserts that the subscriber reads
    already-committed state, which is what makes eventual consistency correct.
- Related suites still green: `GetExpandedItemDefinitionsQueryHandlerTests`,
  `CommandDispatcherBehavior`, `IndustriesEventFlowTests` — **23/23 passed**.

See [backlog scope and completion rules](README.md).

