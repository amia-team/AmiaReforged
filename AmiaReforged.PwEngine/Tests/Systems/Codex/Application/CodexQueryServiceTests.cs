using AmiaReforged.PwEngine.Features.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Infrastructure;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.Codex.Application;

[TestFixture]
public class CodexQueryServiceTests
{
    private InMemoryPlayerCodexRepository _repository;
    private CodexQueryService _queryService;
    private CharacterId _characterId;
    private PlayerCodex _codex;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryPlayerCodexRepository();
        _queryService = new CodexQueryService(_repository);
        _characterId = CharacterId.New();
        _codex = new PlayerCodex(_characterId, DateTime.UtcNow);
    }

    [TearDown]
    public void TearDown()
    {
        _repository.Clear();
    }

    #region Quest Queries

    [Test]
    public async Task Given_CodexWithQuests_When_GetAllQuests_Then_ReturnsAllQuests()
    {
        // Given
        CodexQuestEntry quest1 = new CodexQuestEntry
        {
            QuestId = QuestId.NewId(),
            Title = "Quest 1",
            Description = "Description 1",
            DateStarted = DateTime.UtcNow
        };
        CodexQuestEntry quest2 = new CodexQuestEntry
        {
            QuestId = QuestId.NewId(),
            Title = "Quest 2",
            Description = "Description 2",
            DateStarted = DateTime.UtcNow
        };

        _codex.RecordQuestStarted(quest1, DateTime.UtcNow);
        _codex.RecordQuestStarted(quest2, DateTime.UtcNow);
        await _repository.SaveAsync(_codex);

        // When
        IReadOnlyList<CodexQuestEntry> quests = await _queryService.GetAllQuestsAsync(_characterId);

        // Then
        Assert.That(quests.Count, Is.EqualTo(2));
        Assert.That(quests.Any(q => q.Title == "Quest 1"), Is.True);
        Assert.That(quests.Any(q => q.Title == "Quest 2"), Is.True);
    }

    [Test]
    public async Task Given_NoCodex_When_GetAllQuests_Then_ReturnsEmptyList()
    {
        // When
        IReadOnlyList<CodexQuestEntry> quests = await _queryService.GetAllQuestsAsync(_characterId);

        // Then
        Assert.That(quests, Is.Empty);
    }

    [Test]
    public async Task Given_CodexWithMixedQuestStates_When_GetQuestsByState_Then_ReturnsFilteredQuests()
    {
        // Given
        CodexQuestEntry inProgressQuest = new CodexQuestEntry
        {
            QuestId = QuestId.NewId(),
            Title = "In Progress Quest",
            Description = "Description",
            DateStarted = DateTime.UtcNow
        };
        CodexQuestEntry completedQuest = new CodexQuestEntry
        {
            QuestId = QuestId.NewId(),
            Title = "Completed Quest",
            Description = "Description",
            DateStarted = DateTime.UtcNow
        };

        _codex.RecordQuestStarted(inProgressQuest, DateTime.UtcNow);
        _codex.RecordQuestStarted(completedQuest, DateTime.UtcNow);
        _codex.RecordQuestCompleted(completedQuest.QuestId, DateTime.UtcNow.AddHours(1));
        await _repository.SaveAsync(_codex);

        // When
        IReadOnlyList<CodexQuestEntry> inProgressQuests = await _queryService.GetQuestsByStateAsync(_characterId, QuestState.InProgress);
        IReadOnlyList<CodexQuestEntry> completedQuests = await _queryService.GetQuestsByStateAsync(_characterId, QuestState.Completed);

        // Then
        Assert.That(inProgressQuests.Count, Is.EqualTo(1));
        Assert.That(inProgressQuests.First().Title, Is.EqualTo("In Progress Quest"));
        Assert.That(completedQuests.Count, Is.EqualTo(1));
        Assert.That(completedQuests.First().Title, Is.EqualTo("Completed Quest"));
    }

    [Test]
    public async Task Given_CodexWithQuests_When_SearchQuests_Then_ReturnsMatchingQuests()
    {
        // Given
        CodexQuestEntry quest1 = new CodexQuestEntry
        {
            QuestId = QuestId.NewId(),
            Title = "Find the Dragon",
            Description = "Locate the ancient dragon",
            DateStarted = DateTime.UtcNow
        };
        CodexQuestEntry quest2 = new CodexQuestEntry
        {
            QuestId = QuestId.NewId(),
            Title = "Rescue the Princess",
            Description = "Save the princess from the castle",
            DateStarted = DateTime.UtcNow
        };

        _codex.RecordQuestStarted(quest1, DateTime.UtcNow);
        _codex.RecordQuestStarted(quest2, DateTime.UtcNow);
        await _repository.SaveAsync(_codex);

        // When
        IReadOnlyList<CodexQuestEntry> dragonQuests = await _queryService.SearchQuestsAsync(_characterId, "dragon");
        IReadOnlyList<CodexQuestEntry> princessQuests = await _queryService.SearchQuestsAsync(_characterId, "princess");

        // Then
        Assert.That(dragonQuests.Count, Is.EqualTo(1));
        Assert.That(dragonQuests.First().Title, Is.EqualTo("Find the Dragon"));
        Assert.That(princessQuests.Count, Is.EqualTo(1));
        Assert.That(princessQuests.First().Title, Is.EqualTo("Rescue the Princess"));
    }

    #endregion

    #region Lore Queries

    [Test]
    public async Task Given_CodexWithLore_When_GetAllLore_Then_ReturnsAllLore()
    {
        // Given
        CodexLoreEntry lore1 = new CodexLoreEntry
        {
            LoreId = LoreId.NewId(),
            Title = "Lore 1",
            Content = "Content 1",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = DateTime.UtcNow
        };
        CodexLoreEntry lore2 = new CodexLoreEntry
        {
            LoreId = LoreId.NewId(),
            Title = "Lore 2",
            Content = "Content 2",
            Category = "Geography",
            Tier = LoreTier.Rare,
            DateDiscovered = DateTime.UtcNow
        };

        _codex.RecordLoreDiscovered(lore1, DateTime.UtcNow);
        _codex.RecordLoreDiscovered(lore2, DateTime.UtcNow);
        await _repository.SaveAsync(_codex);

        // When
        IReadOnlyList<CodexLoreEntry> lore = await _queryService.GetAllLoreAsync(_characterId);

        // Then
        Assert.That(lore.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Given_CodexWithLore_When_GetLoreByTier_Then_ReturnsFilteredLore()
    {
        // Given
        CodexLoreEntry commonLore = new CodexLoreEntry
        {
            LoreId = LoreId.NewId(),
            Title = "Common Lore",
            Content = "Common content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = DateTime.UtcNow
        };
        CodexLoreEntry rareLore = new CodexLoreEntry
        {
            LoreId = LoreId.NewId(),
            Title = "Rare Lore",
            Content = "Rare content",
            Category = "History",
            Tier = LoreTier.Rare,
            DateDiscovered = DateTime.UtcNow
        };

        _codex.RecordLoreDiscovered(commonLore, DateTime.UtcNow);
        _codex.RecordLoreDiscovered(rareLore, DateTime.UtcNow);
        await _repository.SaveAsync(_codex);

        // When
        IReadOnlyList<CodexLoreEntry> common = await _queryService.GetLoreByTierAsync(_characterId, LoreTier.Common);
        IReadOnlyList<CodexLoreEntry> rare = await _queryService.GetLoreByTierAsync(_characterId, LoreTier.Rare);

        // Then
        Assert.That(common.Count, Is.EqualTo(1));
        Assert.That(common.First().Title, Is.EqualTo("Common Lore"));
        Assert.That(rare.Count, Is.EqualTo(1));
        Assert.That(rare.First().Title, Is.EqualTo("Rare Lore"));
    }

    [Test]
    public async Task Given_CodexWithLore_When_GetLoreByCategory_Then_ReturnsFilteredLore()
    {
        // Given
        CodexLoreEntry historyLore = new CodexLoreEntry
        {
            LoreId = LoreId.NewId(),
            Title = "History Lore",
            Content = "Historical content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = DateTime.UtcNow
        };
        CodexLoreEntry geographyLore = new CodexLoreEntry
        {
            LoreId = LoreId.NewId(),
            Title = "Geography Lore",
            Content = "Geographical content",
            Category = "Geography",
            Tier = LoreTier.Common,
            DateDiscovered = DateTime.UtcNow
        };

        _codex.RecordLoreDiscovered(historyLore, DateTime.UtcNow);
        _codex.RecordLoreDiscovered(geographyLore, DateTime.UtcNow);
        await _repository.SaveAsync(_codex);

        // When
        IReadOnlyList<CodexLoreEntry> history = await _queryService.GetLoreByCategoryAsync(_characterId, "History");
        IReadOnlyList<CodexLoreEntry> geography = await _queryService.GetLoreByCategoryAsync(_characterId, "Geography");

        // Then
        Assert.That(history.Count, Is.EqualTo(1));
        Assert.That(history.First().Title, Is.EqualTo("History Lore"));
        Assert.That(geography.Count, Is.EqualTo(1));
        Assert.That(geography.First().Title, Is.EqualTo("Geography Lore"));
    }

    [Test]
    public async Task Given_CodexWithLore_When_SearchLore_Then_ReturnsMatchingLore()
    {
        // Given
        CodexLoreEntry lore1 = new CodexLoreEntry
        {
            LoreId = LoreId.NewId(),
            Title = "The Ancient Dragons",
            Content = "Dragons ruled the land in ancient times",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = DateTime.UtcNow
        };
        CodexLoreEntry lore2 = new CodexLoreEntry
        {
            LoreId = LoreId.NewId(),
            Title = "The Elven Kingdoms",
            Content = "Elves established great kingdoms",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = DateTime.UtcNow
        };

        _codex.RecordLoreDiscovered(lore1, DateTime.UtcNow);
        _codex.RecordLoreDiscovered(lore2, DateTime.UtcNow);
        await _repository.SaveAsync(_codex);

        // When
        IReadOnlyList<CodexLoreEntry> dragonLore = await _queryService.SearchLoreAsync(_characterId, "dragon");

        // Then
        Assert.That(dragonLore.Count, Is.EqualTo(1));
        Assert.That(dragonLore.First().Title, Is.EqualTo("The Ancient Dragons"));
    }

    #endregion

    #region Note Queries

    [Test]
    public async Task Given_CodexWithNotes_When_GetAllNotes_Then_ReturnsAllNotes()
    {
        // Given
        CodexNoteEntry note1 = new CodexNoteEntry(Guid.NewGuid(), "Note 1", NoteCategory.General, DateTime.UtcNow, false, false);
        CodexNoteEntry note2 = new CodexNoteEntry(Guid.NewGuid(), "Note 2", NoteCategory.Quest, DateTime.UtcNow, false, false);

        _codex.AddNote(note1, DateTime.UtcNow);
        _codex.AddNote(note2, DateTime.UtcNow);
        await _repository.SaveAsync(_codex);

        // When
        IReadOnlyList<CodexNoteEntry> notes = await _queryService.GetAllNotesAsync(_characterId);

        // Then
        Assert.That(notes.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Given_CodexWithNotes_When_GetNotesByCategory_Then_ReturnsFilteredNotes()
    {
        // Given
        CodexNoteEntry personalNote = new CodexNoteEntry(Guid.NewGuid(), "Personal note", NoteCategory.General, DateTime.UtcNow, false, false);
        CodexNoteEntry questNote = new CodexNoteEntry(Guid.NewGuid(), "Quest note", NoteCategory.Quest, DateTime.UtcNow, false, false);

        _codex.AddNote(personalNote, DateTime.UtcNow);
        _codex.AddNote(questNote, DateTime.UtcNow);
        await _repository.SaveAsync(_codex);

        // When
        IReadOnlyList<CodexNoteEntry> personal = await _queryService.GetNotesByCategoryAsync(_characterId, NoteCategory.General);
        IReadOnlyList<CodexNoteEntry> quest = await _queryService.GetNotesByCategoryAsync(_characterId, NoteCategory.Quest);

        // Then
        Assert.That(personal.Count, Is.EqualTo(1));
        Assert.That(personal.First().Content, Is.EqualTo("Personal note"));
        Assert.That(quest.Count, Is.EqualTo(1));
        Assert.That(quest.First().Content, Is.EqualTo("Quest note"));
    }

    [Test]
    public async Task Given_CodexWithNotes_When_GetDmNotes_Then_ReturnsOnlyDmNotes()
    {
        // Given
        CodexNoteEntry playerNote = new CodexNoteEntry(Guid.NewGuid(), "Player note", NoteCategory.General, DateTime.UtcNow, false, false);
        CodexNoteEntry dmNote = new CodexNoteEntry(Guid.NewGuid(), "DM note", NoteCategory.DmNote, DateTime.UtcNow, true, false);

        _codex.AddNote(playerNote, DateTime.UtcNow);
        _codex.AddNote(dmNote, DateTime.UtcNow);
        await _repository.SaveAsync(_codex);

        // When
        IReadOnlyList<CodexNoteEntry> dmNotes = await _queryService.GetDmNotesAsync(_characterId);

        // Then
        Assert.That(dmNotes.Count, Is.EqualTo(1));
        Assert.That(dmNotes.First().Content, Is.EqualTo("DM note"));
        Assert.That(dmNotes.First().IsDmNote, Is.True);
    }

    #endregion

    #region Reputation Queries

    [Test]
    public async Task Given_CodexWithReputations_When_GetAllReputations_Then_ReturnsAllReputations()
    {
        // Given
        FactionId faction1 = FactionId.NewId();
        FactionId faction2 = FactionId.NewId();

        _codex.RecordReputationChange(faction1, "Faction 1", ReputationScore.Parse(50), "Test", DateTime.UtcNow);
        _codex.RecordReputationChange(faction2, "Faction 2", ReputationScore.Parse(-30), "Test", DateTime.UtcNow);
        await _repository.SaveAsync(_codex);

        // When
        IReadOnlyList<FactionReputation> reputations = await _queryService.GetAllReputationsAsync(_characterId);

        // Then
        Assert.That(reputations.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Given_CodexWithReputations_When_GetPositiveReputations_Then_ReturnsOnlyPositive()
    {
        // Given
        FactionId faction1 = FactionId.NewId();
        FactionId faction2 = FactionId.NewId();
        FactionId faction3 = FactionId.NewId();

        _codex.RecordReputationChange(faction1, "Faction 1", ReputationScore.Parse(50), "Test", DateTime.UtcNow);
        _codex.RecordReputationChange(faction2, "Faction 2", ReputationScore.Parse(-30), "Test", DateTime.UtcNow);
        _codex.RecordReputationChange(faction3, "Faction 3", ReputationScore.Parse(100), "Test", DateTime.UtcNow);
        await _repository.SaveAsync(_codex);

        // When
        IReadOnlyList<FactionReputation> positive = await _queryService.GetPositiveReputationsAsync(_characterId);

        // Then
        Assert.That(positive.Count, Is.EqualTo(2));
        Assert.That(positive.All(r => r.CurrentScore.Value > 0), Is.True);
    }

    [Test]
    public async Task Given_CodexWithReputations_When_GetNegativeReputations_Then_ReturnsOnlyNegative()
    {
        // Given
        FactionId faction1 = FactionId.NewId();
        FactionId faction2 = FactionId.NewId();

        _codex.RecordReputationChange(faction1, "Faction 1", ReputationScore.Parse(50), "Test", DateTime.UtcNow);
        _codex.RecordReputationChange(faction2, "Faction 2", ReputationScore.Parse(-30), "Test", DateTime.UtcNow);
        await _repository.SaveAsync(_codex);

        // When
        IReadOnlyList<FactionReputation> negative = await _queryService.GetNegativeReputationsAsync(_characterId);

        // Then
        Assert.That(negative.Count, Is.EqualTo(1));
        Assert.That(negative.First().CurrentScore.Value, Is.LessThan(0));
    }

    #endregion

    #region Constructor Tests

    [Test]
    public void Given_NullRepository_When_Constructing_Then_ThrowsArgumentNullException()
    {
        // When/Then
        Assert.Throws<ArgumentNullException>(() => new CodexQueryService(null!));
    }

    #endregion
}

