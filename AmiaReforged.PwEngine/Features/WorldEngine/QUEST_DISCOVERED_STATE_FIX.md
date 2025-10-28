# Quest Discovered State Fix

**Date**: October 28, 2025
**Issue**: Test `GetQuestsByState_WithDiscoveredState_ReturnsOnlyDiscoveredQuests` failing
**Status**: ✅ Fixed

---

## Problem

The test `GetQuestsByState_WithDiscoveredState_ReturnsOnlyDiscoveredQuests` was failing because:

1. Test expected to find 1 quest in `Discovered` state
2. Test was calling `RecordQuestStarted()` for both quests
3. Our previous fix made `RecordQuestStarted()` enforce `InProgress` state
4. Result: Expected 1, but got 0 quests in `Discovered` state

### Error Message
```
Expected: property Count equal to 1
But was:  0
```

### Test Code (Before)
```csharp
codex.RecordQuestStarted(quest1, _testDate); // Forced to InProgress
codex.RecordQuestStarted(quest2, _testDate); // Forced to InProgress
codex.RecordQuestCompleted(quest2.QuestId, _testDate.AddHours(1));

var result = codex.GetQuestsByState(QuestState.Discovered).ToList();
// Expected 1, got 0 - no quests are in Discovered state!
```

---

## Root Cause

The test had a **conceptual mismatch** between intent and implementation:

- **Test Intent**: Verify filtering quests by `Discovered` state
- **Test Implementation**: Called `RecordQuestStarted()` which now enforces `InProgress` state
- **Previous Behavior**: `RecordQuestStarted()` left state as default (Discovered)
- **New Behavior**: `RecordQuestStarted()` enforces `InProgress` state (correct domain rule)

The test was relying on a **bug** (quests staying in Discovered state when started) rather than proper domain behavior.

---

## Solution

Added a new method `RecordQuestDiscovered()` to properly support the `Discovered` state for quests that are known but not yet started.

### 1. Added RecordQuestDiscovered Method

**File**: `Features/WorldEngine/Codex/Aggregates/PlayerCodex.cs`

```csharp
/// <summary>
/// Records a quest as discovered (known but not started) in the codex.
/// Used for quest hints, rumors, or quests offered but not accepted.
/// </summary>
public void RecordQuestDiscovered(CodexQuestEntry quest, DateTime occurredAt)
{
    ArgumentNullException.ThrowIfNull(quest);

    if (_quests.ContainsKey(quest.QuestId))
        throw new InvalidOperationException($"Quest {quest.QuestId.Value} already exists in codex");

    // Ensure quest is in Discovered state when discovered
    quest.State = QuestState.Discovered;

    _quests[quest.QuestId] = quest;
    LastUpdated = occurredAt;
}
```

### 2. Updated Test to Use Correct Method

**File**: `Tests/Systems/Codex/Aggregates/PlayerCodexTests.cs`

```csharp
// Before:
codex.RecordQuestStarted(quest1, _testDate); // Wrong - forces InProgress

// After:
codex.RecordQuestDiscovered(quest1, _testDate); // Correct - keeps as Discovered
```

---

## Domain Model Enhancement

This fix actually **improves the domain model** by making quest states more explicit:

### Quest Lifecycle (Updated)

```
┌─────────────────┐
│ RecordQuestDiscovered()
│ ↓
│ Discovered      │  ← Quest is known (hint, rumor, offered but not accepted)
└─────────────────┘
        ↓
  RecordQuestStarted()
        ↓
┌─────────────────┐
│ InProgress      │  ← Quest is actively being pursued
└─────────────────┘
        ↓
   ┌────┴────┬────────────┬─────────────┐
   ↓         ↓            ↓             ↓
Completed  Failed    Abandoned   (terminal states)
```

### Use Cases for RecordQuestDiscovered

1. **Quest Hints**: NPC mentions a quest but player doesn't accept it yet
2. **Quest Rumors**: Player hears about a quest from multiple NPCs
3. **Quest Offers**: Quest giver presents quest, player can decide later
4. **Quest Prerequisites**: Player discovers a quest but can't start it yet

### Use Cases for RecordQuestStarted

1. **Quest Acceptance**: Player explicitly accepts a quest
2. **Quest Trigger**: Quest automatically starts based on player actions
3. **Quest Continuation**: Player resumes a previously discovered quest

---

## API Clarity

Now we have **two explicit methods** with clear semantics:

```csharp
// For quests that are known but not started
codex.RecordQuestDiscovered(quest, occurredAt);
// → State: Discovered

// For quests that player is actively pursuing
codex.RecordQuestStarted(quest, occurredAt);
// → State: InProgress
```

### Benefits

✅ **Explicit Intent**: Method names clearly communicate what happens
✅ **Type Safety**: Can't accidentally put quest in wrong state
✅ **Domain Alignment**: Matches real-world quest discovery flow
✅ **Backward Compatible**: Existing `RecordQuestStarted` still works
✅ **Test Clarity**: Tests express exact behavior being tested

---

## Files Modified

1. **PlayerCodex.cs** - Added `RecordQuestDiscovered()` method
2. **PlayerCodexTests.cs** - Updated test to use `RecordQuestDiscovered()`

---

## Testing

### Before Fix
```
❌ GetQuestsByState_WithDiscoveredState_ReturnsOnlyDiscoveredQuests
   Expected: 1 Discovered quest
   But was:  0 Discovered quests
```

### After Fix
```
✅ GetQuestsByState_WithDiscoveredState_ReturnsOnlyDiscoveredQuests
   Expected: 1 Discovered quest
   Got:      1 Discovered quest (quest1)
   ✅ Test passes!
```

### Test Scenario
```csharp
// Quest 1: Discovered but not started
codex.RecordQuestDiscovered(quest1, date);
Assert: quest1.State == Discovered ✅

// Quest 2: Started and completed
codex.RecordQuestStarted(quest2, date);
Assert: quest2.State == InProgress ✅

codex.RecordQuestCompleted(quest2.QuestId, date);
Assert: quest2.State == Completed ✅

// Filter by state
var discovered = codex.GetQuestsByState(Discovered);
Assert: Contains only quest1 ✅
```

---

## Domain Design Improvement

This fix demonstrates **good DDD practices**:

### 1. Explicit State Transitions

```csharp
// ❌ Bad - Implicit state management
RecordQuest(quest); // What state is it in?

// ✅ Good - Explicit state transitions
RecordQuestDiscovered(quest); // State: Discovered
RecordQuestStarted(quest);    // State: InProgress
```

### 2. Ubiquitous Language

The method names match how domain experts (game designers, DMs) talk about quests:
- "The player **discovered** the quest from an NPC"
- "The player **started** the quest by accepting it"
- "The player **completed** the quest"

### 3. Aggregate Responsibility

The `PlayerCodex` aggregate enforces quest state invariants:
- Discovered quests → `Discovered` state
- Started quests → `InProgress` state
- Completed quests → `Completed` state

You can't accidentally create invalid states.

---

## Related Documentation

- `QUEST_STATE_FIX.md` - Original quest state enforcement
- `QUEST_STATE_CONSISTENCY_FIX.md` - RecordQuestStarted enforcement
- This document - Discovered state support

---

## Summary

✅ **Fixed** the failing test by adding proper `Discovered` state support
✅ **Enhanced** the domain model with explicit quest discovery
✅ **Improved** API clarity with intention-revealing method names
✅ **Maintained** all existing functionality and tests
✅ **Zero** breaking changes to existing code

The quest state management is now complete and robust! All quest lifecycle transitions are properly supported with clear, type-safe APIs.

