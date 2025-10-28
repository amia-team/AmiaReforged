using AmiaReforged.PwEngine.Features.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Codex.Entities;

/// <summary>
/// Entity representing a quest entry in a player's codex.
/// Tracks quest state, objectives, and progression.
/// </summary>
public class CodexQuestEntry
{
    /// <summary>
    /// Unique identifier for this quest
    /// </summary>
    public required QuestId QuestId { get; init; }

    /// <summary>
    /// Display name of the quest
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Detailed description of the quest
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Current state of the quest
    /// </summary>
    public QuestState State { get; internal set; } = QuestState.Discovered;

    /// <summary>
    /// List of objectives for this quest (can be empty)
    /// </summary>
    public List<string> Objectives { get; init; } = new();

    /// <summary>
    /// When the quest was first discovered/started
    /// </summary>
    public DateTime DateStarted { get; init; }

    /// <summary>
    /// When the quest reached a terminal state (Completed/Failed/Abandoned)
    /// </summary>
    public DateTime? DateCompleted { get; private set; }

    /// <summary>
    /// Optional giver/source of the quest
    /// </summary>
    public string? QuestGiver { get; init; }

    /// <summary>
    /// Optional location where quest was acquired
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Keywords for searching/filtering
    /// </summary>
    public List<Keyword> Keywords { get; init; } = new();

    /// <summary>
    /// Updates the quest state to completed
    /// </summary>
    public void MarkCompleted(DateTime completedAt)
    {
        if (State is QuestState.Completed or QuestState.Failed or QuestState.Abandoned)
            throw new InvalidOperationException($"Cannot complete quest in state {State}");

        State = QuestState.Completed;
        DateCompleted = completedAt;
    }

    /// <summary>
    /// Updates the quest state to failed
    /// </summary>
    public void MarkFailed(DateTime failedAt)
    {
        if (State is QuestState.Completed or QuestState.Failed or QuestState.Abandoned)
            throw new InvalidOperationException($"Cannot fail quest in state {State}");

        State = QuestState.Failed;
        DateCompleted = failedAt;
    }

    /// <summary>
    /// Updates the quest state to abandoned
    /// </summary>
    public void MarkAbandoned(DateTime abandonedAt)
    {
        if (State is QuestState.Completed or QuestState.Failed or QuestState.Abandoned)
            throw new InvalidOperationException($"Cannot abandon quest in state {State}");

        State = QuestState.Abandoned;
        DateCompleted = abandonedAt;
    }

    /// <summary>
    /// Checks if the quest matches any of the provided search terms
    /// </summary>
    public bool MatchesSearch(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return false;

        string lowerSearch = searchTerm.ToLowerInvariant();

        return Title.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ||
               Description.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ||
               (QuestGiver?.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (Location?.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
               Keywords.Any(k => k.Matches(searchTerm));
    }
}
