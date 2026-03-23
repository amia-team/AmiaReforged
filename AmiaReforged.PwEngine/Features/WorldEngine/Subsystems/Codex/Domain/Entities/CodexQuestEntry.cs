using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;

// ReSharper disable MemberCanBePrivate.Global

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
    /// The numeric stage ID the player has reached (0 = not started/no stage set).
    /// Mirrors the NWN journal stage system — quest definitions assign IDs like 10, 20, 30.
    /// </summary>
    public int CurrentStageId { get; private set; }

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
    /// Ordered stages that define the quest's structure.
    /// Each stage owns its own objective groups, hints, journal text, and rewards.
    /// Empty for legacy quests that don't use the stage system.
    /// </summary>
    public List<QuestStage> Stages { get; init; } = new();

    /// <summary>
    /// Reward granted when the quest reaches a terminal completion state,
    /// in addition to any per-stage rewards already collected.
    /// </summary>
    public RewardMix CompletionReward { get; init; } = RewardMix.Empty;

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

    /// <summary>
    /// Advances the quest to the given stage ID.
    /// Stage must be greater than the current stage (no going backwards).
    /// </summary>
    public void AdvanceToStage(int stageId)
    {
        if (State is QuestState.Completed or QuestState.Failed or QuestState.Abandoned)
            throw new InvalidOperationException($"Cannot advance stage on quest in state {State}");

        if (stageId <= CurrentStageId)
            throw new InvalidOperationException(
                $"Cannot advance to stage {stageId} — current stage is {CurrentStageId}");

        CurrentStageId = stageId;

        // Automatically move to InProgress if still in Discovered state
        if (State == QuestState.Discovered)
            State = QuestState.InProgress;
    }
}
