using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;

/// <summary>
/// A single node within a dialogue tree. Represents either NPC speech,
/// an action gate, or a termination point.
/// </summary>
public sealed class DialogueNode
{
    /// <summary>
    /// Unique identifier for this node within its tree.
    /// </summary>
    public DialogueNodeId Id { get; init; }

    /// <summary>
    /// The type of this node (Root, NpcText, Action, End).
    /// </summary>
    public DialogueNodeType Type { get; init; }

    /// <summary>
    /// Optional NPC tag override. If null, the tree's default speaker is used.
    /// Allows mid-conversation speaker switches.
    /// </summary>
    public string? SpeakerTag { get; init; }

    /// <summary>
    /// The dialogue text displayed when this node is active.
    /// For NpcText/Root nodes this is the NPC's speech.
    /// For End nodes this can be a farewell message.
    /// For Action nodes this is typically empty.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Display order among sibling nodes (used for authoring, not runtime).
    /// </summary>
    public int SortOrder { get; init; }

    /// <summary>
    /// Player response options branching from this node.
    /// Only meaningful on NpcText and Root nodes.
    /// </summary>
    public List<DialogueChoice> Choices { get; init; } = [];

    /// <summary>
    /// Actions fired when this node is entered during conversation.
    /// Executed in order of <see cref="DialogueAction.ExecutionOrder"/>.
    /// </summary>
    public List<DialogueAction> Actions { get; init; } = [];

    /// <summary>
    /// Conditions that must be met for this node to be considered.
    /// Used primarily on Root-type nodes to select which NPC greeting is shown.
    /// If empty, the node is unconditionally available.
    /// </summary>
    public List<DialogueCondition> Conditions { get; init; } = [];

    /// <summary>
    /// Optional parent node ID for tree structure (used in authoring).
    /// Null for the root node.
    /// </summary>
    public DialogueNodeId? ParentNodeId { get; init; }
}
