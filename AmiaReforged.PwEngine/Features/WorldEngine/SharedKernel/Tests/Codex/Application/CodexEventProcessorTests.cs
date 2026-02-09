using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Infrastructure;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Application;

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

    #region Quest Event Tests

    [Test]
    public async Task Given_QuestStartedEvent_When_Processed_Then_QuestAddedToCodex()
    {
        // Given
        QuestId questId = QuestId.NewId();
        QuestStartedEvent evt = new QuestStartedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow,
            QuestId: questId,
            QuestName: "The Lost Artifact",
            Description: "Find the ancient artifact in the ruins"
        );

        // When
        _processor.Start();
        await _processor.EnqueueEventAsync(evt);
        await Task.Delay(100); // Give processor time to process
        await _processor.StopAsync();

        // Then
        PlayerCodex? codex = await _repository.LoadAsync(_characterId);
        Assert.That(codex, Is.Not.Null);
        Assert.That(codex!.Quests.Count, Is.EqualTo(1));

        CodexQuestEntry quest = codex.Quests.First();
        Assert.That(quest.QuestId, Is.EqualTo(questId));
        Assert.That(quest.Title, Is.EqualTo("The Lost Artifact"));
        Assert.That(quest.Description, Is.EqualTo("Find the ancient artifact in the ruins"));
        Assert.That(quest.State, Is.EqualTo(QuestState.InProgress));
    }

    [Test]
    public async Task Given_QuestCompletedEvent_When_Processed_Then_QuestMarkedCompleted()
    {
        // Given
        QuestId questId = QuestId.NewId();
        QuestStartedEvent startEvent = new QuestStartedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow,
            QuestId: questId,
            QuestName: "Test Quest",
            Description: "Test Description"
        );
        QuestCompletedEvent completeEvent = new QuestCompletedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow.AddHours(1),
            QuestId: questId
        );

        // When
        _processor.Start();
        await _processor.EnqueueEventAsync(startEvent);
        await _processor.EnqueueEventAsync(completeEvent);
        await Task.Delay(100);
        await _processor.StopAsync();

        // Then
        PlayerCodex? codex = await _repository.LoadAsync(_characterId);
        CodexQuestEntry? quest = codex!.GetQuest(questId);
        Assert.That(quest, Is.Not.Null);
        Assert.That(quest!.State, Is.EqualTo(QuestState.Completed));
        Assert.That(quest.DateCompleted, Is.Not.Null);
    }

    [Test]
    public async Task Given_QuestFailedEvent_When_Processed_Then_QuestMarkedFailed()
    {
        // Given
        QuestId questId = QuestId.NewId();
        QuestStartedEvent startEvent = new QuestStartedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow,
            QuestId: questId,
            QuestName: "Test Quest",
            Description: "Test Description"
        );
        QuestFailedEvent failEvent = new QuestFailedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow.AddHours(1),
            QuestId: questId,
            Reason: "Time limit expired"
        );

        // When
        _processor.Start();
        await _processor.EnqueueEventAsync(startEvent);
        await _processor.EnqueueEventAsync(failEvent);
        await Task.Delay(100);
        await _processor.StopAsync();

        // Then
        PlayerCodex? codex = await _repository.LoadAsync(_characterId);
        CodexQuestEntry? quest = codex!.GetQuest(questId);
        Assert.That(quest!.State, Is.EqualTo(QuestState.Failed));
    }

    [Test]
    public async Task Given_QuestAbandonedEvent_When_Processed_Then_QuestMarkedAbandoned()
    {
        // Given
        QuestId questId = QuestId.NewId();
        QuestStartedEvent startEvent = new QuestStartedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow,
            QuestId: questId,
            QuestName: "Test Quest",
            Description: "Test Description"
        );
        QuestAbandonedEvent abandonEvent = new QuestAbandonedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow.AddHours(1),
            QuestId: questId
        );

        // When
        _processor.Start();
        await _processor.EnqueueEventAsync(startEvent);
        await _processor.EnqueueEventAsync(abandonEvent);
        await Task.Delay(100);
        await _processor.StopAsync();

        // Then
        PlayerCodex? codex = await _repository.LoadAsync(_characterId);
        CodexQuestEntry? quest = codex!.GetQuest(questId);
        Assert.That(quest!.State, Is.EqualTo(QuestState.Abandoned));
    }

    #endregion

    #region Lore Event Tests

    [Test]
    public async Task Given_LoreDiscoveredEvent_When_Processed_Then_LoreAddedToCodex()
    {
        // Given
        LoreId loreId = LoreId.NewId();
        List<Keyword> keywords = new List<Keyword> { new Keyword("ancient"), new Keyword("history") };
        LoreDiscoveredEvent evt = new LoreDiscoveredEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow,
            LoreId: loreId,
            Title: "The Fall of Netheril",
            Summary: "Ancient empire that fell due to hubris",
            Source: "Ancient Tome",
            Tier: LoreTier.Common,
            Keywords: keywords
        );

        // When
        _processor.Start();
        await _processor.EnqueueEventAsync(evt);
        await Task.Delay(100);
        await _processor.StopAsync();

        // Then
        PlayerCodex? codex = await _repository.LoadAsync(_characterId);
        Assert.That(codex!.Lore.Count, Is.EqualTo(1));

        CodexLoreEntry lore = codex.Lore.First();
        Assert.That(lore.LoreId, Is.EqualTo(loreId));
        Assert.That(lore.Title, Is.EqualTo("The Fall of Netheril"));
        Assert.That(lore.Content, Is.EqualTo("Ancient empire that fell due to hubris"));
        Assert.That(lore.Tier, Is.EqualTo(LoreTier.Common));
        Assert.That(lore.Keywords.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Given_MultipleLoreEvents_When_Processed_Then_AllLoreAdded()
    {
        // Given
        LoreDiscoveredEvent lore1 = new LoreDiscoveredEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow,
            LoreId: LoreId.NewId(),
            Title: "Lore 1",
            Summary: "Summary 1",
            Source: "Source 1",
            Tier: LoreTier.Common,
            Keywords: new List<Keyword>()
        );
        LoreDiscoveredEvent lore2 = new LoreDiscoveredEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow.AddMinutes(1),
            LoreId: LoreId.NewId(),
            Title: "Lore 2",
            Summary: "Summary 2",
            Source: "Source 2",
            Tier: LoreTier.Rare,
            Keywords: new List<Keyword>()
        );

        // When
        _processor.Start();
        await _processor.EnqueueEventAsync(lore1);
        await _processor.EnqueueEventAsync(lore2);
        await Task.Delay(100);
        await _processor.StopAsync();

        // Then
        PlayerCodex? codex = await _repository.LoadAsync(_characterId);
        Assert.That(codex!.Lore.Count, Is.EqualTo(2));
    }

    #endregion

    #region Note Event Tests

    [Test]
    public async Task Given_NoteAddedEvent_When_Processed_Then_NoteAddedToCodex()
    {
        // Given
        Guid noteId = Guid.NewGuid();
        NoteAddedEvent evt = new NoteAddedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow,
            NoteId: noteId,
            Content: "Remember to check the library",
            Category: NoteCategory.General,
            IsDmNote: false,
            IsPrivate: true
        );

        // When
        _processor.Start();
        await _processor.EnqueueEventAsync(evt);
        await Task.Delay(100);
        await _processor.StopAsync();

        // Then
        PlayerCodex? codex = await _repository.LoadAsync(_characterId);
        Assert.That(codex!.Notes.Count, Is.EqualTo(1));

        CodexNoteEntry note = codex.Notes.First();
        Assert.That(note.Id, Is.EqualTo(noteId));
        Assert.That(note.Content, Is.EqualTo("Remember to check the library"));
        Assert.That(note.Category, Is.EqualTo(NoteCategory.General));
        Assert.That(note.IsPrivate, Is.True);
    }

    [Test]
    public async Task Given_NoteEditedEvent_When_Processed_Then_NoteContentUpdated()
    {
        // Given
        Guid noteId = Guid.NewGuid();
        NoteAddedEvent addEvent = new NoteAddedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow,
            NoteId: noteId,
            Content: "Original content",
            Category: NoteCategory.General,
            IsDmNote: false,
            IsPrivate: false
        );
        NoteEditedEvent editEvent = new NoteEditedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow.AddMinutes(5),
            NoteId: noteId,
            NewContent: "Updated content"
        );

        // When
        _processor.Start();
        await _processor.EnqueueEventAsync(addEvent);
        await _processor.EnqueueEventAsync(editEvent);
        await Task.Delay(100);
        await _processor.StopAsync();

        // Then
        PlayerCodex? codex = await _repository.LoadAsync(_characterId);
        CodexNoteEntry? note = codex!.GetNote(noteId);
        Assert.That(note!.Content, Is.EqualTo("Updated content"));
    }

    [Test]
    public async Task Given_NoteDeletedEvent_When_Processed_Then_NoteRemovedFromCodex()
    {
        // Given
        Guid noteId = Guid.NewGuid();
        NoteAddedEvent addEvent = new NoteAddedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow,
            NoteId: noteId,
            Content: "To be deleted",
            Category: NoteCategory.General,
            IsDmNote: false,
            IsPrivate: false
        );
        NoteDeletedEvent deleteEvent = new NoteDeletedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow.AddMinutes(5),
            NoteId: noteId
        );

        // When
        _processor.Start();
        await _processor.EnqueueEventAsync(addEvent);
        await _processor.EnqueueEventAsync(deleteEvent);
        await Task.Delay(100);
        await _processor.StopAsync();

        // Then
        PlayerCodex? codex = await _repository.LoadAsync(_characterId);
        CodexNoteEntry? note = codex!.GetNote(noteId);
        Assert.That(note, Is.Null);
    }

    #endregion

    #region Reputation Event Tests

    [Test]
    public async Task Given_ReputationChangedEvent_When_Processed_Then_ReputationUpdated()
    {
        // Given
        FactionId factionId = FactionId.NewId();
        ReputationChangedEvent evt = new ReputationChangedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow,
            FactionId: factionId,
            Delta: ReputationScore.Parse(50),
            Reason: "Completed faction quest"
        );

        // When
        _processor.Start();
        await _processor.EnqueueEventAsync(evt);
        await Task.Delay(100);
        await _processor.StopAsync();

        // Then
        PlayerCodex? codex = await _repository.LoadAsync(_characterId);
        FactionReputation? reputation = codex!.GetReputation(factionId);
        Assert.That(reputation, Is.Not.Null);
        Assert.That(reputation!.CurrentScore.Value, Is.EqualTo(50));
    }

    [Test]
    public async Task Given_MultipleReputationEvents_When_Processed_Then_ReputationAccumulates()
    {
        // Given
        FactionId factionId = FactionId.NewId();
        ReputationChangedEvent event1 = new ReputationChangedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow,
            FactionId: factionId,
            Delta: ReputationScore.Parse(25),
            Reason: "First quest"
        );
        ReputationChangedEvent event2 = new ReputationChangedEvent(
            CharacterId: _characterId,
            OccurredAt: DateTime.UtcNow.AddHours(1),
            FactionId: factionId,
            Delta: ReputationScore.Parse(30),
            Reason: "Second quest"
        );

        // When
        _processor.Start();
        await _processor.EnqueueEventAsync(event1);
        await _processor.EnqueueEventAsync(event2);
        await Task.Delay(100);
        await _processor.StopAsync();

        // Then
        PlayerCodex? codex = await _repository.LoadAsync(_characterId);
        FactionReputation? reputation = codex!.GetReputation(factionId);
        Assert.That(reputation!.CurrentScore.Value, Is.EqualTo(55));
    }

    #endregion

    #region Integration Tests

    [Test]
    public async Task Given_MixedEvents_When_Processed_Then_AllAppliedCorrectly()
    {
        // Given
        QuestId questId = QuestId.NewId();
        LoreId loreId = LoreId.NewId();
        Guid noteId = Guid.NewGuid();
        FactionId factionId = FactionId.NewId();

        List<CodexDomainEvent> events = new List<CodexDomainEvent>
        {
            new QuestStartedEvent(_characterId, DateTime.UtcNow, questId, "Quest", "Description"),
            new LoreDiscoveredEvent(_characterId, DateTime.UtcNow, loreId, "Lore", "Summary", "Source", LoreTier.Common, new List<Keyword>()),
            new NoteAddedEvent(_characterId, DateTime.UtcNow, noteId, "Note", NoteCategory.General, false, false),
            new ReputationChangedEvent(_characterId, DateTime.UtcNow, factionId, ReputationScore.Parse(10), "Test")
        };

        // When
        _processor.Start();
        foreach (CodexDomainEvent evt in events)
        {
            await _processor.EnqueueEventAsync(evt);
        }
        await Task.Delay(200);
        await _processor.StopAsync();

        // Then
        PlayerCodex? codex = await _repository.LoadAsync(_characterId);
        Assert.That(codex!.Quests.Count, Is.EqualTo(1));
        Assert.That(codex.Lore.Count, Is.EqualTo(1));
        Assert.That(codex.Notes.Count, Is.EqualTo(1));
        Assert.That(codex.Reputations.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Given_MultipleCharacters_When_EventsProcessed_Then_EachCharacterHasSeparateCodex()
    {
        // Given
        CharacterId character1 = CharacterId.New();
        CharacterId character2 = CharacterId.New();
        QuestStartedEvent quest1 = new QuestStartedEvent(character1, DateTime.UtcNow, QuestId.NewId(), "Quest1", "Desc1");
        QuestStartedEvent quest2 = new QuestStartedEvent(character2, DateTime.UtcNow, QuestId.NewId(), "Quest2", "Desc2");

        // When
        _processor.Start();
        await _processor.EnqueueEventAsync(quest1);
        await _processor.EnqueueEventAsync(quest2);
        await Task.Delay(100);
        await _processor.StopAsync();

        // Then
        PlayerCodex? codex1 = await _repository.LoadAsync(character1);
        PlayerCodex? codex2 = await _repository.LoadAsync(character2);

        Assert.That(codex1, Is.Not.Null);
        Assert.That(codex2, Is.Not.Null);
        Assert.That(codex1!.Quests.First().Title, Is.EqualTo("Quest1"));
        Assert.That(codex2!.Quests.First().Title, Is.EqualTo("Quest2"));
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public void Given_NullEvent_When_Enqueued_Then_ThrowsArgumentNullException()
    {
        // When/Then
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await _processor.EnqueueEventAsync(null!);
        });
    }

    [Test]
    public async Task Given_ProcessorNotStarted_When_EventEnqueued_Then_EventNotProcessed()
    {
        // Given
        QuestStartedEvent evt = new QuestStartedEvent(_characterId, DateTime.UtcNow, QuestId.NewId(), "Quest", "Desc");

        // When
        await _processor.EnqueueEventAsync(evt);
        await Task.Delay(100);

        // Then
        PlayerCodex? codex = await _repository.LoadAsync(_characterId);
        Assert.That(codex, Is.Null);
    }

    #endregion
}

