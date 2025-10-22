# Enchiridion Amiae – Domain-Driven Design Specifications

## Bounded Context

**Codex Domain**: Tracks character knowledge, discoveries, and progress
**NWN Domain**: Game engine, triggers events (separate bounded context)

**Anti-Corruption Layer**: NWN adapter translates NWN events → Codex domain events

---

## Domain Model Architecture

### Aggregates

#### 1. **PlayerCodex** (Aggregate Root)
- **Identity**: `CharacterId` (value object)
- **Invariants**:
  - CharacterId must be valid (not empty)
  - LastUpdated must be in the past or present
  - All pages must be initialized
  - Cannot have duplicate entries with same identifier within a page
- **Responsibilities**:
  - Maintain consistency of all codex pages
  - Enforce entry uniqueness rules
  - Track when codex was last modified
  - Provide unified search across all pages

---

### Entities

#### 1. **CodexQuestEntry** (Entity within Quest Page)
- **Identity**: `QuestId` (value object)
- **Mutable State**: `State`, `ObjectiveProgress`
- **Lifecycle**: Discovered → InProgress → (Completed | Failed | Abandoned)

#### 2. **CodexNoteEntry** (Entity within Notes Page)
- **Identity**: `NoteId` (Guid)
- **Mutable State**: `Content`, `LastEditedAt`
- **Lifecycle**: Created → Edited* → (Deleted)

---

### Value Objects

#### 1. **CharacterId**
```csharp
public readonly record struct CharacterId(Guid Value)
{
    public static CharacterId New() => new(Guid.NewGuid());
    public static CharacterId From(Guid id) =>
        id == Guid.Empty ? throw new ArgumentException("CharacterId cannot be empty") : new(id);
}
```

#### 2. **QuestId**
```csharp
public readonly record struct QuestId(string Value)
{
    public QuestId
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Value))
                throw new ArgumentException("QuestId cannot be empty");
        }
    }
}
```

#### 3. **LoreId**
```csharp
public readonly record struct LoreId(string Value)
{
    public LoreId
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Value))
                throw new ArgumentException("LoreId cannot be empty");
        }
    }
}
```

#### 4. **EventType**
```csharp
public readonly record struct EventType(string Value)
{
    public static EventType QuestStarted => new("Quest_Started");
    public static EventType QuestCompleted => new("Quest_Completed");
    public static EventType LoreDiscovered => new("Lore_Discovered");
    // ... etc

    public EventType
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Value))
                throw new ArgumentException("EventType cannot be empty");
        }
    }
}
```

#### 5. **QuestState** (Enum as Value Object)
```csharp
public enum QuestState
{
    Discovered = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Abandoned = 4
}
```

#### 6. **LoreTier** (Enum as Value Object)
```csharp
public enum LoreTier
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Legendary = 3
}
```

#### 7. **ReputationScore**
```csharp
public readonly record struct ReputationScore(int Value)
{
    public const int MinReputation = -100;
    public const int MaxReputation = 100;

    public ReputationScore
    {
        get
        {
            if (Value < MinReputation || Value > MaxReputation)
                throw new ArgumentException($"Reputation must be between {MinReputation} and {MaxReputation}");
        }
    }
}
```

#### 8. **FactionId**
```csharp
public readonly record struct FactionId(string Value)
{
    public FactionId
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Value))
                throw new ArgumentException("FactionId cannot be empty");
        }
    }
}
```

#### 9. **Keyword**
```csharp
public readonly record struct Keyword(string Value)
{
    public Keyword
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Value))
                throw new ArgumentException("Keyword cannot be empty");
            if (Value.Length > 50)
                throw new ArgumentException("Keyword cannot exceed 50 characters");
        }
    }

    public bool Matches(string searchTerm) =>
        Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}
```

---

### Domain Events

#### Base Event
```csharp
public abstract record CodexDomainEvent(
    CharacterId CharacterId,
    DateTime OccurredAt
);
```

#### Specific Events
```csharp
public sealed record QuestStartedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId,
    string QuestName,
    string Description
) : CodexDomainEvent(CharacterId, OccurredAt);

public sealed record QuestCompletedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId
) : CodexDomainEvent(CharacterId, OccurredAt);

public sealed record LoreDiscoveredEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    LoreId LoreId,
    string Title,
    string Summary,
    string Source,
    LoreTier Tier,
    IReadOnlyList<Keyword> Keywords
) : CodexDomainEvent(CharacterId, OccurredAt);

public sealed record ReputationChangedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    FactionId FactionId,
    int Delta,
    string Reason
) : CodexDomainEvent(CharacterId, OccurredAt);

public sealed record TraitAcquiredEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    string TraitTag,
    string AcquisitionMethod
) : CodexDomainEvent(CharacterId, OccurredAt);

public sealed record NoteAddedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    Guid NoteId,
    string Content,
    NoteCategory Category,
    bool IsDmNote,
    bool IsPrivate
) : CodexDomainEvent(CharacterId, OccurredAt);
```

---

## Refined Specifications

### 1. Domain Model Specifications

#### Spec: PlayerCodex Aggregate
- **MUST** enforce all invariants on construction and mutation
- **MUST** raise domain events for all state changes
- **MUST** prevent direct mutation of pages (encapsulation)
- **MUST** provide methods for adding entries (not direct collection access)
- **MUST** validate entries before adding to pages

**Methods:**
```csharp
public sealed class PlayerCodex // Aggregate Root
{
    public CharacterId CharacterId { get; }
    public DateTime LastUpdated { get; private set; }

    private readonly CodexQuestPage _quests = new();
    private readonly CodexLorePage _lore = new();
    private readonly CodexEventPage _events = new();
    private readonly CodexReputationPage _reputation = new();
    private readonly CodexTraitsPage _traits = new();
    private readonly CodexNotesPage _notes = new();

    // Queries (read-only access)
    public IEnumerable<CodexQuestEntry> GetQuests() => _quests.GetAll();
    public IEnumerable<CodexLoreEntry> GetLore() => _lore.GetAll();
    public CodexQuestEntry? FindQuest(QuestId questId) => _quests.Find(questId);
    public IEnumerable<ICodexEntry> Search(Keyword keyword);

    // Commands (state modification)
    public void RecordQuestStarted(QuestId questId, string name, string description);
    public void RecordQuestCompleted(QuestId questId);
    public void RecordLoreDiscovered(LoreId loreId, string title, string summary, ...);
    public void RecordReputationChange(FactionId factionId, int delta, string reason);
    public void RecordTraitAcquired(string traitTag, string method);
    public Guid AddNote(string content, NoteCategory category);
    public void UpdateNote(Guid noteId, string newContent);

    // Internal
    private void UpdateTimestamp() => LastUpdated = DateTime.UtcNow;
}
```

**Edge Cases:**
- RecordQuestStarted with duplicate QuestId → idempotent (no-op or update metadata)
- RecordQuestCompleted for non-existent quest → create quest in Completed state
- RecordLoreDiscovered with empty keywords → valid (searchable by title/summary only)
- RecordReputationChange with delta=0 → no-op (idempotent)
- AddNote with empty content → throws ArgumentException
- UpdateNote for non-existent note → throws InvalidOperationException

---

#### Spec: Value Objects
- **MUST** be immutable (readonly record struct or record)
- **MUST** validate on construction (throw ArgumentException for invalid)
- **MUST** implement structural equality
- **MUST** have no identity (equality based on value)
- **MUST** be side-effect free

**Edge Cases:**
- CharacterId.From(Guid.Empty) → throws ArgumentException
- QuestId with null/empty string → throws ArgumentException
- ReputationScore(-101) → throws ArgumentException
- ReputationScore(101) → throws ArgumentException
- Keyword with 51+ characters → throws ArgumentException
- Keyword with null/empty → throws ArgumentException

---

#### Spec: Domain Events
- **MUST** be immutable records
- **MUST** include CharacterId and OccurredAt
- **MUST** use value objects (not primitives)
- **MUST** capture all information needed to reconstitute state change
- **SHOULD** have past-tense names (QuestCompleted, not CompleteQuest)

**Edge Cases:**
- Event with OccurredAt in future → corrected to DateTime.UtcNow
- Event with null value objects → throws ArgumentNullException
- Event deserialized with missing properties → throws or uses defaults

---

### 2. Application Layer Specifications

#### Service: CodexEventProcessor (Application Service)
**Responsibilities:**
- Receive events from NWN adapter via channel
- Load appropriate PlayerCodex aggregate
- Apply event to aggregate
- Save aggregate via repository
- Handle failures and retries

```csharp
public sealed class CodexEventProcessor
{
    private readonly ChannelReader<CodexDomainEvent> _eventChannel;
    private readonly IPlayerCodexRepository _repository;
    private readonly ILogger<CodexEventProcessor> _logger;

    public async Task ProcessEventsAsync(CancellationToken ct)
    {
        await foreach (var domainEvent in _eventChannel.ReadAllAsync(ct))
        {
            try
            {
                var codex = await _repository.LoadAsync(domainEvent.CharacterId)
                    ?? PlayerCodex.Create(domainEvent.CharacterId);

                ApplyEvent(domainEvent, codex);

                await _repository.SaveAsync(codex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process event {EventType} for {CharacterId}",
                    domainEvent.GetType().Name, domainEvent.CharacterId);
            }
        }
    }

    private void ApplyEvent(CodexDomainEvent domainEvent, PlayerCodex codex)
    {
        switch (domainEvent)
        {
            case QuestStartedEvent e:
                codex.RecordQuestStarted(e.QuestId, e.QuestName, e.Description);
                break;
            case QuestCompletedEvent e:
                codex.RecordQuestCompleted(e.QuestId);
                break;
            // ... etc
        }
    }
}
```

**Specifications:**
- **MUST** process events sequentially per character (no race conditions)
- **MUST** log all processing failures
- **MUST** not crash on handler exceptions
- **MUST** continue processing remaining events on failure
- **SHOULD** support graceful shutdown (CancellationToken)

**Edge Cases:**
- Event for non-existent codex → creates new codex
- Multiple events for same character in queue → processed in order
- Save fails → logged, event lost (or retry logic added)
- Channel closed during processing → graceful shutdown

---

#### Service: CodexQueryService (Application Service)
**Responsibilities:**
- Provide read-only queries over codex data
- Implement search and filtering
- Return DTOs (not domain objects)

```csharp
public sealed class CodexQueryService
{
    private readonly IPlayerCodexRepository _repository;

    public async Task<PlayerCodexDto?> GetCodexAsync(CharacterId characterId);
    public async Task<IEnumerable<QuestDto>> GetQuestsAsync(CharacterId characterId, QuestState? state = null);
    public async Task<IEnumerable<LoreDto>> SearchLoreAsync(CharacterId characterId, string searchTerm);
    public async Task<ReputationDto?> GetReputationAsync(CharacterId characterId, FactionId factionId);
}
```

**Specifications:**
- **MUST** return DTOs, not domain objects
- **MUST** handle non-existent codex gracefully (return null or empty)
- **SHOULD** support pagination for large result sets
- **SHOULD** cache frequently accessed data

---

### 3. Infrastructure Layer Specifications

#### Repository: IPlayerCodexRepository
```csharp
public interface IPlayerCodexRepository
{
    Task<PlayerCodex?> LoadAsync(CharacterId characterId);
    Task SaveAsync(PlayerCodex codex);
}
```

**Specifications:**
- **MUST** load entire aggregate atomically
- **MUST** save entire aggregate atomically
- **MUST** use optimistic concurrency (version/timestamp)
- **MUST** handle concurrency conflicts gracefully
- **SHOULD** use write-behind caching for performance

**Edge Cases:**
- LoadAsync for non-existent → returns null
- SaveAsync with stale version → throws ConcurrencyException
- SaveAsync during concurrent save → last write wins or throws
- Repository unavailable → throws, retry at application layer

---

#### Implementation: JsonPlayerCodexRepository
```csharp
public sealed class JsonPlayerCodexRepository : IPlayerCodexRepository
{
    private readonly string _dataDirectory;
    private readonly JsonSerializerOptions _options;

    public async Task<PlayerCodex?> LoadAsync(CharacterId characterId);
    public async Task SaveAsync(PlayerCodex codex);

    private string GetFilePath(CharacterId id) =>
        Path.Combine(_dataDirectory, $"{id.Value:N}.codex.json");
}
```

**Specifications:**
- **MUST** save to temp file, then atomic rename
- **MUST** preserve existing file on save failure
- **MUST** handle file locking (another process reading)
- **MUST** deserialize with forward/backward compatibility

**Edge Cases:**
- DataDirectory doesn't exist → create on first save
- File is locked by another process → retry or throw
- Corrupted JSON → throw JsonException, log error
- Missing properties in JSON → use defaults
- Extra properties in JSON → ignore (forward compat)

---

### 4. Anti-Corruption Layer (NWN Adapter)

#### NwnCodexEventAdapter
**Responsibilities:**
- Translate NWN script events into domain events
- Publish domain events to channel
- Isolate NWN primitives from domain

```csharp
public sealed class NwnCodexEventAdapter
{
    private readonly ChannelWriter<CodexDomainEvent> _eventWriter;

    // Called from NWN scripts
    public void OnQuestStarted(uint nwnCreature, string questId, string questName, string description)
    {
        var characterId = CharacterId.From(GetCharacterIdFromCreature(nwnCreature));

        var domainEvent = new QuestStartedEvent(
            characterId,
            DateTime.UtcNow,
            new QuestId(questId),
            questName,
            description
        );

        _eventWriter.TryWrite(domainEvent); // Fire and forget
    }

    public void OnQuestCompleted(uint nwnCreature, string questId) { /* ... */ }
    public void OnLoreDiscovered(uint nwnCreature, string loreId, /* ... */) { /* ... */ }
    // ... etc
}
```

**Specifications:**
- **MUST** convert NWN types (uint creature, string IDs) to domain types
- **MUST** validate input before creating domain events
- **MUST** log when channel write fails (back pressure)
- **MUST NOT** throw exceptions (NWN is synchronous, can't handle async exceptions)
- **MUST** be callable from synchronous NWN context

**Edge Cases:**
- Invalid nwnCreature → logged, event not published
- Channel full (back pressure) → logged, event dropped (or queued)
- Invalid questId/loreId → logged, event not published
- Called during server shutdown → graceful (channel closed)

---

### 5. Concurrency Model

#### Event Processing via Channels
```csharp
// Setup (at startup)
var channel = Channel.CreateBounded<CodexDomainEvent>(new BoundedChannelOptions(1000)
{
    FullMode = BoundedChannelFullMode.Wait // Block NWN thread if overwhelmed
});

var processor = new CodexEventProcessor(channel.Reader, repository, logger);

// Background processing
_ = Task.Run(() => processor.ProcessEventsAsync(cancellationToken));

// NWN adapter writes to channel (synchronous from NWN's perspective)
var adapter = new NwnCodexEventAdapter(channel.Writer);
```

**Specifications:**
- **MUST** use bounded channel (prevent unbounded memory growth)
- **MUST** process events sequentially per character (order matters)
- **SHOULD** use BoundedChannelFullMode.Wait to apply back pressure
- **SHOULD** support graceful shutdown (drain channel on shutdown)

**Concurrency Requirements (Revised):**
- NWN thread writes to channel synchronously → non-blocking (TryWrite) or blocking (WriteAsync)
- Background task processes events asynchronously → one event at a time per character
- Multiple characters can process concurrently → separate tasks or partitioned channel
- No concurrent writes to same PlayerCodex → enforced by sequential processing

**Edge Cases:**
- Channel full → NWN thread waits (back pressure) or event dropped
- Processing slower than event rate → channel fills, back pressure applied
- Server shutdown during processing → drain remaining events or discard

---

### 6. Testing Specifications

#### Unit Tests (Domain Layer)
```csharp
[TestFixture]
public class PlayerCodexTests
{
    [Test]
    public void RecordQuestStarted_CreatesNewQuestEntry()
    {
        // Arrange
        var codex = PlayerCodex.Create(CharacterId.New());
        var questId = new QuestId("Q001");

        // Act
        codex.RecordQuestStarted(questId, "Test Quest", "Description");

        // Assert
        var quest = codex.FindQuest(questId);
        Assert.That(quest, Is.Not.Null);
        Assert.That(quest.State, Is.EqualTo(QuestState.InProgress));
    }

    [Test]
    public void RecordQuestStarted_WithDuplicateId_IsIdempotent()
    {
        // Arrange
        var codex = PlayerCodex.Create(CharacterId.New());
        var questId = new QuestId("Q001");
        codex.RecordQuestStarted(questId, "Test Quest", "Description");

        // Act
        codex.RecordQuestStarted(questId, "Test Quest", "Description");

        // Assert
        Assert.That(codex.GetQuests().Count(), Is.EqualTo(1));
    }

    [Test]
    public void RecordReputationChange_UpdatesCorrectly()
    {
        // Arrange
        var codex = PlayerCodex.Create(CharacterId.New());
        var factionId = new FactionId("Thay");

        // Act
        codex.RecordReputationChange(factionId, 10, "Helped wizard");
        codex.RecordReputationChange(factionId, -5, "Offended noble");

        // Assert
        var reputation = codex.GetReputation(factionId);
        Assert.That(reputation.CurrentScore.Value, Is.EqualTo(5)); // 0 + 10 - 5
    }
}
```

#### Unit Tests (Value Objects)
```csharp
[TestFixture]
public class CharacterIdTests
{
    [Test]
    public void From_WithEmptyGuid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CharacterId.From(Guid.Empty));
    }

    [Test]
    public void StructuralEquality_WithSameValue_AreEqual()
    {
        var id1 = CharacterId.From(Guid.Parse("..."));
        var id2 = CharacterId.From(Guid.Parse("..."));

        Assert.That(id1, Is.EqualTo(id2));
    }
}

[TestFixture]
public class ReputationScoreTests
{
    [Test]
    public void Constructor_WithValueAboveMax_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ReputationScore(101));
    }

    [Test]
    public void Constructor_WithValueBelowMin_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ReputationScore(-101));
    }
}
```

#### Integration Tests (Application + Infrastructure)
```csharp
[TestFixture]
public class CodexEventProcessorIntegrationTests
{
    [Test]
    public async Task ProcessEvents_SavesCodexCorrectly()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<CodexDomainEvent>();
        var repository = new InMemoryPlayerCodexRepository();
        var processor = new CodexEventProcessor(channel.Reader, repository, NullLogger.Instance);

        var characterId = CharacterId.New();
        var questEvent = new QuestStartedEvent(characterId, DateTime.UtcNow,
            new QuestId("Q001"), "Test Quest", "Description");

        // Act
        await channel.Writer.WriteAsync(questEvent);
        channel.Writer.Complete();

        await processor.ProcessEventsAsync(CancellationToken.None);

        // Assert
        var codex = await repository.LoadAsync(characterId);
        Assert.That(codex, Is.Not.Null);
        Assert.That(codex.GetQuests().Count(), Is.EqualTo(1));
    }
}
```

#### Test Doubles
```csharp
public sealed class InMemoryPlayerCodexRepository : IPlayerCodexRepository
{
    private readonly Dictionary<CharacterId, PlayerCodex> _store = new();

    public Task<PlayerCodex?> LoadAsync(CharacterId characterId) =>
        Task.FromResult(_store.TryGetValue(characterId, out var codex) ? codex : null);

    public Task SaveAsync(PlayerCodex codex)
    {
        _store[codex.CharacterId] = codex;
        return Task.CompletedTask;
    }
}
```

---

## Summary of Changes

### Removed Requirements
- ❌ Web front-end security (XSS, CSRF)
- ❌ Complex concurrent write scenarios (channels provide sequential processing)
- ❌ Client-side validation
- ❌ Real-time concurrent access to same codex from multiple sources

### Added Requirements
- ✅ Strong typed value objects (no primitive obsession)
- ✅ Domain events for all state changes
- ✅ Aggregate boundaries and invariants
- ✅ Anti-corruption layer at NWN boundary
- ✅ Channel-based event processing (queue + background worker)
- ✅ Sequential event processing per character
- ✅ Back pressure handling when event rate exceeds processing capacity

### Architecture Layers
1. **Domain Layer**: Aggregates, Entities, Value Objects, Domain Events (pure C#, no dependencies)
2. **Application Layer**: Event processor, query service, use cases
3. **Infrastructure Layer**: Repository implementations, JSON serialization
4. **Adapter Layer**: NWN event adapter (anti-corruption layer)

### Testability
- Domain layer: Pure unit tests (no mocks needed)
- Application layer: Integration tests with in-memory repository
- Infrastructure layer: Integration tests with file system
- Adapter layer: Unit tests with mock channel writer