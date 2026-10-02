using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;

/// <summary>
/// Aggregate root representing a complete dialogue conversation tree.
/// Contains all nodes, choices, conditions, and actions defining an NPC conversation.
/// </summary>
public sealed class DialogueTree
{
    /// <summary>
    /// Unique identifier for this dialogue tree (natural key).
    /// </summary>
    public DialogueTreeId Id { get; init; }

    /// <summary>
    /// Display title for admin panel identification.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Designer notes / description of what this dialogue does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the root node where the conversation starts.
    /// </summary>
    public DialogueNodeId RootNodeId { get; set; }

    /// <summary>
    /// Default NPC tag (creature tag/resref) that speaks this dialogue.
    /// Individual nodes can override this via <see cref="DialogueNode.SpeakerTag"/>.
    /// </summary>
    public string? SpeakerTag { get; set; }

    /// <summary>
    /// All nodes in the tree. The tree owns its nodes.
    /// </summary>
    public List<DialogueNode> Nodes { get; init; } = [];

    /// <summary>
    /// When this tree was first created (UTC).
    /// </summary>
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When this tree was last modified (UTC).
    /// </summary>
    public DateTime? UpdatedUtc { get; set; }

    // ──────────────────── Query helpers ────────────────────

    /// <summary>
    /// Finds a node by its ID, or null if not found.
    /// </summary>
    public DialogueNode? FindNode(DialogueNodeId nodeId) =>
        Nodes.Find(n => n.Id == nodeId);

    /// <summary>
    /// Gets the root node of this tree (the node pointed to by <see cref="RootNodeId"/>).
    /// For trees with multiple Root-type nodes, use <see cref="GetRootCandidates"/> instead.
    /// </summary>
    public DialogueNode? GetRootNode() => FindNode(RootNodeId);

    /// <summary>
    /// Gets all Root-type nodes in sort order. Used for conditional root selection — 
    /// the first root whose conditions pass becomes the NPC greeting.
    /// </summary>
    public List<DialogueNode> GetRootCandidates() =>
        Nodes.Where(n => n.Type == DialogueNodeType.Root)
             .OrderBy(n => n.SortOrder)
             .ToList();

    /// <summary>
    /// Gets all child nodes reachable from a given node via its choices.
    /// </summary>
    public List<DialogueNode> GetChildrenOf(DialogueNodeId nodeId)
    {
        DialogueNode? node = FindNode(nodeId);
        if (node == null) return [];

        return node.Choices
            .Select(c => FindNode(c.TargetNodeId))
            .Where(n => n != null)
            .Cast<DialogueNode>()
            .OrderBy(n => n.SortOrder)
            .ToList();
    }

    // ──────────────────── Mutations ────────────────────

    /// <summary>
    /// Adds a node to the tree. Validates no duplicate IDs.
    /// </summary>
    public void AddNode(DialogueNode node)
    {
        if (Nodes.Any(n => n.Id == node.Id))
            throw new InvalidOperationException($"Node with ID '{node.Id}' already exists in tree '{Id}'");

        Nodes.Add(node);
        UpdatedUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes a node and all choices pointing to it.
    /// Cannot remove the root node.
    /// </summary>
    public void RemoveNode(DialogueNodeId nodeId)
    {
        if (nodeId == RootNodeId)
            throw new InvalidOperationException("Cannot remove the root node");

        Nodes.RemoveAll(n => n.Id == nodeId);

        // Remove any choices in other nodes that point to the removed node
        foreach (DialogueNode n in Nodes)
        {
            n.Choices.RemoveAll(c => c.TargetNodeId == nodeId);
        }

        UpdatedUtc = DateTime.UtcNow;
    }

    // ──────────────────── Validation ────────────────────

    /// <summary>
    /// Validates the tree structure. Returns a list of validation errors (empty if valid).
    /// </summary>
    public List<string> Validate() => AmiaReforged.Shared.Dialogue.DialogueDefinitionValidator.Validate(
        DialogueTreeMapper.ToDto(this));
}
