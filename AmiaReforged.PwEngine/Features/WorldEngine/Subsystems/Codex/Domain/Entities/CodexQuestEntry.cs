using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

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
    public int CurrentStageId { get; internal set; }

    /// <summary>
    /// When the quest was first discovered/started
    /// </summary>
    public DateTime DateStarted { get; init; }

    /// <summary>
    /// When the quest reached a terminal state (Completed/Failed/Abandoned)
    /// </summary>
    public DateTime? DateCompleted { get; internal set; }

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

    #region Dynamic Quest Fields

    /// <summary>
    /// Links this quest back to the dynamic quest template it was created from.
    /// Null for static (hand-crafted) quests.
    /// </summary>
    public TemplateId? SourceTemplateId { get; init; }

    /// <summary>
    /// Wall-clock deadline (UTC) by which this quest must be completed.
    /// Null means no time limit. Set when a dynamic quest is claimed.
    /// </summary>
    public DateTime? Deadline { get; init; }

    /// <summary>
    /// What happens when the <see cref="Deadline"/> elapses.
    /// Only meaningful for dynamic quests with a time limit.
    /// </summary>
    public ExpiryBehavior? ExpiryBehavior { get; init; }

    /// <summary>
    /// How many times this character has completed this quest.
    /// Used for repeatable dynamic quests to enforce <c>MaxCompletionsPerCharacter</c>.
    /// </summary>
    public int CompletionCount { get; internal set; }

    /// <summary>
    /// Whether this quest originated from the dynamic quest system.
    /// </summary>
    public bool IsDynamic => SourceTemplateId.HasValue;

    #endregion

    /// <summary>
    /// Updates the quest state to completed
    /// </summary>
    public void MarkCompleted(DateTime completedAt)
    {
        if (State is QuestState.Completed or QuestState.Failed or QuestState.Abandoned or QuestState.Expired)
            throw new InvalidOperationException($"Cannot complete quest in state {State}");

        State = QuestState.Completed;
        DateCompleted = completedAt;
    }

    /// <summary>
    /// Updates the quest state to failed
    /// </summary>
    public void MarkFailed(DateTime failedAt)
    {
        if (State is QuestState.Completed or QuestState.Failed or QuestState.Abandoned or QuestState.Expired)
            throw new InvalidOperationException($"Cannot fail quest in state {State}");

        State = QuestState.Failed;
        DateCompleted = failedAt;
    }

    /// <summary>
    /// Updates the quest state to abandoned
    /// </summary>
    public void MarkAbandoned(DateTime abandonedAt)
    {
        if (State is QuestState.Completed or QuestState.Failed or QuestState.Abandoned or QuestState.Expired)
            throw new InvalidOperationException($"Cannot abandon quest in state {State}");

        State = QuestState.Abandoned;
        DateCompleted = abandonedAt;
    }

    /// <summary>
    /// Marks the quest as expired due to its time limit elapsing.
    /// The actual effect depends on the <see cref="ExpiryBehavior"/>.
    /// </summary>
    public void MarkExpired(DateTime expiredAt)
    {
        if (State is QuestState.Completed or QuestState.Failed or QuestState.Abandoned or QuestState.Expired)
            throw new InvalidOperationException($"Cannot expire quest in state {State}");

        State = QuestState.Expired;
        DateCompleted = expiredAt;
    }

    /// <summary>
    /// Increments the completion count for repeatable dynamic quests.
    /// </summary>
    internal void IncrementCompletionCount() => CompletionCount++;

    /// <summary>
    /// Resets a dynamic quest back to Discovered state for re-acceptance.
    /// Only valid for quests in terminal states (Completed, Failed, Expired, Abandoned).
    /// </summary>
    public void ResetForReplay(DateTime resetAt)
    {
        if (State is QuestState.Discovered or QuestState.InProgress)
            throw new InvalidOperationException($"Cannot reset quest in state {State} — it must be in a terminal state");

        State = QuestState.Discovered;
        DateCompleted = null;
        CurrentStageId = 0;
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
        if (State is QuestState.Completed or QuestState.Failed or QuestState.Abandoned or QuestState.Expired)
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
