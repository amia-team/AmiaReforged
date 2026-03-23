using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;

/// <summary>
/// A player response option that branches to another dialogue node.
/// Contains optional preconditions controlling visibility.
/// </summary>
public sealed class DialogueChoice
{
    /// <summary>
    /// The node this choice leads to when selected.
    /// </summary>
    public DialogueNodeId TargetNodeId { get; init; }

    /// <summary>
    /// The text shown to the player for this response option.
    /// </summary>
    public string ResponseText { get; init; } = string.Empty;

    /// <summary>
    /// Display order among sibling choices. Lower values appear first.
    /// </summary>
    public int SortOrder { get; init; }

    /// <summary>
    /// Preconditions that must all pass for this choice to be visible.
    /// If empty, the choice is always shown.
    /// </summary>
    public List<DialogueCondition> Conditions { get; init; } = [];
}
