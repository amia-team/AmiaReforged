using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Tests;

/// <summary>
/// Focused unit tests for duplicate-node-ID detection in <see cref="DialogueTree.Validate"/>.
///
/// Nodes are placed directly into <see cref="DialogueTree.Nodes"/> (bypassing
/// <see cref="DialogueTree.AddNode"/>), which is the scenario the duplicate check must
/// still catch. Each test isolates the duplicate-ID invariant: the tree is otherwise
/// structurally valid so the only error produced is the duplicate-ID error itself.
/// </summary>
[TestFixture]
public class DialogueTreeValidationTests
{
    private static Guid Id(string name) => Guid.NewGuid();

    private static DialogueNode Node(Guid id, DialogueNodeType type = DialogueNodeType.End) =>
        new() { Id = new DialogueNodeId(id), Type = type };

    private static DialogueChoice Choice(Guid target) => new() { TargetNodeId = new DialogueNodeId(target) };

    private static DialogueNode RootNode(Guid id, Guid target)
    {
        DialogueNode node = Node(id, DialogueNodeType.Root);
        node.Choices.Add(Choice(target));
        return node;
    }

    /// <summary>
    /// A structurally valid minimal tree: a Root node with one choice pointing to an End node.
    /// </summary>
    private static DialogueTree ValidTree(Guid rootId, Guid otherId, string title = "valid") =>
        new()
        {
            Id = DialogueTreeId.NewId(),
            Title = title,
            RootNodeId = new DialogueNodeId(rootId),
            Nodes = new List<DialogueNode>
            {
                RootNode(rootId, otherId),
                Node(otherId),
            },
        };

    // 1. Two nodes sharing an ID: exactly one duplicate-ID error, including that ID.

    [Test]
    public void TwoNodes_SharingId_ReportsExactlyOneDuplicateError_WithTheId()
    {
        Guid shared = Id("dup");
        var tree = new DialogueTree
        {
            Id = DialogueTreeId.NewId(),
            Title = "dup",
            RootNodeId = new DialogueNodeId(shared),
            Nodes = new List<DialogueNode>
            {
                RootNode(shared, shared),
                Node(shared),
            },
        };

        List<string> errors = tree.Validate();

        int duplicateErrors = errors.Count(e => e.Contains("Duplicate node ID"));
        Assert.That(duplicateErrors, Is.EqualTo(1), "Exactly one duplicate-ID error is reported");
        Assert.That(errors, Contains.Item($"Duplicate node ID '{shared}' is used by 2 nodes"),
            "The duplicated ID is included in the error message");
    }

    // 2. Three nodes sharing an ID: still exactly one duplicate-ID error.

    [Test]
    public void ThreeNodes_SharingId_ReportsExactlyOneDuplicateError()
    {
        Guid shared = Id("trip");
        var tree = new DialogueTree
        {
            Id = DialogueTreeId.NewId(),
            Title = "trip",
            RootNodeId = new DialogueNodeId(shared),
            Nodes = new List<DialogueNode>
            {
                RootNode(shared, shared),
                Node(shared),
                Node(shared),
            },
        };

        List<string> errors = tree.Validate();

        int duplicateErrors = errors.Count(e => e.Contains("Duplicate node ID"));
        Assert.That(duplicateErrors, Is.EqualTo(1), "Exactly one duplicate-ID error is reported, even with 3 nodes");
        Assert.That(errors, Contains.Item($"Duplicate node ID '{shared}' is used by 3 nodes"),
            "The error reports the duplicated ID and the node count");
    }

    // 3a. Distinct IDs: a valid tree produces no errors (existing behavior preserved).

    [Test]
    public void DistinctIds_ValidTree_ProducesNoErrors()
    {
        var tree = ValidTree(Id("root"), Id("leaf"));

        List<string> errors = tree.Validate();

        Assert.That(errors, Is.Empty, "A tree with unique IDs passes validation unchanged");
    }

    // 3b. Distinct IDs: an existing (non-duplicate) structural error is still reported.

    [Test]
    public void DistinctIds_DanglingChoice_StillReportsExistingError_AndNoDuplicateError()
    {
        Guid rootId = Id("root");
        Guid dangling = Id("missing");
        var tree = new DialogueTree
        {
            Id = DialogueTreeId.NewId(),
            Title = "dangling",
            RootNodeId = new DialogueNodeId(rootId),
            Nodes = new List<DialogueNode>
            {
                RootNode(rootId, dangling),
                Node(Id("leaf")),
            },
        };

        List<string> errors = tree.Validate();

        Assert.That(errors, Contains.Item(
            $"Node '{rootId}' has choice targeting non-existent node '{dangling}'"),
            "Existing choice-target validation behavior is preserved for unique-ID trees");
        Assert.That(errors, Does.Not.Contain("Duplicate node ID"),
            "No duplicate-ID error appears when all IDs are distinct");
    }

    // 4. Validation must not mutate the node collection (count, IDs, or updated timestamp).

    [Test]
    public void Validate_DoesNotMutateNodeCollection()
    {
        var tree = new DialogueTree
        {
            Id = DialogueTreeId.NewId(),
            Title = "dup",
            RootNodeId = new DialogueNodeId(Id("dup")),
            Nodes = new List<DialogueNode>
            {
                RootNode(Id("dup"), Id("dup")),
                Node(Id("dup")),
                Node(Id("leaf")),
            },
        };

        int nodeCountBefore = tree.Nodes.Count;
        List<Guid> idsBefore = tree.Nodes.Select(n => n.Id.Value).ToList();
        DateTime? updatedUtcBefore = tree.UpdatedUtc;

        tree.Validate();

        Assert.That(tree.Nodes.Count, Is.EqualTo(nodeCountBefore), "Node count is unchanged");
        Assert.That(tree.Nodes.Select(n => n.Id.Value).ToList(), Is.EqualTo(idsBefore),
            "Node IDs are unchanged after validation");
        Assert.That(tree.UpdatedUtc, Is.EqualTo(updatedUtcBefore), "Validation does not touch UpdatedUtc");
    }

    // Cycles must remain supported: a node that points back to an ancestor is not an orphan error.

    [Test]
    public void Cycle_BetweenNodes_IsValid()
    {
        var tree = ValidTree(Id("root"), Id("leaf"));
        // Add a back-edge leaf -> root to form a cycle, keeping both nodes reachable.
        tree.Nodes[1].Choices.Add(Choice(tree.Nodes[0].Id.Value));

        List<string> errors = tree.Validate();

        Assert.That(errors, Is.Empty, "A cycle between nodes must remain supported");
    }

    // Multiple conditional roots must remain supported: several Root-type nodes are allowed.

    [Test]
    public void MultipleRootTypeNodes_AreValid()
    {
        Guid rootId = Id("root");
        Guid leafId = Id("leaf");
        Guid altId = Id("alt");
        var tree = new DialogueTree
        {
            Id = DialogueTreeId.NewId(),
            Title = "multiroot",
            RootNodeId = new DialogueNodeId(rootId),
            Nodes = new List<DialogueNode>
            {
                RootNode(rootId, leafId),
                Node(leafId),
                RootNode(altId, leafId),
            },
        };

        List<string> errors = tree.Validate();

        Assert.That(errors, Is.Empty, "Multiple Root-type nodes must remain supported");
    }
}
