# Quest State Fix - QuestStartedEvent

**Date**: October 28, 2025
**Issue**: Test `Given_QuestStartedEvent_When_Processed_Then_QuestAddedToCodex` failing
**Status**: ✅ Fixed

---

## Problem

When a `QuestStartedEvent` was processed, the resulting quest entry had a state of `Discovered` instead of the expected `InProgress`.

### Root Cause

1. The `QuestState` enum has `Discovered = 0` as its default value
2. The `CodexQuestEntry.State` property was initialized to the default enum value (Discovered)
3. The `CreateQuestEntry()` method in `CodexEventProcessor` was not explicitly setting the `State` property
4. The `State` property had a `private set` accessor, preventing external initialization

### Error Message
```
Expected: InProgress
But was:  Discovered
```

---

## Solution

### 1. Made State Property Settable During Initialization

**File**: `Features/WorldEngine/Codex/Entities/CodexQuestEntry.cs`

Changed the `State` property from:
```csharp
public QuestState State { get; private set; }
```

To:
```csharp
public QuestState State { get; internal set; } = QuestState.Discovered;
```

**Rationale**:
- `internal set` allows the property to be set within the same assembly (e.g., by CodexEventProcessor)
- Explicit default value `= QuestState.Discovered` documents the default state
- Methods like `MarkCompleted()`, `MarkFailed()`, `MarkAbandoned()` can still modify the state internally

### 2. Set State to InProgress When Creating Quest Entry

**File**: `Features/WorldEngine/Codex/Application/CodexEventProcessor.cs`

Added the `State` property initialization in `CreateQuestEntry()`:
```csharp
private CodexQuestEntry CreateQuestEntry(QuestStartedEvent evt)
{
    return new CodexQuestEntry
    {
        QuestId = evt.QuestId,
        Title = evt.QuestName,
        Description = evt.Description,
        State = QuestState.InProgress,  // ← Added this line
        DateStarted = evt.OccurredAt,
        Keywords = new List<Keyword>()
    };
}
```

### 3. Added Missing Using Statement

Added `using AmiaReforged.PwEngine.Features.Codex.Domain.Enums;` to access `QuestState` enum.

---

## Design Rationale

### Why `internal set` Instead of `init`?

The `State` property needs to be modified by methods within the entity:
- `MarkCompleted()` sets State to `Completed`
- `MarkFailed()` sets State to `Failed`
- `MarkAbandoned()` sets State to `Abandoned`

Using `init` would only allow setting during object initialization, breaking these methods.

Using `internal set` allows:
- ✅ Setting during initialization (by CodexEventProcessor)
- ✅ Modification by entity methods
- ✅ Protection from external modification (only within assembly)

### Quest State Lifecycle

```
QuestStartedEvent → InProgress
                    ↓
        ┌───────────┼───────────┐
        ↓           ↓           ↓
   Completed    Failed    Abandoned
   (terminal)  (terminal)  (terminal)
```

Note: `Discovered` state exists for quests that are known but not yet started. This would be used for a separate `QuestDiscoveredEvent` if implemented in the future.

---

## Files Modified

1. **CodexQuestEntry.cs** - Changed State property accessor
2. **CodexEventProcessor.cs** - Added State initialization and using statement

---

## Testing

### Expected Behavior
When `QuestStartedEvent` is processed:
- ✅ Quest is added to codex
- ✅ Quest state is `InProgress`
- ✅ Quest title, description, and ID match the event

### Test Coverage
- `Given_QuestStartedEvent_When_Processed_Then_QuestAddedToCodex` - Now passes ✅
- All quest state transition tests remain valid

---

## Related Issues

This fix ensures proper quest lifecycle tracking and aligns with the domain model where:
- `QuestStartedEvent` → Quest begins in `InProgress` state
- `QuestCompletedEvent` → Transitions to `Completed` (terminal)
- `QuestFailedEvent` → Transitions to `Failed` (terminal)
- `QuestAbandonedEvent` → Transitions to `Abandoned` (terminal)

The `Discovered` state is reserved for future use (e.g., quest hints or rumors that haven't been officially started).

