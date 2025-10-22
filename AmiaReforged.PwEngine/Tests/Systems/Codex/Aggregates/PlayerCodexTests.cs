using AmiaReforged.PwEngine.Features.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.Codex.Aggregates;

[TestFixture]
public class PlayerCodexTests
{
    private CharacterId _testCharacterId;
    private DateTime _testDate;

    [SetUp]
    public void SetUp()
    {
        _testCharacterId = CharacterId.New();
        _testDate = new DateTime(2025, 10, 22, 12, 0, 0);
    }

    #region Construction Tests

    [Test]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var codex = new PlayerCodex(_testCharacterId, _testDate);

        // Assert
        Assert.That(codex.OwnerId, Is.EqualTo(_testCharacterId));
        Assert.That(codex.DateCreated, Is.EqualTo(_testDate));
        Assert.That(codex.LastUpdated, Is.EqualTo(_testDate));
    }

    [Test]
    public void Constructor_InitializesEmptyCollections()
    {
        // Act
        var codex = new PlayerCodex(_testCharacterId, _testDate);

        // Assert
        Assert.That(codex.Quests, Is.Empty);
        Assert.That(codex.Lore, Is.Empty);
        Assert.That(codex.Notes, Is.Empty);
        Assert.That(codex.Reputations, Is.Empty);
    }

    #endregion

    #region Quest Command Tests

    [Test]
    public void RecordQuestStarted_WithValidQuest_AddsQuest()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var quest = CreateTestQuest("quest_001", "Test Quest");
        var occurredAt = _testDate.AddHours(1);

        // Act
        codex.RecordQuestStarted(quest, occurredAt);

        // Assert
        Assert.That(codex.Quests, Has.Count.EqualTo(1));
        Assert.That(codex.HasQuest(quest.QuestId), Is.True);
        Assert.That(codex.LastUpdated, Is.EqualTo(occurredAt));
    }

    [Test]
    public void RecordQuestStarted_WithDuplicateQuestId_ThrowsInvalidOperationException()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var quest1 = CreateTestQuest("quest_001", "Test Quest 1");
        var quest2 = CreateTestQuest("quest_001", "Test Quest 2");
        codex.RecordQuestStarted(quest1, _testDate);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            codex.RecordQuestStarted(quest2, _testDate.AddHours(1)));
        Assert.That(ex.Message, Does.Contain("quest_001"));
        Assert.That(ex.Message, Does.Contain("already exists"));
    }

    [Test]
    public void RecordQuestStarted_WithNullQuest_ThrowsArgumentNullException()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            codex.RecordQuestStarted(null!, _testDate));
    }

    [Test]
    public void RecordQuestCompleted_WithValidQuest_UpdatesQuestStateAndLastUpdated()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var quest = CreateTestQuest("quest_001", "Test Quest");
        codex.RecordQuestStarted(quest, _testDate);
        var completedAt = _testDate.AddHours(2);

        // Act
        codex.RecordQuestCompleted(quest.QuestId, completedAt);

        // Assert
        var retrievedQuest = codex.GetQuest(quest.QuestId);
        Assert.That(retrievedQuest, Is.Not.Null);
        Assert.That(retrievedQuest!.State, Is.EqualTo(QuestState.Completed));
        Assert.That(retrievedQuest.DateCompleted, Is.EqualTo(completedAt));
        Assert.That(codex.LastUpdated, Is.EqualTo(completedAt));
    }

    [Test]
    public void RecordQuestCompleted_WithNonExistentQuest_ThrowsInvalidOperationException()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var questId = new QuestId("nonexistent");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            codex.RecordQuestCompleted(questId, _testDate));
        Assert.That(ex.Message, Does.Contain("nonexistent"));
        Assert.That(ex.Message, Does.Contain("not found"));
    }

    [Test]
    public void RecordQuestFailed_WithValidQuest_UpdatesQuestState()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var quest = CreateTestQuest("quest_001", "Test Quest");
        codex.RecordQuestStarted(quest, _testDate);
        var failedAt = _testDate.AddHours(2);

        // Act
        codex.RecordQuestFailed(quest.QuestId, failedAt);

        // Assert
        var retrievedQuest = codex.GetQuest(quest.QuestId);
        Assert.That(retrievedQuest!.State, Is.EqualTo(QuestState.Failed));
        Assert.That(codex.LastUpdated, Is.EqualTo(failedAt));
    }

    [Test]
    public void RecordQuestAbandoned_WithValidQuest_UpdatesQuestState()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var quest = CreateTestQuest("quest_001", "Test Quest");
        codex.RecordQuestStarted(quest, _testDate);
        var abandonedAt = _testDate.AddHours(2);

        // Act
        codex.RecordQuestAbandoned(quest.QuestId, abandonedAt);

        // Assert
        var retrievedQuest = codex.GetQuest(quest.QuestId);
        Assert.That(retrievedQuest!.State, Is.EqualTo(QuestState.Abandoned));
        Assert.That(codex.LastUpdated, Is.EqualTo(abandonedAt));
    }

    [Test]
    public void GetQuest_WithExistingQuest_ReturnsQuest()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var quest = CreateTestQuest("quest_001", "Test Quest");
        codex.RecordQuestStarted(quest, _testDate);

        // Act
        var result = codex.GetQuest(quest.QuestId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.QuestId, Is.EqualTo(quest.QuestId));
    }

    [Test]
    public void GetQuest_WithNonExistentQuest_ReturnsNull()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var questId = new QuestId("nonexistent");

        // Act
        var result = codex.GetQuest(questId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void HasQuest_WithExistingQuest_ReturnsTrue()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var quest = CreateTestQuest("quest_001", "Test Quest");
        codex.RecordQuestStarted(quest, _testDate);

        // Act & Assert
        Assert.That(codex.HasQuest(quest.QuestId), Is.True);
    }

    [Test]
    public void HasQuest_WithNonExistentQuest_ReturnsFalse()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var questId = new QuestId("nonexistent");

        // Act & Assert
        Assert.That(codex.HasQuest(questId), Is.False);
    }

    #endregion

    #region Lore Command Tests

    [Test]
    public void RecordLoreDiscovered_WithValidLore_AddsLore()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var lore = CreateTestLore("lore_001", "Test Lore");
        var occurredAt = _testDate.AddHours(1);

        // Act
        codex.RecordLoreDiscovered(lore, occurredAt);

        // Assert
        Assert.That(codex.Lore, Has.Count.EqualTo(1));
        Assert.That(codex.HasLore(lore.LoreId), Is.True);
        Assert.That(codex.LastUpdated, Is.EqualTo(occurredAt));
    }

    [Test]
    public void RecordLoreDiscovered_WithDuplicateLoreId_ThrowsInvalidOperationException()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var lore1 = CreateTestLore("lore_001", "Test Lore 1");
        var lore2 = CreateTestLore("lore_001", "Test Lore 2");
        codex.RecordLoreDiscovered(lore1, _testDate);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            codex.RecordLoreDiscovered(lore2, _testDate.AddHours(1)));
        Assert.That(ex.Message, Does.Contain("lore_001"));
        Assert.That(ex.Message, Does.Contain("already exists"));
    }

    [Test]
    public void RecordLoreDiscovered_WithNullLore_ThrowsArgumentNullException()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            codex.RecordLoreDiscovered(null!, _testDate));
    }

    [Test]
    public void GetLore_WithExistingLore_ReturnsLore()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var lore = CreateTestLore("lore_001", "Test Lore");
        codex.RecordLoreDiscovered(lore, _testDate);

        // Act
        var result = codex.GetLore(lore.LoreId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.LoreId, Is.EqualTo(lore.LoreId));
    }

    [Test]
    public void GetLore_WithNonExistentLore_ReturnsNull()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var loreId = new LoreId("nonexistent");

        // Act
        var result = codex.GetLore(loreId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void HasLore_WithExistingLore_ReturnsTrue()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var lore = CreateTestLore("lore_001", "Test Lore");
        codex.RecordLoreDiscovered(lore, _testDate);

        // Act & Assert
        Assert.That(codex.HasLore(lore.LoreId), Is.True);
    }

    [Test]
    public void HasLore_WithNonExistentLore_ReturnsFalse()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var loreId = new LoreId("nonexistent");

        // Act & Assert
        Assert.That(codex.HasLore(loreId), Is.False);
    }

    #endregion

    #region Note Command Tests

    [Test]
    public void AddNote_WithValidNote_AddsNote()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var note = CreateTestNote(Guid.NewGuid(), "Test note content");
        var occurredAt = _testDate.AddHours(1);

        // Act
        codex.AddNote(note, occurredAt);

        // Assert
        Assert.That(codex.Notes, Has.Count.EqualTo(1));
        Assert.That(codex.HasNote(note.Id), Is.True);
        Assert.That(codex.LastUpdated, Is.EqualTo(occurredAt));
    }

    [Test]
    public void AddNote_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var noteId = Guid.NewGuid();
        var note1 = CreateTestNote(noteId, "Test note 1");
        var note2 = CreateTestNote(noteId, "Test note 2");
        codex.AddNote(note1, _testDate);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            codex.AddNote(note2, _testDate.AddHours(1)));
        Assert.That(ex.Message, Does.Contain(noteId.ToString()));
        Assert.That(ex.Message, Does.Contain("already exists"));
    }

    [Test]
    public void AddNote_WithNullNote_ThrowsArgumentNullException()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            codex.AddNote(null!, _testDate));
    }

    [Test]
    public void EditNote_WithValidNote_UpdatesContentAndLastUpdated()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var note = CreateTestNote(Guid.NewGuid(), "Original content");
        codex.AddNote(note, _testDate);
        var editedAt = _testDate.AddHours(1);
        var newContent = "Updated content";

        // Act
        codex.EditNote(note.Id, newContent, editedAt);

        // Assert
        var retrievedNote = codex.GetNote(note.Id);
        Assert.That(retrievedNote!.Content, Is.EqualTo(newContent));
        Assert.That(retrievedNote.LastModified, Is.EqualTo(editedAt));
        Assert.That(codex.LastUpdated, Is.EqualTo(editedAt));
    }

    [Test]
    public void EditNote_WithNonExistentNote_ThrowsInvalidOperationException()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var noteId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            codex.EditNote(noteId, "New content", _testDate));
        Assert.That(ex.Message, Does.Contain(noteId.ToString()));
        Assert.That(ex.Message, Does.Contain("not found"));
    }

    [Test]
    public void DeleteNote_WithValidNote_RemovesNoteAndUpdatesLastUpdated()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var note = CreateTestNote(Guid.NewGuid(), "Test note");
        codex.AddNote(note, _testDate);
        var deletedAt = _testDate.AddHours(1);

        // Act
        codex.DeleteNote(note.Id, deletedAt);

        // Assert
        Assert.That(codex.Notes, Is.Empty);
        Assert.That(codex.HasNote(note.Id), Is.False);
        Assert.That(codex.LastUpdated, Is.EqualTo(deletedAt));
    }

    [Test]
    public void DeleteNote_WithNonExistentNote_ThrowsInvalidOperationException()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var noteId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            codex.DeleteNote(noteId, _testDate));
        Assert.That(ex.Message, Does.Contain(noteId.ToString()));
        Assert.That(ex.Message, Does.Contain("not found"));
    }

    [Test]
    public void GetNote_WithExistingNote_ReturnsNote()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var note = CreateTestNote(Guid.NewGuid(), "Test note");
        codex.AddNote(note, _testDate);

        // Act
        var result = codex.GetNote(note.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(note.Id));
    }

    [Test]
    public void GetNote_WithNonExistentNote_ReturnsNull()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var noteId = Guid.NewGuid();

        // Act
        var result = codex.GetNote(noteId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void HasNote_WithExistingNote_ReturnsTrue()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var note = CreateTestNote(Guid.NewGuid(), "Test note");
        codex.AddNote(note, _testDate);

        // Act & Assert
        Assert.That(codex.HasNote(note.Id), Is.True);
    }

    [Test]
    public void HasNote_WithNonExistentNote_ReturnsFalse()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var noteId = Guid.NewGuid();

        // Act & Assert
        Assert.That(codex.HasNote(noteId), Is.False);
    }

    #endregion

    #region Reputation Command Tests

    [Test]
    public void RecordReputationChange_FirstInteraction_CreatesNewReputation()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var factionId = new FactionId("faction_001");
        var factionName = "Test Faction";
        var occurredAt = _testDate.AddHours(1);

        // Act
        codex.RecordReputationChange(factionId, factionName, 10, "Helped villagers", occurredAt);

        // Assert
        Assert.That(codex.Reputations, Has.Count.EqualTo(1));
        Assert.That(codex.HasReputation(factionId), Is.True);
        var reputation = codex.GetReputation(factionId);
        Assert.That(reputation, Is.Not.Null);
        Assert.That(reputation!.FactionId, Is.EqualTo(factionId));
        Assert.That(reputation.FactionName, Is.EqualTo(factionName));
        Assert.That(reputation.CurrentScore.Value, Is.EqualTo(10));
        Assert.That(codex.LastUpdated, Is.EqualTo(occurredAt));
    }

    [Test]
    public void RecordReputationChange_ExistingReputation_UpdatesReputation()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var factionId = new FactionId("faction_001");
        var factionName = "Test Faction";
        codex.RecordReputationChange(factionId, factionName, 10, "First deed", _testDate);
        var secondChangeAt = _testDate.AddHours(1);

        // Act
        codex.RecordReputationChange(factionId, factionName, 5, "Second deed", secondChangeAt);

        // Assert
        Assert.That(codex.Reputations, Has.Count.EqualTo(1));
        var reputation = codex.GetReputation(factionId);
        Assert.That(reputation!.CurrentScore.Value, Is.EqualTo(15));
        Assert.That(reputation.History, Has.Count.EqualTo(2));
        Assert.That(codex.LastUpdated, Is.EqualTo(secondChangeAt));
    }

    [Test]
    public void RecordReputationChange_UpdatesLastUpdated()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var factionId = new FactionId("faction_001");
        var occurredAt = _testDate.AddHours(2);

        // Act
        codex.RecordReputationChange(factionId, "Test Faction", 10, "Test", occurredAt);

        // Assert
        Assert.That(codex.LastUpdated, Is.EqualTo(occurredAt));
    }

    [Test]
    public void GetReputation_WithExistingFaction_ReturnsReputation()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var factionId = new FactionId("faction_001");
        codex.RecordReputationChange(factionId, "Test Faction", 10, "Test", _testDate);

        // Act
        var result = codex.GetReputation(factionId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FactionId, Is.EqualTo(factionId));
    }

    [Test]
    public void GetReputation_WithNonExistentFaction_ReturnsNull()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var factionId = new FactionId("nonexistent");

        // Act
        var result = codex.GetReputation(factionId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void HasReputation_WithExistingFaction_ReturnsTrue()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var factionId = new FactionId("faction_001");
        codex.RecordReputationChange(factionId, "Test Faction", 10, "Test", _testDate);

        // Act & Assert
        Assert.That(codex.HasReputation(factionId), Is.True);
    }

    [Test]
    public void HasReputation_WithNonExistentFaction_ReturnsFalse()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var factionId = new FactionId("nonexistent");

        // Act & Assert
        Assert.That(codex.HasReputation(factionId), Is.False);
    }

    #endregion

    #region Query Method Tests

    [Test]
    public void GetQuestsByState_WithDiscoveredState_ReturnsOnlyDiscoveredQuests()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var quest1 = CreateTestQuest("quest_001", "Discovered Quest");
        var quest2 = CreateTestQuest("quest_002", "Completed Quest");
        codex.RecordQuestStarted(quest1, _testDate);
        codex.RecordQuestStarted(quest2, _testDate);
        codex.RecordQuestCompleted(quest2.QuestId, _testDate.AddHours(1));

        // Act
        var result = codex.GetQuestsByState(QuestState.Discovered).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].QuestId, Is.EqualTo(quest1.QuestId));
    }

    [Test]
    public void GetQuestsByState_WithCompletedState_ReturnsOnlyCompletedQuests()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var quest1 = CreateTestQuest("quest_001", "Quest 1");
        var quest2 = CreateTestQuest("quest_002", "Quest 2");
        codex.RecordQuestStarted(quest1, _testDate);
        codex.RecordQuestStarted(quest2, _testDate);
        codex.RecordQuestCompleted(quest2.QuestId, _testDate.AddHours(1));

        // Act
        var result = codex.GetQuestsByState(QuestState.Completed).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].QuestId, Is.EqualTo(quest2.QuestId));
    }

    [Test]
    public void GetQuestsByState_WithFailedState_ReturnsOnlyFailedQuests()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var quest1 = CreateTestQuest("quest_001", "Quest 1");
        var quest2 = CreateTestQuest("quest_002", "Quest 2");
        codex.RecordQuestStarted(quest1, _testDate);
        codex.RecordQuestStarted(quest2, _testDate);
        codex.RecordQuestFailed(quest2.QuestId, _testDate.AddHours(1));

        // Act
        var result = codex.GetQuestsByState(QuestState.Failed).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].QuestId, Is.EqualTo(quest2.QuestId));
    }

    [Test]
    public void GetQuestsByState_WithAbandonedState_ReturnsOnlyAbandonedQuests()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var quest1 = CreateTestQuest("quest_001", "Quest 1");
        var quest2 = CreateTestQuest("quest_002", "Quest 2");
        codex.RecordQuestStarted(quest1, _testDate);
        codex.RecordQuestStarted(quest2, _testDate);
        codex.RecordQuestAbandoned(quest2.QuestId, _testDate.AddHours(1));

        // Act
        var result = codex.GetQuestsByState(QuestState.Abandoned).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].QuestId, Is.EqualTo(quest2.QuestId));
    }

    [Test]
    public void GetLoreByTier_WithCommonTier_ReturnsOnlyCommonLore()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var lore1 = CreateTestLoreWithTier("lore_001", "Common Lore", LoreTier.Common);
        var lore2 = CreateTestLoreWithTier("lore_002", "Rare Lore", LoreTier.Rare);
        codex.RecordLoreDiscovered(lore1, _testDate);
        codex.RecordLoreDiscovered(lore2, _testDate);

        // Act
        var result = codex.GetLoreByTier(LoreTier.Common).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].LoreId, Is.EqualTo(lore1.LoreId));
    }

    [Test]
    public void GetLoreByTier_WithRareTier_ReturnsOnlyRareLore()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var lore1 = CreateTestLoreWithTier("lore_001", "Common Lore", LoreTier.Common);
        var lore2 = CreateTestLoreWithTier("lore_002", "Rare Lore", LoreTier.Rare);
        codex.RecordLoreDiscovered(lore1, _testDate);
        codex.RecordLoreDiscovered(lore2, _testDate);

        // Act
        var result = codex.GetLoreByTier(LoreTier.Rare).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].LoreId, Is.EqualTo(lore2.LoreId));
    }

    [Test]
    public void GetLoreByTier_WithLegendaryTier_ReturnsOnlyLegendaryLore()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var lore1 = CreateTestLoreWithTier("lore_001", "Common Lore", LoreTier.Common);
        var lore2 = CreateTestLoreWithTier("lore_002", "Legendary Lore", LoreTier.Legendary);
        codex.RecordLoreDiscovered(lore1, _testDate);
        codex.RecordLoreDiscovered(lore2, _testDate);

        // Act
        var result = codex.GetLoreByTier(LoreTier.Legendary).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].LoreId, Is.EqualTo(lore2.LoreId));
    }

    [Test]
    public void GetNotesByCategory_WithGeneralCategory_ReturnsOnlyGeneralNotes()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var note1 = CreateTestNoteWithCategory(Guid.NewGuid(), "General note", NoteCategory.General);
        var note2 = CreateTestNoteWithCategory(Guid.NewGuid(), "Quest note", NoteCategory.Quest);
        codex.AddNote(note1, _testDate);
        codex.AddNote(note2, _testDate);

        // Act
        var result = codex.GetNotesByCategory(NoteCategory.General).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(note1.Id));
    }

    [Test]
    public void GetNotesByCategory_WithQuestCategory_ReturnsOnlyQuestNotes()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var note1 = CreateTestNoteWithCategory(Guid.NewGuid(), "General note", NoteCategory.General);
        var note2 = CreateTestNoteWithCategory(Guid.NewGuid(), "Quest note", NoteCategory.Quest);
        codex.AddNote(note1, _testDate);
        codex.AddNote(note2, _testDate);

        // Act
        var result = codex.GetNotesByCategory(NoteCategory.Quest).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(note2.Id));
    }

    [Test]
    public void GetNotesByCategory_WithLocationCategory_ReturnsOnlyLocationNotes()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var note1 = CreateTestNoteWithCategory(Guid.NewGuid(), "Character note", NoteCategory.Character);
        var note2 = CreateTestNoteWithCategory(Guid.NewGuid(), "Location note", NoteCategory.Location);
        codex.AddNote(note1, _testDate);
        codex.AddNote(note2, _testDate);

        // Act
        var result = codex.GetNotesByCategory(NoteCategory.Location).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(note2.Id));
    }

    [Test]
    public void SearchQuests_WithMatchingTerm_ReturnsMatchingQuests()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var quest1 = CreateTestQuest("quest_001", "Dragon Quest");
        var quest2 = CreateTestQuest("quest_002", "Village Help");
        codex.RecordQuestStarted(quest1, _testDate);
        codex.RecordQuestStarted(quest2, _testDate);

        // Act
        var result = codex.SearchQuests("dragon").ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].QuestId, Is.EqualTo(quest1.QuestId));
    }

    [Test]
    public void SearchQuests_WithNoMatches_ReturnsEmpty()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var quest = CreateTestQuest("quest_001", "Dragon Quest");
        codex.RecordQuestStarted(quest, _testDate);

        // Act
        var result = codex.SearchQuests("nonexistent").ToList();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SearchLore_WithMatchingTerm_ReturnsMatchingLore()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var lore1 = CreateTestLore("lore_001", "Ancient Dragon History");
        var lore2 = CreateTestLore("lore_002", "Village Traditions");
        codex.RecordLoreDiscovered(lore1, _testDate);
        codex.RecordLoreDiscovered(lore2, _testDate);

        // Act
        var result = codex.SearchLore("dragon").ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].LoreId, Is.EqualTo(lore1.LoreId));
    }

    [Test]
    public void SearchNotes_WithMatchingTerm_ReturnsMatchingNotes()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        var note1 = CreateTestNote(Guid.NewGuid(), "Met a dragon today");
        var note2 = CreateTestNote(Guid.NewGuid(), "Visited the village");
        codex.AddNote(note1, _testDate);
        codex.AddNote(note2, _testDate);

        // Act
        var result = codex.SearchNotes("dragon").ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(note1.Id));
    }

    [Test]
    public void GetTotalEntryCount_WithMultipleEntries_ReturnsCorrectSum()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);
        codex.RecordQuestStarted(CreateTestQuest("quest_001", "Quest 1"), _testDate);
        codex.RecordQuestStarted(CreateTestQuest("quest_002", "Quest 2"), _testDate);
        codex.RecordLoreDiscovered(CreateTestLore("lore_001", "Lore 1"), _testDate);
        codex.AddNote(CreateTestNote(Guid.NewGuid(), "Note 1"), _testDate);
        codex.AddNote(CreateTestNote(Guid.NewGuid(), "Note 2"), _testDate);
        codex.AddNote(CreateTestNote(Guid.NewGuid(), "Note 3"), _testDate);
        codex.RecordReputationChange(new FactionId("faction_001"), "Faction 1", 10, "Test", _testDate);

        // Act
        var count = codex.GetTotalEntryCount();

        // Assert
        Assert.That(count, Is.EqualTo(7)); // 2 quests + 1 lore + 3 notes + 1 reputation
    }

    [Test]
    public void GetTotalEntryCount_WithEmptyCodex_ReturnsZero()
    {
        // Arrange
        var codex = new PlayerCodex(_testCharacterId, _testDate);

        // Act
        var count = codex.GetTotalEntryCount();

        // Assert
        Assert.That(count, Is.EqualTo(0));
    }

    #endregion

    #region DM Support Tests

    [Test]
    public void PlayerCodex_CanBeCreatedWithDmId()
    {
        // Arrange
        var dmId = CharacterId.New();

        // Act
        var codex = new PlayerCodex(dmId, _testDate);

        // Assert
        Assert.That(codex.OwnerId, Is.EqualTo(dmId));
    }

    [Test]
    public void DM_CanAddQuests()
    {
        // Arrange
        var dmId = CharacterId.New();
        var codex = new PlayerCodex(dmId, _testDate);
        var quest = CreateTestQuest("quest_001", "DM Quest");

        // Act
        codex.RecordQuestStarted(quest, _testDate);

        // Assert
        Assert.That(codex.Quests, Has.Count.EqualTo(1));
    }

    [Test]
    public void DM_CanAddLore()
    {
        // Arrange
        var dmId = CharacterId.New();
        var codex = new PlayerCodex(dmId, _testDate);
        var lore = CreateTestLore("lore_001", "DM Lore");

        // Act
        codex.RecordLoreDiscovered(lore, _testDate);

        // Assert
        Assert.That(codex.Lore, Has.Count.EqualTo(1));
    }

    [Test]
    public void DM_CanAddNotes()
    {
        // Arrange
        var dmId = CharacterId.New();
        var codex = new PlayerCodex(dmId, _testDate);
        var note = CreateTestNote(Guid.NewGuid(), "DM Note", isDmNote: true);

        // Act
        codex.AddNote(note, _testDate);

        // Assert
        Assert.That(codex.Notes, Has.Count.EqualTo(1));
        Assert.That(codex.Notes.First().IsDmNote, Is.True);
    }

    [Test]
    public void DM_CanAddReputation()
    {
        // Arrange
        var dmId = CharacterId.New();
        var codex = new PlayerCodex(dmId, _testDate);
        var factionId = new FactionId("faction_001");

        // Act
        codex.RecordReputationChange(factionId, "Test Faction", 10, "DM Award", _testDate);

        // Assert
        Assert.That(codex.Reputations, Has.Count.EqualTo(1));
    }

    #endregion

    #region Helper Methods

    private CodexQuestEntry CreateTestQuest(string questId, string title)
    {
        return new CodexQuestEntry
        {
            QuestId = new QuestId(questId),
            Title = title,
            Description = $"Description for {title}",
            DateStarted = _testDate
        };
    }

    private CodexLoreEntry CreateTestLore(string loreId, string title)
    {
        return new CodexLoreEntry
        {
            LoreId = new LoreId(loreId),
            Title = title,
            Content = $"Content for {title}",
            Category = "Test Category",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };
    }

    private CodexLoreEntry CreateTestLoreWithTier(string loreId, string title, LoreTier tier)
    {
        return new CodexLoreEntry
        {
            LoreId = new LoreId(loreId),
            Title = title,
            Content = $"Content for {title}",
            Category = "Test Category",
            Tier = tier,
            DateDiscovered = _testDate
        };
    }

    private CodexNoteEntry CreateTestNote(Guid id, string content, bool isDmNote = false)
    {
        return new CodexNoteEntry(
            id,
            content,
            NoteCategory.General,
            _testDate,
            isDmNote,
            false
        );
    }

    private CodexNoteEntry CreateTestNoteWithCategory(Guid id, string content, NoteCategory category)
    {
        return new CodexNoteEntry(
            id,
            content,
            category,
            _testDate,
            false,
            false
        );
    }

    #endregion
}
