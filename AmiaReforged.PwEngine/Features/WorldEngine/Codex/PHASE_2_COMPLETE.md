# Phase 2: Entities & Aggregate Root - COMPLETE ✅

## Overview
Phase 2 successfully implemented the domain entities and aggregate root for the Codex system, building upon the value objects, enums, and domain events from Phase 1.

---

## Entities Created

### 1. CodexQuestEntry
**Location:** `Features/WorldEngine/Codex/Entities/CodexQuestEntry.cs`

**Purpose:** Represents a quest entry in a player's codex.

**Key Features:**
- Tracks quest state progression (Discovered → InProgress → Completed/Failed/Abandoned)
- Maintains quest objectives, quest giver, location
- State transition methods with invariant enforcement
- Search functionality across all fields
- Keyword-based categorization

**State Transitions:**
- `MarkCompleted()` - Transitions to Completed state
- `MarkFailed()` - Transitions to Failed state
- `MarkAbandoned()` - Transitions to Abandoned state
- All transitions validate current state and throw `InvalidOperationException` for invalid transitions

**Tests:** 30 comprehensive tests in `Tests/Systems/Codex/Entities/CodexQuestEntryTests.cs`

---

### 2. CodexLoreEntry
**Location:** `Features/WorldEngine/Codex/Entities/CodexLoreEntry.cs`

**Purpose:** Represents immutable lore discovered by a player.

**Key Features:**
- Immutable once discovered
- Categorized by LoreTier (Common/Uncommon/Rare/Legendary)
- Tracks discovery location and source
- Keyword-based searching
- Category and tier filtering

**Query Methods:**
- `MatchesSearch()` - Full-text search across all fields
- `MatchesTier()` - Filter by lore tier
- `MatchesCategory()` - Case-insensitive category matching

**Tests:** 28 comprehensive tests in `Tests/Systems/Codex/Entities/CodexLoreEntryTests.cs`

---

### 3. CodexNoteEntry
**Location:** `Features/WorldEngine/Codex/Entities/CodexNoteEntry.cs`

**Purpose:** Represents mutable player or DM notes.

**Key Features:**
- Fully mutable content (unlike quests/lore)
- Categorized by NoteCategory (General/Quest/Character/Location/DmNote/DmPrivate)
- Tracks creation and modification timestamps
- Privacy controls (public/private notes)
- DM note support

**Mutation Methods:**
- `UpdateContent()` - Changes note text
- `UpdateTitle()` - Changes note title
- `UpdateCategory()` - Changes category
- `UpdatePrivacy()` - Toggles privacy setting
- All mutations update `LastModified` timestamp

**Validation:**
- Constructor validates non-empty content
- Constructor validates non-empty GUID
- Update methods validate non-empty content

**Tests:** 37 comprehensive tests in `Tests/Systems/Codex/Entities/CodexNoteEntryTests.cs`

---

### 4. FactionReputation
**Location:** `Features/WorldEngine/Codex/Entities/FactionReputation.cs`

**Purpose:** Tracks character reputation with factions.

**Key Features:**
- Reputation score with automatic clamping (-100 to +100)
- History tracking of all reputation changes
- Standing calculation (Exalted/Revered/Honored/Friendly/Neutral/Unfriendly/Hostile/Hated/Nemesis)
- Immutable adjustment pattern

**Reputation Standings:**
| Score Range | Standing |
|------------|----------|
| ≥ 75 | Exalted |
| ≥ 50 | Revered |
| ≥ 25 | Honored |
| ≥ 10 | Friendly |
| > -10 | Neutral |
| > -25 | Unfriendly |
| > -50 | Hostile |
| > -75 | Hated |
| ≤ -75 | Nemesis |

**Note:** Boundaries are exclusive (e.g., -10 is "Unfriendly", not "Neutral")

**Methods:**
- `AdjustReputation()` - Modifies reputation with reason tracking
- `GetStanding()` - Returns descriptive standing string
- `IsAtLeast()` / `IsAtMost()` - Threshold checking

**History Tracking:**
- `ReputationChange` record tracks each adjustment
- Stores timestamp, delta, old score, new score, reason
- Read-only `History` property provides full audit trail

**Tests:** 32 comprehensive tests in `Tests/Systems/Codex/Entities/FactionReputationTests.cs`

---

## Aggregate Root

### PlayerCodex
**Location:** `Features/WorldEngine/Codex/Aggregates/PlayerCodex.cs`

**Purpose:** Aggregate root managing all codex data for a player or DM.

**Key Responsibilities:**
- Encapsulates all codex entities (quests, lore, notes, reputations)
- Enforces invariants (no duplicate IDs, proper state transitions)
- Provides command methods for mutations
- Provides query methods for retrieval
- Tracks LastUpdated timestamp
- Supports both CharacterId and DmId (polymorphic)

**Command Methods:**

#### Quest Commands
- `RecordQuestStarted()` - Adds new quest
- `RecordQuestCompleted()` - Marks quest complete
- `RecordQuestFailed()` - Marks quest failed
- `RecordQuestAbandoned()` - Marks quest abandoned
- `GetQuest()` / `HasQuest()` - Retrieval

#### Lore Commands
- `RecordLoreDiscovered()` - Adds new lore
- `GetLore()` / `HasLore()` - Retrieval

#### Note Commands
- `AddNote()` - Adds new note
- `EditNote()` - Updates note content
- `DeleteNote()` - Removes note
- `GetNote()` / `HasNote()` - Retrieval

#### Reputation Commands
- `RecordReputationChange()` - Creates or updates faction reputation
- `GetReputation()` / `HasReputation()` - Retrieval

**Query Methods:**
- `GetQuestsByState()` - Filter quests by state
- `GetLoreByTier()` - Filter lore by tier
- `GetNotesByCategory()` - Filter notes by category
- `SearchQuests()` / `SearchLore()` / `SearchNotes()` - Full-text search
- `GetTotalEntryCount()` - Total entries across all types

**Invariants Enforced:**
- No duplicate quest IDs (throws `InvalidOperationException`)
- No duplicate lore IDs (throws `InvalidOperationException`)
- No duplicate note IDs (throws `InvalidOperationException`)
- Quest state transitions validated
- All commands update `LastUpdated` timestamp
- Null arguments rejected (throws `ArgumentNullException`)

**DM Support:**
- Accepts `CharacterId` as owner (includes DmId via implicit conversion)
- DMs can maintain persistent codex across character avatars
- Same command/query methods work for players and DMs

**Tests:** 59 comprehensive tests in `Tests/Systems/Codex/Aggregates/PlayerCodexTests.cs`

---

## Test Summary

| Entity/Aggregate | Test File | Test Count | Status |
|------------------|-----------|------------|--------|
| CodexQuestEntry | CodexQuestEntryTests.cs | 30 | ✅ Passing |
| CodexLoreEntry | CodexLoreEntryTests.cs | 28 | ✅ Passing |
| CodexNoteEntry | CodexNoteEntryTests.cs | 37 | ✅ Passing |
| FactionReputation | FactionReputationTests.cs | 32 | ✅ Passing |
| PlayerCodex | PlayerCodexTests.cs | 59 | ✅ Passing |
| **TOTAL** | **5 files** | **186 tests** | **✅ All Passing** |

**Overall Project Test Status:**
- **Total Tests:** 422 (236 previous + 186 new)
- **Passing:** 422 ✅
- **Failing:** 0
- **Build:** Clean (0 errors, warnings pre-existing)

---

## Technical Decisions

### 1. Entity Design Patterns

**Immutability Where Appropriate:**
- `CodexQuestEntry` - Mutable state via controlled methods
- `CodexLoreEntry` - Fully immutable after discovery
- `CodexNoteEntry` - Fully mutable for player notes
- `FactionReputation` - Mutable via controlled `AdjustReputation()` method

**Encapsulation:**
- Private setters on state properties
- Public mutation methods with validation
- Read-only collection properties

### 2. Aggregate Boundary

**PlayerCodex as Aggregate Root:**
- All entities accessed through PlayerCodex
- No direct entity repositories
- Enforces consistency boundaries
- Single transaction boundary

**Rationale:**
- Codex is a cohesive unit (quests/lore/notes/reputation)
- Strong consistency needed within a player's codex
- Simplifies persistence (single aggregate save)
- Natural DDD aggregate boundary

### 3. Search Functionality

**Entity-Level Search:**
- Each entity implements `MatchesSearch()`
- Case-insensitive matching
- Null-safe for optional properties
- Keyword support

**Aggregate-Level Search:**
- PlayerCodex provides `Search*()` methods
- Delegates to entity `MatchesSearch()`
- Returns `IEnumerable<T>` for LINQ composition

### 4. History Tracking

**FactionReputation History:**
- Immutable `ReputationChange` records
- Captures old/new scores and reason
- Read-only `History` property
- Enables audit trails and rollback

**Future Consideration:**
- Could extend to quest/note edit history
- Event sourcing pattern compatible

### 5. Validation Strategy

**Constructor Validation:**
- Required properties validated in constructors
- Null/empty checks for IDs and content
- Throws `ArgumentException` for invalid data

**Method Validation:**
- State transition validation in Mark* methods
- Existence validation in Get/Update/Delete methods
- Throws `InvalidOperationException` for business rule violations

### 6. DM Support Integration

**Polymorphic Owner:**
- `OwnerId` is `CharacterId` type
- `DmId` implicitly converts to `CharacterId`
- Same aggregate works for players and DMs
- No special DM code paths needed

---

## Issues Resolved

### 1. Compilation Errors (32 fixes)
**Problem:** Tests tried to set private properties via object initializer.
**Solution:** Use constructors properly, especially `FactionReputation(score, date)`.

### 2. Test Logic Errors (4 fixes)
**Problems:**
- Reputation standing boundaries misunderstood (> vs ≥)
- IsAtMost test assertions backwards
- Quest search tests matched on unintended fields

**Solutions:**
- Corrected boundary expectations (e.g., -10 is "Unfriendly", not "Neutral")
- Fixed IsAtMost assertions (-30 ≤ -20 is True)
- Changed test data to avoid false matches

---

## Next Steps: Phase 3

### Application Layer
1. **CodexEventProcessor**
   - Processes events from channel
   - Applies events to PlayerCodex aggregate
   - Handles concurrent updates

2. **CodexQueryService**
   - Read-only queries with DTOs
   - Search/filter operations
   - Query optimization

3. **Event Handlers**
   - QuestEventHandler
   - LoreEventHandler
   - NoteEventHandler
   - ReputationEventHandler
   - TraitEventHandler

### Infrastructure Layer (Phase 4)
1. **IPlayerCodexRepository**
   - Interface for persistence
   - Load/Save PlayerCodex aggregate

2. **JsonPlayerCodexRepository**
   - JSON file-based persistence
   - Atomic save operations

3. **Channel Setup**
   - Configure event channels
   - Sequential per-character processing

4. **NWN Adapter**
   - Anti-corruption layer
   - Translate NWN events to domain events

---

## Summary

Phase 2 successfully implemented:
- ✅ 4 domain entities (Quest, Lore, Note, Reputation)
- ✅ 1 aggregate root (PlayerCodex)
- ✅ 186 comprehensive tests (all passing)
- ✅ DM support via polymorphic CharacterId
- ✅ Search, filtering, and query capabilities
- ✅ Immutable/mutable patterns as appropriate
- ✅ State transition validation
- ✅ History tracking
- ✅ Clean build (0 errors)

**Total Project Progress:**
- **Tests:** 422/422 passing (100%)
- **Coverage:** Entities, Aggregates, Value Objects, Enums, Domain Events
- **Production Ready:** Phase 1 + Phase 2 foundations complete

Next: Phase 3 (Application Layer) to wire up event processing and queries.
