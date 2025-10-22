# Phase 3: Application Layer - Implementation Guide

## What's Already Done ✅

I've created the skeleton for Phase 3 with working structure and clear TODOs:

1. **CodexEventProcessor.cs** - Event processing with channels
2. **CodexQueryService.cs** - Read-only query service
3. **IPlayerCodexRepository** - Repository interface (in CodexEventProcessor.cs)

---

## Quick Wins: What To Do Next

### Step 1: Implement Event Creation Helpers (15 minutes)

Create helper methods in `ApplyEvent()` to convert events to entities:

```csharp
// In CodexEventProcessor.cs, add these private helper methods:

private CodexQuestEntry CreateQuestEntry(QuestStartedEvent evt)
{
    return new CodexQuestEntry
    {
        QuestId = evt.QuestId,
        Title = evt.Title,
        Description = evt.Description,
        State = QuestState.InProgress,  // Or Discovered based on your needs
        DateStarted = evt.OccurredAt,
        QuestGiver = evt.QuestGiver,
        Location = evt.Location,
        Objectives = evt.Objectives?.ToList() ?? new List<string>(),
        Keywords = evt.Keywords?.ToList() ?? new List<Keyword>()
    };
}

private CodexLoreEntry CreateLoreEntry(LoreDiscoveredEvent evt)
{
    return new CodexLoreEntry
    {
        LoreId = evt.LoreId,
        Title = evt.Title,
        Content = evt.Content,
        Category = evt.Category,
        Tier = evt.Tier,
        DateDiscovered = evt.OccurredAt,
        DiscoveryLocation = evt.Location,
        Keywords = evt.Keywords.ToList()
    };
}

private CodexNoteEntry CreateNoteEntry(NoteAddedEvent evt)
{
    return new CodexNoteEntry(
        id: evt.NoteId,
        content: evt.Content,
        category: evt.Category,
        dateCreated: evt.OccurredAt,
        isDmNote: evt.IsDmNote,
        isPrivate: evt.IsPrivate,
        title: evt.Title
    );
}
```

Then update the `ApplyEvent()` switch cases:

```csharp
case QuestStartedEvent qse:
    var questEntry = CreateQuestEntry(qse);
    codex.RecordQuestStarted(questEntry, qse.OccurredAt);
    break;

case LoreDiscoveredEvent lde:
    var loreEntry = CreateLoreEntry(lde);
    codex.RecordLoreDiscovered(loreEntry, lde.OccurredAt);
    break;

case NoteAddedEvent nae:
    var noteEntry = CreateNoteEntry(nae);
    codex.AddNote(noteEntry, nae.OccurredAt);
    break;
```

**Testing Tip:** Write tests that verify events are correctly converted to entities.

---

### Step 2: Create In-Memory Repository (10 minutes)

Quick implementation for testing before you build the real persistence:

```csharp
// Create: Features/WorldEngine/Codex/Infrastructure/InMemoryPlayerCodexRepository.cs

using System.Collections.Concurrent;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Codex.Infrastructure;

/// <summary>
/// In-memory implementation for testing. NOT for production.
/// </summary>
public class InMemoryPlayerCodexRepository : IPlayerCodexRepository
{
    private readonly ConcurrentDictionary<CharacterId, PlayerCodex> _storage = new();

    public Task<PlayerCodex?> LoadAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(characterId, out var codex);
        return Task.FromResult(codex);
    }

    public Task SaveAsync(PlayerCodex codex, CancellationToken cancellationToken = default)
    {
        _storage[codex.OwnerId] = codex;
        return Task.CompletedTask;
    }

    // Helper for testing
    public void Clear() => _storage.Clear();
    public int Count => _storage.Count;
}
```

---

### Step 3: Write Basic Tests (20 minutes)

Create test skeleton with TODOs for you to fill in:

```csharp
// Create: Tests/Systems/Codex/Application/CodexEventProcessorTests.cs

using AmiaReforged.PwEngine.Features.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Infrastructure;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.Codex.Application;

[TestFixture]
public class CodexEventProcessorTests
{
    private InMemoryPlayerCodexRepository _repository;
    private CodexEventProcessor _processor;
    private CharacterId _characterId;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryPlayerCodexRepository();
        _processor = new CodexEventProcessor(_repository);
        _characterId = CharacterId.New();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _processor.StopAsync();
    }

    [Test]
    public async Task ProcessQuestStartedEvent_CreatesQuestInCodex()
    {
        // Arrange
        _processor.Start();
        var evt = new QuestStartedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow,
            QuestId: new QuestId("quest_001"),
            Title: "Test Quest",
            Description: "Test Description",
            QuestGiver: "NPC",
            Location: "Town",
            Objectives: new List<string> { "Objective 1" },
            Keywords: new List<Keyword> { new Keyword("test") }
        );

        // Act
        await _processor.EnqueueEventAsync(evt);
        await Task.Delay(100); // Give processor time to work

        // Assert
        var codex = await _repository.LoadAsync(_characterId);
        Assert.That(codex, Is.Not.Null);
        Assert.That(codex!.HasQuest(evt.QuestId), Is.True);

        var quest = codex.GetQuest(evt.QuestId);
        Assert.That(quest!.Title, Is.EqualTo("Test Quest"));
    }

    [Test]
    public async Task ProcessReputationChangedEvent_UpdatesReputation()
    {
        // TODO: Test reputation changes
        // 1. Send ReputationChangedEvent
        // 2. Verify FactionReputation is created/updated
        // 3. Check score and history
        Assert.Pass("TODO: Implement this test");
    }

    [Test]
    public async Task ProcessMultipleEvents_MaintainsConsistency()
    {
        // TODO: Test multiple events for same character
        // 1. Send QuestStarted
        // 2. Send QuestCompleted
        // 3. Verify quest state is Completed
        Assert.Pass("TODO: Implement this test");
    }

    [Test]
    public async Task ProcessEvents_ForDifferentCharacters_KeepsSeparate()
    {
        // TODO: Test events for different characters
        // 1. Send events for character A
        // 2. Send events for character B
        // 3. Verify each has separate codex
        Assert.Pass("TODO: Implement this test");
    }
}
```

```csharp
// Create: Tests/Systems/Codex/Application/CodexQueryServiceTests.cs

using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Infrastructure;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.Codex.Application;

[TestFixture]
public class CodexQueryServiceTests
{
    private InMemoryPlayerCodexRepository _repository;
    private CodexQueryService _queryService;
    private CharacterId _characterId;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryPlayerCodexRepository();
        _queryService = new CodexQueryService(_repository);
        _characterId = CharacterId.New();
    }

    [Test]
    public async Task GetAllQuests_WithNoCodex_ReturnsEmptyList()
    {
        // Act
        var quests = await _queryService.GetAllQuestsAsync(_characterId);

        // Assert
        Assert.That(quests, Is.Empty);
    }

    [Test]
    public async Task GetAllQuests_WithQuests_ReturnsAllQuests()
    {
        // Arrange - Create codex with quests
        var codex = new PlayerCodex(_characterId, DateTime.UtcNow);
        var quest1 = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_001"),
            Title = "Quest 1",
            Description = "Description",
            DateStarted = DateTime.UtcNow
        };
        var quest2 = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_002"),
            Title = "Quest 2",
            Description = "Description",
            DateStarted = DateTime.UtcNow
        };
        codex.RecordQuestStarted(quest1, DateTime.UtcNow);
        codex.RecordQuestStarted(quest2, DateTime.UtcNow);
        await _repository.SaveAsync(codex);

        // Act
        var quests = await _queryService.GetAllQuestsAsync(_characterId);

        // Assert
        Assert.That(quests, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task SearchQuests_FindsMatchingQuests()
    {
        // TODO: Test quest searching
        // 1. Create codex with multiple quests
        // 2. Search for specific term
        // 3. Verify only matching quests returned
        Assert.Pass("TODO: Implement this test");
    }

    [Test]
    public async Task GetStatistics_ReturnsCorrectCounts()
    {
        // TODO: Test statistics gathering
        // 1. Create codex with various entries
        // 2. Get statistics
        // 3. Verify all counts are correct
        Assert.Pass("TODO: Implement this test");
    }

    [Test]
    public async Task GetQuestsByState_FiltersCorrectly()
    {
        // TODO: Test state filtering
        // 1. Create completed and active quests
        // 2. Query for completed only
        // 3. Verify only completed returned
        Assert.Pass("TODO: Implement this test");
    }
}
```

**To get tests passing:**
1. Complete the TODOs in the helper methods (Step 1)
2. Run the tests one at a time
3. Fix any compilation errors
4. Uncomment `Assert.Pass()` and add real assertions

---

## Future Steps (Not Urgent)

### Step 4: JSON Repository (Later)

When you need persistence:

```csharp
// Create: Features/WorldEngine/Codex/Infrastructure/JsonPlayerCodexRepository.cs

public class JsonPlayerCodexRepository : IPlayerCodexRepository
{
    private readonly string _basePath;

    public JsonPlayerCodexRepository(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(basePath);
    }

    public async Task<PlayerCodex?> LoadAsync(CharacterId characterId, CancellationToken ct)
    {
        var path = GetPath(characterId);
        if (!File.Exists(path)) return null;

        var json = await File.ReadAllTextAsync(path, ct);
        // TODO: Deserialize with System.Text.Json
        // Note: You'll need custom converters for value objects
        return JsonSerializer.Deserialize<PlayerCodex>(json);
    }

    public async Task SaveAsync(PlayerCodex codex, CancellationToken ct)
    {
        var path = GetPath(codex.OwnerId);
        var json = JsonSerializer.Serialize(codex, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Atomic write with temp file
        var tempPath = path + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, ct);
        File.Move(tempPath, path, overwrite: true);
    }

    private string GetPath(CharacterId id) =>
        Path.Combine(_basePath, $"codex_{id.Value}.json");
}
```

**Challenge:** You'll need JSON converters for value objects. Consider:
- Write converters for `CharacterId`, `QuestId`, `LoreId`, etc.
- Or use `[JsonConstructor]` attributes
- Or serialize to intermediate DTOs

---

### Step 5: Per-Character Sequential Processing (Later)

The current processor is simple sequential. For production, you want per-character channels:

```csharp
// Advanced version - channels per character

private readonly ConcurrentDictionary<CharacterId, Channel<CodexDomainEvent>> _characterChannels = new();

public async Task EnqueueEventAsync(CodexDomainEvent domainEvent, CancellationToken ct)
{
    var channel = _characterChannels.GetOrAdd(
        domainEvent.CharacterId,
        _ => Channel.CreateUnbounded<CodexDomainEvent>()
    );

    await channel.Writer.WriteAsync(domainEvent, ct);
}

// Start a processing task per character channel
// This ensures events for Character A don't block Character B
```

---

## Testing Strategy

### Unit Tests (Do These First)
- ✅ Test event processing in isolation
- ✅ Test query service with pre-populated data
- ✅ Mock repository if needed

### Integration Tests (Do Later)
- Test full event flow (enqueue → process → save → query)
- Test with real JSON repository
- Test concurrency with multiple characters

### Sample Test Pattern

```csharp
[Test]
public async Task FullFlow_QuestLifecycle()
{
    // Arrange
    var repository = new InMemoryPlayerCodexRepository();
    var processor = new CodexEventProcessor(repository);
    var queryService = new CodexQueryService(repository);
    processor.Start();

    var charId = CharacterId.New();
    var questId = new QuestId("quest_001");

    // Act - Start quest
    await processor.EnqueueEventAsync(new QuestStartedEvent(
        CharacterId: charId,
        OccurredAt: DateTime.UtcNow,
        QuestId: questId,
        Title: "Epic Quest",
        Description: "Do epic things",
        QuestGiver: null,
        Location: null,
        Objectives: null,
        Keywords: null
    ));
    await Task.Delay(100);

    // Assert - Quest exists
    var quests = await queryService.GetAllQuestsAsync(charId);
    Assert.That(quests, Has.Count.EqualTo(1));
    Assert.That(quests[0].State, Is.EqualTo(QuestState.InProgress));

    // Act - Complete quest
    await processor.EnqueueEventAsync(new QuestCompletedEvent(
        CharacterId: charId,
        OccurredAt: DateTime.UtcNow,
        QuestId: questId
    ));
    await Task.Delay(100);

    // Assert - Quest completed
    var completedQuests = await queryService.GetQuestsByStateAsync(
        charId, QuestState.Completed);
    Assert.That(completedQuests, Has.Count.EqualTo(1));

    await processor.StopAsync();
}
```

---

## Common Issues & Solutions

### Issue 1: Events not processing
**Symptom:** Tests timeout, codex never updated
**Solution:** Make sure you called `processor.Start()` and added delays for async processing

### Issue 2: Compilation errors on event properties
**Symptom:** Event properties not found
**Solution:** Check your event definitions in `Codex/Events/` - make sure they have all needed properties

### Issue 3: Repository returns null unexpectedly
**Symptom:** Codex is null in tests
**Solution:** Check that events have the same `CharacterId` you're querying with

### Issue 4: Tests interfere with each other
**Symptom:** Tests pass individually but fail in batch
**Solution:** Use `[TearDown]` to stop processor and clear repository

---

## Quick Reference: Key TODOs in Code

### In CodexEventProcessor.cs:
1. ✅ Line 75-96: Implement helper methods to create entities from events
2. Line 60: Add real logging instead of Console.WriteLine
3. Line 46: Implement per-character sequential processing (future)

### In Event Definitions (if not already done):
Ensure all events have necessary properties:
- `QuestStartedEvent` needs: Title, Description, QuestGiver, Location, Objectives, Keywords
- `LoreDiscoveredEvent` needs: Title, Content, Category, Tier, Location, Keywords
- `NoteAddedEvent` needs: NoteId, Content, Category, IsDmNote, IsPrivate, Title

### Test Files:
1. Complete TODOs in CodexEventProcessorTests.cs (4 tests marked)
2. Complete TODOs in CodexQueryServiceTests.cs (3 tests marked)

---

## Success Metrics

You'll know Phase 3 is working when:
- ✅ You can enqueue events and see them in the codex
- ✅ Query service returns expected data
- ✅ Multiple events process correctly
- ✅ Different characters have separate codexes
- ✅ All tests pass

---

## Time Estimates

| Task | Time | Priority |
|------|------|----------|
| Step 1: Event helpers | 15 min | HIGH |
| Step 2: InMemory repo | 10 min | HIGH |
| Step 3: Basic tests | 20 min | HIGH |
| Fix event definitions | 10 min | HIGH |
| Complete test TODOs | 30 min | MEDIUM |
| JSON repository | 1 hour | LOW |
| Advanced concurrency | 1 hour | LOW |

**Total for working system: ~1 hour**
**Total for production-ready: ~3 hours**

---

## Next Session Checklist

When you come back to this:

1. ✅ Verify event definitions have all properties (check Events/*.cs)
2. ✅ Complete Step 1 helper methods
3. ✅ Create InMemoryPlayerCodexRepository (Step 2)
4. ✅ Create test files (Step 3)
5. ✅ Run tests and fix compilation errors
6. ✅ Complete test TODOs one by one
7. ⏳ JSON repository (only when needed)

Good luck! The foundation is solid - you just need to connect the dots.
