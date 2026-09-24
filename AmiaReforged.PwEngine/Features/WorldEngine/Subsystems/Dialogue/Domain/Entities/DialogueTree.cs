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
    public List<string> Validate()
    {
        List<string> errors = [];

        // Must have nodes
        if (Nodes.Count == 0)
        {
            errors.Add("Dialogue tree has no nodes");
            return errors;
        }

        // Node IDs must be unique. Checked before the structural checks below so
        // duplicates are always reported, even when nodes bypass AddNode() and the
        // rest of the tree is broken. Read-only: never mutates the tree.
        errors.AddRange(DuplicateNodeIdErrors());

        // Root node must exist
        DialogueNode? root = GetRootNode();
        if (root == null)
        {
            errors.Add($"Root node '{RootNodeId}' not found in tree");
            return errors;
        }

        // Root must be of type Root
        if (root.Type != DialogueNodeType.Root)
        {
            errors.Add($"Root node must be of type Root, but is {root.Type}");
        }

        // Must have at least one Root-type node
        int rootCount = Nodes.Count(n => n.Type == DialogueNodeType.Root);
        if (rootCount < 1)
        {
            errors.Add("Tree must have at least one Root node");
        }

        // Check all choice targets point to existing nodes
        HashSet<Guid> nodeIds = Nodes.Select(n => n.Id.Value).ToHashSet();
        foreach (DialogueNode node in Nodes)
        {
            foreach (DialogueChoice choice in node.Choices)
            {
                if (!nodeIds.Contains(choice.TargetNodeId.Value))
                {
                    errors.Add(
                        $"Node '{node.Id}' has choice targeting non-existent node '{choice.TargetNodeId}'");
                }
            }
        }

        // Check for orphan nodes (not reachable from any root)
        HashSet<Guid> reachable = [];
        foreach (DialogueNode rootCandidate in Nodes.Where(n => n.Type == DialogueNodeType.Root))
        {
            CollectReachable(rootCandidate.Id, reachable, nodeIds);
        }

        List<DialogueNode> orphans = Nodes.Where(n => !reachable.Contains(n.Id.Value)).ToList();
        foreach (DialogueNode orphan in orphans)
        {
            errors.Add($"Node '{orphan.Id}' is not reachable from the root node");
        }

        // NpcText/Root nodes should have either choices or be End-type
        foreach (DialogueNode node in Nodes)
        {
            if (node.Type is DialogueNodeType.NpcText or DialogueNodeType.Root
                && node.Choices.Count == 0)
            {
                errors.Add($"NPC text node '{node.Id}' has no choices (add choices or change to End type)");
            }
        }

        // Title required
        if (string.IsNullOrWhiteSpace(Title))
        {
            errors.Add("Dialogue tree must have a title");
        }

        return errors;
    }

    /// <summary>
    /// Reports node IDs that appear more than once in <see cref="Nodes"/>. Each
    /// duplicated ID is reported exactly once, with the ID included in the message.
    /// Compares <see cref="DialogueNodeId.Value"/> so it detects duplicates created
    /// by placing nodes directly into <see cref="Nodes"/> (bypassing <see cref="AddNode"/>).
    /// </summary>
    private List<string> DuplicateNodeIdErrors()
    {
        List<string> errors = [];

        foreach (var group in Nodes.GroupBy(n => n.Id.Value))
        {
            if (group.Count() > 1)
            {
                errors.Add($"Duplicate node ID '{group.Key}' is used by {group.Count()} nodes");
            }
        }

        return errors;
    }

    private void CollectReachable(DialogueNodeId nodeId, HashSet<Guid> visited, HashSet<Guid> allNodeIds)
    {
        if (!visited.Add(nodeId.Value)) return;

        DialogueNode? node = FindNode(nodeId);
        if (node == null) return;

        foreach (DialogueChoice choice in node.Choices)
        {
            if (allNodeIds.Contains(choice.TargetNodeId.Value))
            {
                CollectReachable(choice.TargetNodeId, visited, allNodeIds);
            }
        }
    }
}
