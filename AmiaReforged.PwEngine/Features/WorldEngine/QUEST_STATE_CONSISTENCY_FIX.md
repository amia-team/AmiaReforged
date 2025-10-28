# Quest State Consistency Fix - RecordQuestStarted

**Date**: October 28, 2025
**Issue**: Test `Given_CodexWithMixedQuestStates_When_GetQuestsByState_Then_ReturnsFilteredQuests` failing
**Status**: ✅ Fixed

---

## Problem

When quests were created directly in tests and passed to `RecordQuestStarted()`, they retained their default `Discovered` state instead of transitioning to `InProgress` state. This caused the test to fail because:

1. Test created quest entries without explicitly setting `State` property
2. Default `QuestState` enum value is `Discovered` (value 0)
3. `RecordQuestStarted()` stored quests as-is without validating/updating state
4. Query for `InProgress` quests returned 0 results instead of expected 1

### Error Message
```
Expected: 1
But was:  0
```

### Test Code
```csharp
CodexQuestEntry inProgressQuest = new CodexQuestEntry
{
    QuestId = QuestId.NewId(),
    Title = "In Progress Quest",
    Description = "Description",
    DateStarted = DateTime.UtcNow
    // State not set - defaults to Discovered
};

_codex.RecordQuestStarted(inProgressQuest, DateTime.UtcNow);
```

---

## Solution

### Modified `RecordQuestStarted` to Enforce State Invariant

**File**: `Features/WorldEngine/Codex/Aggregates/PlayerCodex.cs`

**Before**:
```csharp
public void RecordQuestStarted(CodexQuestEntry quest, DateTime occurredAt)
{
    ArgumentNullException.ThrowIfNull(quest);

    if (_quests.ContainsKey(quest.QuestId))
        throw new InvalidOperationException($"Quest {quest.QuestId.Value} already exists in codex");

    _quests[quest.QuestId] = quest;
    LastUpdated = occurredAt;
}
```

**After**:
```csharp
public void RecordQuestStarted(CodexQuestEntry quest, DateTime occurredAt)
{
    ArgumentNullException.ThrowIfNull(quest);

    if (_quests.ContainsKey(quest.QuestId))
        throw new InvalidOperationException($"Quest {quest.QuestId.Value} already exists in codex");

    // Ensure quest is in InProgress state when started
    quest.State = QuestState.InProgress;

    _quests[quest.QuestId] = quest;
    LastUpdated = occurredAt;
}
```

---

## Design Rationale

### Domain Invariant Enforcement

The aggregate root (`PlayerCodex`) is responsible for maintaining domain invariants. When a quest is "started" (via `RecordQuestStarted`), it should **always** be in the `InProgress` state, regardless of how the quest entry was created.

This change ensures:
- ✅ **Consistency**: All started quests are in `InProgress` state
- ✅ **Robustness**: Tests don't need to manually set state
- ✅ **Intent**: Method name `RecordQuestStarted` clearly implies state transition
- ✅ **Encapsulation**: Aggregate enforces business rules

### Why Not Fix the Test Instead?

We could have modified the test to set `State = QuestState.InProgress` when creating quest entries, but this approach has drawbacks:

❌ **Test Fragility**: Every test would need to remember to set state correctly
❌ **Inconsistency Risk**: Easy to forget in future tests
❌ **Weak Invariant**: Aggregate doesn't enforce domain rule
❌ **Violates DDD**: Aggregate should protect its invariants

By enforcing the invariant in the aggregate, we:
- ✅ Make invalid states unrepresentable
- ✅ Reduce test boilerplate
- ✅ Prevent bugs in production code

### Quest State Lifecycle (Updated)

```
RecordQuestStarted() → InProgress (enforced by aggregate)
                       ↓
           ┌───────────┼───────────┐
           ↓           ↓           ↓
      Completed    Failed    Abandoned
      (terminal)  (terminal)  (terminal)
```

The `Discovered` state exists for scenarios where:
- A quest hint is revealed but not accepted
- A quest giver mentions a quest in dialogue
- A quest exists in the world but player hasn't started it

These would use a different method like `RecordQuestDiscovered()` (future enhancement).

---

## Relationship to Previous Fix

This fix complements the previous `QuestStartedEvent` fix:

1. **Event Processor Path**: `QuestStartedEvent` → `CreateQuestEntry()` sets `State = InProgress`
2. **Direct Path**: `RecordQuestStarted()` ensures `State = InProgress`

Both paths now correctly enforce the domain invariant that started quests are in progress.

---

## Files Modified

1. **PlayerCodex.cs** - Added state enforcement in `RecordQuestStarted()`

---

## Testing

### Test Scenario
```csharp
// Create quests without setting state
CodexQuestEntry quest1 = new CodexQuestEntry { /* ... */ };
CodexQuestEntry quest2 = new CodexQuestEntry { /* ... */ };

// Record them as started
_codex.RecordQuestStarted(quest1, DateTime.UtcNow);
_codex.RecordQuestStarted(quest2, DateTime.UtcNow);

// Complete one quest
_codex.RecordQuestCompleted(quest2.QuestId, DateTime.UtcNow);

// Query by state
var inProgress = await GetQuestsByStateAsync(QuestState.InProgress);
var completed = await GetQuestsByStateAsync(QuestState.Completed);
```

### Expected Results
- ✅ `inProgress.Count == 1` (quest1)
- ✅ `completed.Count == 1` (quest2)
- ✅ Both quests correctly transitioned through states

---

## Impact

### Benefits
- **Tests are simpler**: No need to manually set `State` property
- **Domain integrity**: Aggregate enforces business rules
- **Bug prevention**: Impossible to have started quests in wrong state
- **Clear intent**: Method name matches behavior

### Breaking Changes
None - this is a behavior enhancement that makes the API more robust. Any code that was already setting the state correctly will continue to work.

---

## Related Documentation

- `QUEST_STATE_FIX.md` - Event processor quest state handling
- `PHASE3_PART2_COMPLETE.md` - Overall Codex implementation
- Domain model: Quest lifecycle and state transitions

---

## Summary

By enforcing the domain invariant that **all started quests must be in InProgress state** within the aggregate root, we:

1. ✅ Fixed the failing test
2. ✅ Improved domain model correctness
3. ✅ Simplified test code
4. ✅ Prevented future bugs

This is proper Domain-Driven Design: the aggregate root protects its invariants and ensures consistency regardless of how objects are constructed.

