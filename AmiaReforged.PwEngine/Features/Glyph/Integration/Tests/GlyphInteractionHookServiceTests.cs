using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Events;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Integration.Tests;

[TestFixture]
public class GlyphInteractionHookServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private InMemoryGlyphRepository _repository = null!;
    private GlyphBootstrap _bootstrap = null!;
    private GlyphInteractionHookService _hookService = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryGlyphRepository();

        // GlyphBootstrap registers all node definitions & creates the interpreter
        var registry = new GlyphNodeDefinitionRegistry();
        _bootstrap = new GlyphBootstrap(registry);
    }

    // ==================== OnInteractionAttempted ====================

    [Test]
    public void Attempted_graph_with_block_node_should_block_interaction()
    {
        // Given: a graph that blocks interactions via BlockInteraction
        GlyphGraph graph = BuildBlockInteractionGraph("Custom block message");
        RegisterBinding("prospecting", GlyphEventType.OnInteractionAttempted, graph);
        CreateHookService();

        // When
        var (shouldBlock, message) = _hookService.RunOnInteractionAttempted(
            interactionTag: "prospecting",
            characterId: Guid.NewGuid().ToString(),
            targetId: Guid.NewGuid(),
            targetMode: "Node",
            areaResRef: null,
            proficiency: null,
            metadata: null);

        // Then
        shouldBlock.Should().BeTrue();
        message.Should().Be("Custom block message");
    }

    [Test]
    public void Attempted_graph_without_block_node_should_allow_interaction()
    {
        // Given: a graph that has an entry point but no block action
        GlyphGraph graph = BuildPassthroughGraph(GlyphEventType.OnInteractionAttempted);
        RegisterBinding("prospecting", GlyphEventType.OnInteractionAttempted, graph);
        CreateHookService();

        // When
        var (shouldBlock, message) = _hookService.RunOnInteractionAttempted(
            interactionTag: "prospecting",
            characterId: Guid.NewGuid().ToString(),
            targetId: Guid.NewGuid(),
            targetMode: "Node",
            areaResRef: null,
            proficiency: null,
            metadata: null);

        // Then
        shouldBlock.Should().BeFalse();
        message.Should().BeNull();
    }

    [Test]
    public void Attempted_with_no_matching_tag_should_allow_interaction()
    {
        // Given: a binding for "mining" but we attempt "prospecting"
        GlyphGraph graph = BuildBlockInteractionGraph("Blocked!");
        RegisterBinding("mining", GlyphEventType.OnInteractionAttempted, graph);
        CreateHookService();

        // When
        var (shouldBlock, _) = _hookService.RunOnInteractionAttempted(
            interactionTag: "prospecting",
            characterId: Guid.NewGuid().ToString(),
            targetId: Guid.NewGuid(),
            targetMode: "Node",
            areaResRef: null,
            proficiency: null,
            metadata: null);

        // Then
        shouldBlock.Should().BeFalse();
    }

    // ==================== OnInteractionTick ====================

    [Test]
    public void Tick_graph_with_cancel_node_should_cancel_interaction()
    {
        // Given: a graph that cancels interactions via CancelInteraction
        GlyphGraph graph = BuildCancelInteractionGraph("Script cancelled it");
        RegisterBinding("prospecting", GlyphEventType.OnInteractionTick, graph);
        CreateHookService();

        // When
        var (shouldCancel, message) = _hookService.RunOnInteractionTick(
            interactionTag: "prospecting",
            characterId: Guid.NewGuid().ToString(),
            targetId: Guid.NewGuid(),
            areaResRef: null,
            sessionId: Guid.NewGuid(),
            progress: 2,
            requiredRounds: 5,
            proficiency: null,
            metadata: null);

        // Then
        shouldCancel.Should().BeTrue();
        message.Should().Be("Script cancelled it");
    }

    [Test]
    public void Tick_with_no_matching_bindings_should_continue()
    {
        // Given: no bindings at all
        CreateHookService();

        // When
        var (shouldCancel, _) = _hookService.RunOnInteractionTick(
            interactionTag: "prospecting",
            characterId: Guid.NewGuid().ToString(),
            targetId: Guid.NewGuid(),
            areaResRef: null,
            sessionId: Guid.NewGuid(),
            progress: 1,
            requiredRounds: 3,
            proficiency: null,
            metadata: null);

        // Then
        shouldCancel.Should().BeFalse();
    }

    // ==================== Area Scoping ====================

    [Test]
    public void Global_binding_matches_any_area()
    {
        // Given: a binding with no area restriction
        GlyphGraph graph = BuildBlockInteractionGraph("Global block");
        RegisterBinding("prospecting", GlyphEventType.OnInteractionAttempted, graph, areaResRef: null);
        CreateHookService();

        // When: attempting from a specific area
        var (shouldBlock, message) = _hookService.RunOnInteractionAttempted(
            interactionTag: "prospecting",
            characterId: Guid.NewGuid().ToString(),
            targetId: Guid.NewGuid(),
            targetMode: "Node",
            areaResRef: "area_mining_camp",
            proficiency: null,
            metadata: null);

        // Then
        shouldBlock.Should().BeTrue();
        message.Should().Be("Global block");
    }

    [Test]
    public void Area_scoped_binding_matches_only_correct_area()
    {
        // Given: a binding scoped to "area_mining_camp"
        GlyphGraph graph = BuildBlockInteractionGraph("Area-specific block");
        RegisterBinding("prospecting", GlyphEventType.OnInteractionAttempted, graph, areaResRef: "area_mining_camp");
        CreateHookService();

        // When: attempting from the correct area
        var (shouldBlock, _) = _hookService.RunOnInteractionAttempted(
            interactionTag: "prospecting",
            characterId: Guid.NewGuid().ToString(),
            targetId: Guid.NewGuid(),
            targetMode: "Node",
            areaResRef: "area_mining_camp",
            proficiency: null,
            metadata: null);

        // Then
        shouldBlock.Should().BeTrue();
    }

    [Test]
    public void Area_scoped_binding_does_not_match_different_area()
    {
        // Given: a binding scoped to "area_mining_camp"
        GlyphGraph graph = BuildBlockInteractionGraph("Should not fire");
        RegisterBinding("prospecting", GlyphEventType.OnInteractionAttempted, graph, areaResRef: "area_mining_camp");
        CreateHookService();

        // When: attempting from a different area
        var (shouldBlock, _) = _hookService.RunOnInteractionAttempted(
            interactionTag: "prospecting",
            characterId: Guid.NewGuid().ToString(),
            targetId: Guid.NewGuid(),
            targetMode: "Node",
            areaResRef: "area_forest",
            proficiency: null,
            metadata: null);

        // Then
        shouldBlock.Should().BeFalse();
    }

    // ==================== Async Event Handlers ====================

    [Test]
    public async Task OnStarted_event_executes_matching_graphs()
    {
        // Given: a passthrough graph bound to OnInteractionStarted
        GlyphGraph graph = BuildPassthroughGraph(GlyphEventType.OnInteractionStarted);
        RegisterBinding("prospecting", GlyphEventType.OnInteractionStarted, graph);
        CreateHookService();

        // When: publishing the started event — should not throw
        InteractionStartedEvent started = new(
            SessionId: Guid.NewGuid(),
            CharacterId: Guid.NewGuid(),
            InteractionTag: "prospecting",
            TargetId: Guid.NewGuid(),
            RequiredRounds: 3,
            OccurredAt: DateTime.UtcNow);

        Func<Task> act = async () => await _hookService.HandleAsync(started);

        // Then: should complete without error
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task OnCompleted_event_executes_matching_graphs()
    {
        // Given
        GlyphGraph graph = BuildPassthroughGraph(GlyphEventType.OnInteractionCompleted);
        RegisterBinding("prospecting", GlyphEventType.OnInteractionCompleted, graph);
        CreateHookService();

        // When
        InteractionCompletedEvent completed = new(
            SessionId: Guid.NewGuid(),
            CharacterId: Guid.NewGuid(),
            InteractionTag: "prospecting",
            TargetId: Guid.NewGuid(),
            Success: true,
            Message: "Completed",
            OccurredAt: DateTime.UtcNow);

        Func<Task> act = async () => await _hookService.HandleAsync(completed);

        // Then
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task OnStarted_event_with_no_matching_tag_does_nothing()
    {
        // Given: binding for 'mining', event for 'prospecting'
        GlyphGraph graph = BuildPassthroughGraph(GlyphEventType.OnInteractionStarted);
        RegisterBinding("mining", GlyphEventType.OnInteractionStarted, graph);
        CreateHookService();

        InteractionStartedEvent started = new(
            SessionId: Guid.NewGuid(),
            CharacterId: Guid.NewGuid(),
            InteractionTag: "prospecting",
            TargetId: Guid.NewGuid(),
            RequiredRounds: 3,
            OccurredAt: DateTime.UtcNow);

        // When/Then: should complete without error (no graphs to match)
        Func<Task> act = async () => await _hookService.HandleAsync(started);
        await act.Should().NotThrowAsync();
    }

    // ==================== Cache Refresh ====================

    [Test]
    public async Task RefreshCache_picks_up_new_bindings()
    {
        // Given: initially empty
        CreateHookService();

        var (shouldBlock, _) = _hookService.RunOnInteractionAttempted(
            "prospecting", Guid.NewGuid().ToString(), Guid.NewGuid(),
            "Node", null, null, null);
        shouldBlock.Should().BeFalse("no bindings initially");

        // When: add a binding and refresh
        GlyphGraph graph = BuildBlockInteractionGraph("Newly added");
        RegisterBinding("prospecting", GlyphEventType.OnInteractionAttempted, graph);
        await _hookService.RefreshCacheAsync();

        // Then
        var (shouldBlockNow, message) = _hookService.RunOnInteractionAttempted(
            "prospecting", Guid.NewGuid().ToString(), Guid.NewGuid(),
            "Node", null, null, null);
        shouldBlockNow.Should().BeTrue();
        message.Should().Be("Newly added");
    }

    [Test]
    public void Inactive_definitions_are_excluded_from_cache()
    {
        // Given: an inactive definition
        GlyphGraph graph = BuildBlockInteractionGraph("Should not fire");
        RegisterBinding("prospecting", GlyphEventType.OnInteractionAttempted, graph, isActive: false);
        CreateHookService();

        // When
        var (shouldBlock, _) = _hookService.RunOnInteractionAttempted(
            "prospecting", Guid.NewGuid().ToString(), Guid.NewGuid(),
            "Node", null, null, null);

        // Then
        shouldBlock.Should().BeFalse();
    }

    // ==================== Graph Builder Helpers ====================

    /// <summary>
    /// Builds a graph: OnInteractionAttempted → BlockInteraction(message)
    /// </summary>
    private static GlyphGraph BuildBlockInteractionGraph(string message)
    {
        GlyphNodeInstance entryNode = new()
        {
            TypeId = OnInteractionAttemptedEventExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };

        GlyphNodeInstance blockNode = new()
        {
            TypeId = BlockInteractionExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid(),
            PropertyOverrides = new Dictionary<string, string> { ["message"] = message }
        };

        return new GlyphGraph
        {
            EventType = GlyphEventType.OnInteractionAttempted,
            Name = "Block Test Graph",
            Nodes = [entryNode, blockNode],
            Edges =
            [
                new GlyphEdge
                {
                    SourceNodeId = entryNode.InstanceId,
                    SourcePinId = "exec_out",
                    TargetNodeId = blockNode.InstanceId,
                    TargetPinId = "exec_in"
                }
            ]
        };
    }

    /// <summary>
    /// Builds a graph: OnInteractionTick → CancelInteraction(message)
    /// </summary>
    private static GlyphGraph BuildCancelInteractionGraph(string message)
    {
        GlyphNodeInstance entryNode = new()
        {
            TypeId = OnInteractionTickEventExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };

        GlyphNodeInstance cancelNode = new()
        {
            TypeId = CancelInteractionExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid(),
            PropertyOverrides = new Dictionary<string, string> { ["message"] = message }
        };

        return new GlyphGraph
        {
            EventType = GlyphEventType.OnInteractionTick,
            Name = "Cancel Test Graph",
            Nodes = [entryNode, cancelNode],
            Edges =
            [
                new GlyphEdge
                {
                    SourceNodeId = entryNode.InstanceId,
                    SourcePinId = "exec_out",
                    TargetNodeId = cancelNode.InstanceId,
                    TargetPinId = "exec_in"
                }
            ]
        };
    }

    /// <summary>
    /// Builds a graph with just an entry node (no actions). Tests that graphs
    /// execute without side effects when no action nodes are wired.
    /// </summary>
    private static GlyphGraph BuildPassthroughGraph(GlyphEventType eventType)
    {
        string typeId = eventType switch
        {
            GlyphEventType.OnInteractionAttempted => OnInteractionAttemptedEventExecutor.NodeTypeId,
            GlyphEventType.OnInteractionStarted => OnInteractionStartedEventExecutor.NodeTypeId,
            GlyphEventType.OnInteractionTick => OnInteractionTickEventExecutor.NodeTypeId,
            GlyphEventType.OnInteractionCompleted => OnInteractionCompletedEventExecutor.NodeTypeId,
            _ => throw new ArgumentException($"Unsupported event type: {eventType}")
        };

        return new GlyphGraph
        {
            EventType = eventType,
            Name = $"Passthrough {eventType}",
            Nodes = [new GlyphNodeInstance { TypeId = typeId, InstanceId = Guid.NewGuid() }],
            Edges = []
        };
    }

    // ==================== Repository / Service Helpers ====================

    private void RegisterBinding(
        string interactionTag,
        GlyphEventType eventType,
        GlyphGraph graph,
        string? areaResRef = null,
        int priority = 0,
        bool isActive = true)
    {
        string graphJson = JsonSerializer.Serialize(graph, JsonOptions);
        GlyphDefinition definition = new()
        {
            Id = Guid.NewGuid(),
            Name = $"Test-{eventType}",
            EventType = eventType.ToString(),
            Category = "Interaction",
            GraphJson = graphJson,
            IsActive = isActive
        };

        InteractionGlyphBinding binding = new()
        {
            Id = Guid.NewGuid(),
            InteractionTag = interactionTag,
            AreaResRef = areaResRef,
            GlyphDefinitionId = definition.Id,
            GlyphDefinition = definition,
            Priority = priority
        };

        _repository.AddInteractionBinding(binding);
    }

    private void CreateHookService()
    {
        _hookService = new GlyphInteractionHookService(_bootstrap, _repository, new InteractionSessionManager());
    }

    // ==================== In-Memory Test Double ====================

    private class InMemoryGlyphRepository : IGlyphRepository
    {
        private readonly List<InteractionGlyphBinding> _interactionBindings = [];

        public void AddInteractionBinding(InteractionGlyphBinding binding)
            => _interactionBindings.Add(binding);

        public Task<List<InteractionGlyphBinding>> GetAllInteractionBindingsAsync()
            => Task.FromResult(_interactionBindings.ToList());

        public Task<List<InteractionGlyphBinding>> GetInteractionBindingsForTagAsync(string interactionTag)
            => Task.FromResult(_interactionBindings.Where(b => b.InteractionTag == interactionTag).ToList());

        public Task<List<InteractionGlyphBinding>> GetInteractionBindingsForDefinitionAsync(Guid definitionId)
            => Task.FromResult(_interactionBindings.Where(b => b.GlyphDefinitionId == definitionId).ToList());

        public Task CreateInteractionBindingAsync(InteractionGlyphBinding binding)
        {
            _interactionBindings.Add(binding);
            return Task.CompletedTask;
        }

        public Task DeleteInteractionBindingAsync(Guid id)
        {
            _interactionBindings.RemoveAll(b => b.Id == id);
            return Task.CompletedTask;
        }

        // === Stubs for other interface members (not used by this test fixture) ===
        public Task<List<GlyphDefinition>> GetAllDefinitionsAsync() => Task.FromResult(new List<GlyphDefinition>());
        public Task<GlyphDefinition?> GetDefinitionByIdAsync(Guid id) => Task.FromResult<GlyphDefinition?>(null);
        public Task CreateDefinitionAsync(GlyphDefinition definition) => Task.CompletedTask;
        public Task UpdateDefinitionAsync(GlyphDefinition definition) => Task.CompletedTask;
        public Task DeleteDefinitionAsync(Guid id) => Task.CompletedTask;
        public Task<List<SpawnProfileGlyphBinding>> GetBindingsForProfileAsync(Guid profileId) => Task.FromResult(new List<SpawnProfileGlyphBinding>());
        public Task<List<SpawnProfileGlyphBinding>> GetAllBindingsAsync() => Task.FromResult(new List<SpawnProfileGlyphBinding>());
        public Task<SpawnProfileGlyphBinding?> GetBindingByIdAsync(Guid id) => Task.FromResult<SpawnProfileGlyphBinding?>(null);
        public Task CreateBindingAsync(SpawnProfileGlyphBinding binding) => Task.CompletedTask;
        public Task DeleteBindingAsync(Guid id) => Task.CompletedTask;
        public Task<List<TraitGlyphBinding>> GetTraitBindingsForTagAsync(string traitTag) => Task.FromResult(new List<TraitGlyphBinding>());
        public Task<List<TraitGlyphBinding>> GetAllTraitBindingsAsync() => Task.FromResult(new List<TraitGlyphBinding>());
        public Task CreateTraitBindingAsync(TraitGlyphBinding binding) => Task.CompletedTask;
        public Task DeleteTraitBindingAsync(Guid id) => Task.CompletedTask;
        public Task<List<SpawnProfileGlyphBinding>> GetSpawnBindingsForDefinitionAsync(Guid definitionId) => Task.FromResult(new List<SpawnProfileGlyphBinding>());
        public Task<List<TraitGlyphBinding>> GetTraitBindingsForDefinitionAsync(Guid definitionId) => Task.FromResult(new List<TraitGlyphBinding>());
    }
}
