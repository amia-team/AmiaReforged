using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;

/// <summary>
/// An ordered group of objectives within a quest, governed by a <see cref="CompletionMode"/>
/// that determines when the group is considered satisfied.
/// </summary>
public class QuestObjectiveGroup
{
    /// <summary>
    /// Display label for this group (e.g., "Find the stolen artifacts", "Investigate the crime scene").
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// How the objectives in this group must be satisfied.
    /// </summary>
    public CompletionMode CompletionMode { get; init; } = CompletionMode.All;

    /// <summary>
    /// The objectives in this group, evaluated in order.
    /// Order matters for <see cref="Enums.CompletionMode.Sequence"/> mode.
    /// </summary>
    public List<ObjectiveDefinition> Objectives { get; init; } = new();

    /// <summary>
    /// Optional stage ID to auto-advance the quest to when this group is completed.
    /// If null, no automatic stage advancement occurs.
    /// </summary>
    public int? CompletionStageId { get; init; }
}
