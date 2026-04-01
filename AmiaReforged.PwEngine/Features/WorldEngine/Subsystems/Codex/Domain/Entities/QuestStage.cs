using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;

/// <summary>
/// Represents a single stage in a quest definition. Each stage owns its journal text,
/// objective groups (what the player must accomplish to advance), hints, and rewards
/// granted when the stage is reached/completed.
/// </summary>
public class QuestStage
{
    /// <summary>
    /// NWN-style numeric stage ID (e.g. 10, 20, 30). Gaps are allowed for patching.
    /// </summary>
    public int StageId { get; init; }

    /// <summary>
    /// Journal text displayed to the player when this stage is reached.
    /// </summary>
    public string JournalText { get; init; } = string.Empty;

    /// <summary>
    /// The quest state that should be applied when the player reaches this stage.
    /// For example, a final stage might set <see cref="QuestState.Completed"/>,
    /// a failure branch might set <see cref="QuestState.Failed"/>.
    /// When null, the quest state is not changed on stage entry.
    /// </summary>
    public QuestState? QuestState { get; init; }

    /// <summary>
    /// When true, reaching this stage marks the quest as completed.
    /// Kept for backward compatibility — prefer <see cref="QuestState"/> for new quests.
    /// </summary>
    public bool IsCompletionStage { get; init; }

    /// <summary>
    /// Explicit next-stage override. When all objective groups in this stage are satisfied
    /// (and no group-level <see cref="QuestObjectiveGroup.CompletionStageId"/> fires first),
    /// the quest advances to this stage. If null, falls back to the next numeric stage ID.
    /// </summary>
    public int? NextStageId { get; init; }

    /// <summary>
    /// Optional hints revealed at this stage.
    /// </summary>
    public List<string> Hints { get; init; } = [];

    /// <summary>
    /// Ordered groups of objectives that must be satisfied to advance past this stage.
    /// Empty for stages that advance on narrative triggers (dialog) rather than objectives.
    /// </summary>
    public List<QuestObjectiveGroup> ObjectiveGroups { get; init; } = [];

    /// <summary>
    /// Rewards granted when this stage's objective groups are all satisfied
    /// (or immediately on reaching the stage, if no objective groups are defined).
    /// </summary>
    public RewardMix Rewards { get; init; } = RewardMix.Empty;
}
