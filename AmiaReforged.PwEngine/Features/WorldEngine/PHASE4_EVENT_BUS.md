# Phase 4: Event Bus and Domain Events

**Status**: ⏳ Not Started
**Planned Start**: Week 7

---

## Goal

Implement a Channel-based async event bus to enable loose coupling between subsystems. Allow features to react to world changes without direct dependencies.

---

## Event Infrastructure

### IDomainEvent
```csharp
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    PersonaId Actor { get; }
}
```

### IEventBus
```csharp
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
        where TEvent : IDomainEvent;

    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IDomainEvent;
}
```

---

## Architecture: Channel-Based Processing

### Design
- **In-Game Thread**: Commands publish events to `Channel<IDomainEvent>`
- **Background Tasks**: Consumer tasks read from channel and process in parallel
- **Schedulers**: Use NWN scheduler to sync game-thread-required reactions
- **Event Persistence**: Background task writes to database

### Benefits
- Non-blocking game loop
- True parallelism for event processing
- Keeps game state mutations safe on game thread
- Enables audit logging

---

## Example Events

### Economy
- `GoldTransferredEvent`
- `ResourceHarvestedEvent`
- `ProductionCompletedEvent`

### Organizations
- `ReputationGrantedEvent`
- `MembershipChangedEvent`

### Quests
- `QuestCompletedEvent`
- `QuestFailedEvent`

---

## Migration Path

1. Implement `ChannelEventBus` with background processing
2. Update command handlers to publish events
3. Create event subscribers for cross-cutting concerns
4. Wire up inter-subsystem reactions
5. Add persistent event store

---

## Related Documents

- `RESEARCH_CHANNEL_EVENT_BUS.md` (to be created)

---

**Previous Phase**: [Phase 3.4: Other Subsystems](PHASE3_4_OTHER_SUBSYSTEMS.md)
**Next Phase**: [Phase 5: Public API](PHASE5_PUBLIC_API.md)

