using AmiaReforged.PwEngine.Features.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Codex.Aggregates;

/// <summary>
/// Aggregate root for a player's entire codex.
/// Encapsulates all codex data and enforces invariants.
/// Supports both player characters and DMs (via DmId → CharacterId conversion).
/// </summary>
public class PlayerCodex
{
    /// <summary>
    /// Character or DM identifier
    /// </summary>
    public CharacterId OwnerId { get; init; }

    /// <summary>
    /// When this codex was first created
    /// </summary>
    public DateTime DateCreated { get; init; }

    /// <summary>
    /// When this codex was last updated
    /// </summary>
    public DateTime LastUpdated { get; private set; }

    private readonly Dictionary<QuestId, CodexQuestEntry> _quests = new();
    private readonly Dictionary<LoreId, CodexLoreEntry> _lore = new();
    private readonly Dictionary<Guid, CodexNoteEntry> _notes = new();
    private readonly Dictionary<FactionId, FactionReputation> _reputations = new();

    /// <summary>
    /// Read-only view of all quests
    /// </summary>
    public IReadOnlyCollection<CodexQuestEntry> Quests => _quests.Values;

    /// <summary>
    /// Read-only view of all lore
    /// </summary>
    public IReadOnlyCollection<CodexLoreEntry> Lore => _lore.Values;

    /// <summary>
    /// Read-only view of all notes
    /// </summary>
    public IReadOnlyCollection<CodexNoteEntry> Notes => _notes.Values;

    /// <summary>
    /// Read-only view of all faction reputations
    /// </summary>
    public IReadOnlyCollection<FactionReputation> Reputations => _reputations.Values;

    public PlayerCodex(CharacterId ownerId, DateTime dateCreated)
    {
        OwnerId = ownerId;
        DateCreated = dateCreated;
        LastUpdated = dateCreated;
    }

    #region Quest Commands

    /// <summary>
    /// Records a new quest or updates existing quest to InProgress
    /// </summary>
    public void RecordQuestStarted(CodexQuestEntry quest, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(quest);

        if (_quests.ContainsKey(quest.QuestId))
            throw new InvalidOperationException($"Quest {quest.QuestId.Value} already exists in codex");

        _quests[quest.QuestId] = quest;
        LastUpdated = occurredAt;
    }

    /// <summary>
    /// Marks a quest as completed
    /// </summary>
    public void RecordQuestCompleted(QuestId questId, DateTime occurredAt)
    {
        if (!_quests.TryGetValue(questId, out CodexQuestEntry? quest))
            throw new InvalidOperationException($"Quest {questId.Value} not found in codex");

        quest.MarkCompleted(occurredAt);
        LastUpdated = occurredAt;
    }

    /// <summary>
    /// Marks a quest as failed
    /// </summary>
    public void RecordQuestFailed(QuestId questId, DateTime occurredAt)
    {
        if (!_quests.TryGetValue(questId, out CodexQuestEntry? quest))
            throw new InvalidOperationException($"Quest {questId.Value} not found in codex");

        quest.MarkFailed(occurredAt);
        LastUpdated = occurredAt;
    }

    /// <summary>
    /// Marks a quest as abandoned
    /// </summary>
    public void RecordQuestAbandoned(QuestId questId, DateTime occurredAt)
    {
        if (!_quests.TryGetValue(questId, out CodexQuestEntry? quest))
            throw new InvalidOperationException($"Quest {questId.Value} not found in codex");

        quest.MarkAbandoned(occurredAt);
        LastUpdated = occurredAt;
    }

    /// <summary>
    /// Gets a quest by ID
    /// </summary>
    public CodexQuestEntry? GetQuest(QuestId questId) => _quests.GetValueOrDefault(questId);

    /// <summary>
    /// Checks if a quest exists
    /// </summary>
    public bool HasQuest(QuestId questId) => _quests.ContainsKey(questId);

    #endregion

    #region Lore Commands

    /// <summary>
    /// Records newly discovered lore
    /// </summary>
    public void RecordLoreDiscovered(CodexLoreEntry lore, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(lore);

        if (_lore.ContainsKey(lore.LoreId))
            throw new InvalidOperationException($"Lore {lore.LoreId.Value} already exists in codex");

        _lore[lore.LoreId] = lore;
        LastUpdated = occurredAt;
    }

    /// <summary>
    /// Gets lore by ID
    /// </summary>
    public CodexLoreEntry? GetLore(LoreId loreId) => _lore.GetValueOrDefault(loreId);

    /// <summary>
    /// Checks if lore exists
    /// </summary>
    public bool HasLore(LoreId loreId) => _lore.ContainsKey(loreId);

    #endregion

    #region Note Commands

    /// <summary>
    /// Adds a new note to the codex
    /// </summary>
    public void AddNote(CodexNoteEntry note, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(note);

        if (_notes.ContainsKey(note.Id))
            throw new InvalidOperationException($"Note {note.Id} already exists in codex");

        _notes[note.Id] = note;
        LastUpdated = occurredAt;
    }

    /// <summary>
    /// Edits an existing note
    /// </summary>
    public void EditNote(Guid noteId, string newContent, DateTime occurredAt)
    {
        if (!_notes.TryGetValue(noteId, out CodexNoteEntry? note))
            throw new InvalidOperationException($"Note {noteId} not found in codex");

        note.UpdateContent(newContent, occurredAt);
        LastUpdated = occurredAt;
    }

    /// <summary>
    /// Deletes a note from the codex
    /// </summary>
    public void DeleteNote(Guid noteId, DateTime occurredAt)
    {
        if (!_notes.Remove(noteId))
            throw new InvalidOperationException($"Note {noteId} not found in codex");

        LastUpdated = occurredAt;
    }

    /// <summary>
    /// Gets a note by ID
    /// </summary>
    public CodexNoteEntry? GetNote(Guid noteId) => _notes.GetValueOrDefault(noteId);

    /// <summary>
    /// Checks if a note exists
    /// </summary>
    public bool HasNote(Guid noteId) => _notes.ContainsKey(noteId);

    #endregion

    #region Reputation Commands

    /// <summary>
    /// Records a reputation change with a faction
    /// </summary>
    public void RecordReputationChange(FactionId factionId, string factionName, int delta, string reason, DateTime occurredAt)
    {
        if (!_reputations.TryGetValue(factionId, out FactionReputation? reputation))
        {
            // First interaction with this faction
            reputation = new FactionReputation(ReputationScore.CreateNeutral(), occurredAt)
            {
                FactionId = factionId,
                FactionName = factionName,
                DateEstablished = occurredAt
            };
            _reputations[factionId] = reputation;
        }

        reputation.AdjustReputation(delta, reason, occurredAt);
        LastUpdated = occurredAt;
    }

    /// <summary>
    /// Gets reputation with a faction
    /// </summary>
    public FactionReputation? GetReputation(FactionId factionId) => _reputations.GetValueOrDefault(factionId);

    /// <summary>
    /// Checks if reputation exists with a faction
    /// </summary>
    public bool HasReputation(FactionId factionId) => _reputations.ContainsKey(factionId);

    #endregion

    #region Query Methods

    /// <summary>
    /// Gets all quests in a specific state
    /// </summary>
    public IEnumerable<CodexQuestEntry> GetQuestsByState(QuestState state) =>
        _quests.Values.Where(q => q.State == state);

    /// <summary>
    /// Gets all lore of a specific tier
    /// </summary>
    public IEnumerable<CodexLoreEntry> GetLoreByTier(LoreTier tier) =>
        _lore.Values.Where(l => l.Tier == tier);

    /// <summary>
    /// Gets all notes in a specific category
    /// </summary>
    public IEnumerable<CodexNoteEntry> GetNotesByCategory(NoteCategory category) =>
        _notes.Values.Where(n => n.Category == category);

    /// <summary>
    /// Searches quests by search term
    /// </summary>
    public IEnumerable<CodexQuestEntry> SearchQuests(string searchTerm) =>
        _quests.Values.Where(q => q.MatchesSearch(searchTerm));

    /// <summary>
    /// Searches lore by search term
    /// </summary>
    public IEnumerable<CodexLoreEntry> SearchLore(string searchTerm) =>
        _lore.Values.Where(l => l.MatchesSearch(searchTerm));

    /// <summary>
    /// Searches notes by search term
    /// </summary>
    public IEnumerable<CodexNoteEntry> SearchNotes(string searchTerm) =>
        _notes.Values.Where(n => n.MatchesSearch(searchTerm));

    /// <summary>
    /// Gets total count of all codex entries
    /// </summary>
    public int GetTotalEntryCount() => _quests.Count + _lore.Count + _notes.Count + _reputations.Count;

    #endregion
}
