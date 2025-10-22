# Codex System - Quick Start Guide

## Current Status

✅ **Phase 1 Complete** - Value Objects, Enums, Domain Events (116 tests)
✅ **Phase 2 Complete** - Entities & Aggregate Root (186 tests)
✅ **Phase 3 Started** - Application Layer skeleton created

**Total: 422/422 tests passing**

---

## What You Have Right Now

### Working Code
- `CodexEventProcessor.cs` - Event processing skeleton with TODOs
- `CodexQueryService.cs` - Complete query service
- `IPlayerCodexRepository` - Repository interface
- All domain entities and value objects

### What's Missing
- Event helper methods (15 min to add)
- In-memory repository implementation
- Tests for application layer
- JSON persistence (optional, for later)

---

## To Get Running in 1 Hour

### 1. Check Event Definitions (5 min)

Verify these events exist in `Features/WorldEngine/Codex/Events/`:

```csharp
// QuestEvents.cs - Ensure these have all properties:
public record QuestStartedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId,
    string Title,
    string Description,
    string? QuestGiver,
    string? Location,
    IReadOnlyList<string>? Objectives,
    IReadOnlyList<Keyword>? Keywords
) : CodexDomainEvent(CharacterId, OccurredAt);

// Similar for LoreDiscoveredEvent, NoteAddedEvent
```

If properties are missing, add them.

### 2. Add Event Helpers to CodexEventProcessor (15 min)

Open `CodexEventProcessor.cs` and add after line 96:

```csharp
private CodexQuestEntry CreateQuestEntry(QuestStartedEvent evt) =>
    new CodexQuestEntry
    {
        QuestId = evt.QuestId,
        Title = evt.Title,
        Description = evt.Description,
        DateStarted = evt.OccurredAt,
        QuestGiver = evt.QuestGiver,
        Location = evt.Location,
        Objectives = evt.Objectives?.ToList() ?? new List<string>(),
        Keywords = evt.Keywords?.ToList() ?? new List<Keyword>()
    };

private CodexLoreEntry CreateLoreEntry(LoreDiscoveredEvent evt) =>
    new CodexLoreEntry
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

private CodexNoteEntry CreateNoteEntry(NoteAddedEvent evt) =>
    new CodexNoteEntry(
        id: evt.NoteId,
        content: evt.Content,
        category: evt.Category,
        dateCreated: evt.OccurredAt,
        isDmNote: evt.IsDmNote,
        isPrivate: evt.IsPrivate,
        title: evt.Title
    );
```

Then update the switch cases (lines 75-96):

```csharp
case QuestStartedEvent qse:
    codex.RecordQuestStarted(CreateQuestEntry(qse), qse.OccurredAt);
    break;

case LoreDiscoveredEvent lde:
    codex.RecordLoreDiscovered(CreateLoreEntry(lde), lde.OccurredAt);
    break;

case NoteAddedEvent nae:
    codex.AddNote(CreateNoteEntry(nae), nae.OccurredAt);
    break;
```

### 3. Create InMemory Repository (10 min)

Create `Features/WorldEngine/Codex/Infrastructure/InMemoryPlayerCodexRepository.cs`:

```csharp
using System.Collections.Concurrent;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Codex.Infrastructure;

public class InMemoryPlayerCodexRepository : IPlayerCodexRepository
{
    private readonly ConcurrentDictionary<CharacterId, PlayerCodex> _storage = new();

    public Task<PlayerCodex?> LoadAsync(CharacterId characterId, CancellationToken ct = default)
    {
        _storage.TryGetValue(characterId, out var codex);
        return Task.FromResult(codex);
    }

    public Task SaveAsync(PlayerCodex codex, CancellationToken ct = default)
    {
        _storage[codex.OwnerId] = codex;
        return Task.CompletedTask;
    }

    public void Clear() => _storage.Clear();
}
```

### 4. Create Basic Test (20 min)

Create `Tests/Systems/Codex/Application/CodexEventProcessorTests.cs`:

```csharp
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
        _repository.Clear();
    }

    [Test]
    public async Task ProcessReputationEvent_CreatesReputation()
    {
        // Arrange
        _processor.Start();
        var evt = new ReputationChangedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow,
            FactionId: new FactionId("faction_001"),
            FactionName: "The Silver Order",
            Delta: 10,
            Reason: "Helped townspeople"
        );

        // Act
        await _processor.EnqueueEventAsync(evt);
        await Task.Delay(100); // Give time to process

        // Assert
        var codex = await _repository.LoadAsync(_characterId);
        Assert.That(codex, Is.Not.Null);
        Assert.That(codex!.HasReputation(evt.FactionId), Is.True);

        var rep = codex.GetReputation(evt.FactionId);
        Assert.That(rep!.CurrentScore.Value, Is.EqualTo(10));
    }
}
```

### 5. Run Test (5 min)

```bash
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj --filter "CodexEventProcessorTests"
```

If it fails, check:
- Event definitions have all properties
- Helper methods match event properties
- Processor is started before enqueueing

### 6. Add More Tests (10 min each)

Copy the pattern above for:
- Quest started/completed
- Lore discovered
- Note added/edited

---

## Key Files Reference

| File | Purpose | Status |
|------|---------|--------|
| `Aggregates/PlayerCodex.cs` | Aggregate root | ✅ Complete |
| `Entities/*.cs` | Domain entities | ✅ Complete |
| `ValueObjects/*.cs` | Value objects | ✅ Complete |
| `Events/*.cs` | Domain events | ⚠️ Check properties |
| `Application/CodexEventProcessor.cs` | Event processing | ⏳ Add helpers |
| `Application/CodexQueryService.cs` | Queries | ✅ Complete |
| `Infrastructure/InMemory*.cs` | Test repo | ⏳ Create |

---

## Common Commands

```bash
# Build
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj

# Run all tests
dotnet test AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj

# Run specific test class
dotnet test --filter "CodexEventProcessorTests"

# Run specific test
dotnet test --filter "ProcessReputationEvent_CreatesReputation"

# Watch mode (auto-rerun on file change)
dotnet watch test --project AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj
```

---

## Where to Get Help

1. **Phase 3 Implementation Guide** - Detailed steps with examples
2. **Phase 2 Complete Doc** - Entity/aggregate reference
3. **Test files in Tests/Systems/Codex/** - Working test patterns
4. **Existing WorldEngine tests** - Integration examples

---

## Success Criteria

You know it's working when:
- ✅ Can enqueue a ReputationChangedEvent and query it back
- ✅ Can start a quest and mark it completed
- ✅ Multiple events for same character work
- ✅ Events for different characters stay separate

---

## After Phase 3 Works

Then you can:
1. Build JSON repository for persistence
2. Wire up to NWN events
3. Build UI to display codex
4. Add more complex queries (pagination, sorting, etc.)

But first: Get the basic event flow working!

---

## Estimated Timeline

- ✅ Phase 1: Done (116 tests)
- ✅ Phase 2: Done (186 tests)
- ⏳ Phase 3: 1 hour to basic working, 2-3 hours to complete
- ⏳ Phase 4: 2-4 hours (persistence + NWN integration)

**You're 70% done!** Just need to connect the pieces.
