using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Events;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Integration.Tests;

[TestFixture]
public class GlyphInteractionHookServiceTests
{
    private InMemoryGlyphRepository _repository = null!;
    private GlyphBootstrap _bootstrap = null!;
    private GlyphInteractionHookService _hookService = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryGlyphRepository();

        // GlyphBootstrap registers all node definitions & creates the interpreter
        GlyphNodeDefinitionRegistry registry = new GlyphNodeDefinitionRegistry();
        _bootstrap = new GlyphBootstrap(registry);
    }

    // ==================== OnInteractionAttempted ====================

    [Test]
    public void Attempted_stage_with_fail_node_should_block_interaction()
    {
        // Given: a pipeline graph where the Attempted stage routes to FailInteraction
        GlyphGraph graph = BuildFailAtAttemptedGraph("Custom block message");
        RegisterBinding("prospecting", graph);
        CreateHookService();

        // When
        (bool shouldBlock, string? message) = _hookService.RunOnInteractionAttempted(
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
    public void Attempted_stage_without_fail_node_should_allow_interaction()
    {
        // Given: a pipeline graph with no fail action wired to Attempted
        GlyphGraph graph = BuildPassthroughPipelineGraph();
        RegisterBinding("prospecting", graph);
        CreateHookService();

        // When
        (bool shouldBlock, string? message) = _hookService.RunOnInteractionAttempted(
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
        GlyphGraph graph = BuildFailAtAttemptedGraph("Blocked!");
        RegisterBinding("mining", graph);
        CreateHookService();

        // When
        (bool shouldBlock, _) = _hookService.RunOnInteractionAttempted(
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
    public void Tick_stage_with_fail_node_should_cancel_interaction()
    {
        // Given: a pipeline graph where Tick stage routes to FailInteraction
        GlyphGraph graph = BuildFailAtTickGraph("Script cancelled it");
        RegisterBinding("prospecting", graph);
        CreateHookService();

        // When
        (bool shouldCancel, string? message) = _hookService.RunOnInteractionTick(
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
        (bool shouldCancel, _) = _hookService.RunOnInteractionTick(
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
        GlyphGraph graph = BuildFailAtAttemptedGraph("Global block");
        RegisterBinding("prospecting", graph, areaResRef: null);
        CreateHookService();

        // When: attempting from a specific area
        (bool shouldBlock, string? message) = _hookService.RunOnInteractionAttempted(
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
        GlyphGraph graph = BuildFailAtAttemptedGraph("Area-specific block");
        RegisterBinding("prospecting", graph, areaResRef: "area_mining_camp");
        CreateHookService();

        // When: attempting from the correct area
        (bool shouldBlock, _) = _hookService.RunOnInteractionAttempted(
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
        GlyphGraph graph = BuildFailAtAttemptedGraph("Should not fire");
        RegisterBinding("prospecting", graph, areaResRef: "area_mining_camp");
        CreateHookService();

        // When: attempting from a different area
        (bool shouldBlock, _) = _hookService.RunOnInteractionAttempted(
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
    public async Task OnStarted_event_executes_matching_pipeline()
    {
        // Given: a passthrough pipeline graph
        GlyphGraph graph = BuildPassthroughPipelineGraph();
        RegisterBinding("prospecting", graph);
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
    public async Task OnCompleted_event_executes_matching_pipeline()
    {
        // Given
        GlyphGraph graph = BuildPassthroughPipelineGraph();
        RegisterBinding("prospecting", graph);
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
        GlyphGraph graph = BuildPassthroughPipelineGraph();
        RegisterBinding("mining", graph);
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

        (bool shouldBlock, _) = _hookService.RunOnInteractionAttempted(
            "prospecting", Guid.NewGuid().ToString(), Guid.NewGuid(),
            "Node", null, null, null);
        shouldBlock.Should().BeFalse("no bindings initially");

        // When: add a binding and refresh
        GlyphGraph graph = BuildFailAtAttemptedGraph("Newly added");
        RegisterBinding("prospecting", graph);
        await _hookService.RefreshCacheAsync();

        // Then
        (bool shouldBlockNow, string? message) = _hookService.RunOnInteractionAttempted(
            "prospecting", Guid.NewGuid().ToString(), Guid.NewGuid(),
            "Node", null, null, null);
        shouldBlockNow.Should().BeTrue();
        message.Should().Be("Newly added");
    }

    [Test]
    public void Inactive_definitions_are_excluded_from_cache()
    {
        // Given: an inactive definition
        GlyphGraph graph = BuildFailAtAttemptedGraph("Should not fire");
        RegisterBinding("prospecting", graph, isActive: false);
        CreateHookService();

        // When
        (bool shouldBlock, _) = _hookService.RunOnInteractionAttempted(
            "prospecting", Guid.NewGuid().ToString(), Guid.NewGuid(),
            "Node", null, null, null);

        // Then
        shouldBlock.Should().BeFalse();
    }

    [Test]
    public void Attempted_stage_does_not_affect_started_stage()
    {
        // Given: a pipeline graph where Started stage has a FailInteraction wired to its exec_out.
        // Since stages no longer have exec_in, there is no auto-chain from Attempted → Started.
        // Running the Attempted stage should NOT execute anything in the Started stage's chain.
        GlyphNodeInstance attemptedNode = new()
        {
            TypeId = InteractionAttemptedStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance startedNode = new()
        {
            TypeId = InteractionStartedStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance tickNode = new()
        {
            TypeId = InteractionTickStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance completedNode = new()
        {
            TypeId = InteractionCompletedStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance failNode = new()
        {
            TypeId = FailInteractionExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid(),
            PropertyOverrides = new Dictionary<string, string> { ["message"] = "Started fail" }
        };

        GlyphGraph graph = new()
        {
            EventType = GlyphEventType.InteractionPipeline,
            Name = "Isolation Test Graph",
            Nodes = [attemptedNode, startedNode, tickNode, completedNode, failNode],
            Edges =
            [
                // Started → FailInteraction (should only fire when Started is actually triggered)
                new GlyphEdge
                {
                    SourceNodeId = startedNode.InstanceId,
                    SourcePinId = "exec_out",
                    TargetNodeId = failNode.InstanceId,
                    TargetPinId = "exec_in"
                },
            ]
        };

        RegisterBinding("prospecting", graph);
        CreateHookService();

        // When: running the Attempted stage
        (bool shouldBlock, string? message) = _hookService.RunOnInteractionAttempted(
            interactionTag: "prospecting",
            characterId: Guid.NewGuid().ToString(),
            targetId: Guid.NewGuid(),
            targetMode: "Node",
            areaResRef: null,
            proficiency: null,
            metadata: null);

        // Then: the fail node on Started should NOT have fired
        shouldBlock.Should().BeFalse("the Attempted stage has no outgoing connections");
        message.Should().BeNull();
    }

    // ==================== Graph Builder Helpers ====================

    /// <summary>
    /// Builds a pipeline graph where the Attempted stage routes to FailInteraction(message).
    /// All 4 stage nodes are present (as required by the pipeline model).
    /// </summary>
    private static GlyphGraph BuildFailAtAttemptedGraph(string message)
    {
        GlyphNodeInstance attemptedNode = new()
        {
            TypeId = InteractionAttemptedStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance startedNode = new()
        {
            TypeId = InteractionStartedStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance tickNode = new()
        {
            TypeId = InteractionTickStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance completedNode = new()
        {
            TypeId = InteractionCompletedStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance failNode = new()
        {
            TypeId = FailInteractionExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid(),
            PropertyOverrides = new Dictionary<string, string> { ["message"] = message }
        };

        return new GlyphGraph
        {
            EventType = GlyphEventType.InteractionPipeline,
            Name = "Fail-at-Attempted Test Graph",
            Nodes = [attemptedNode, startedNode, tickNode, completedNode, failNode],
            Edges =
            [
                new GlyphEdge
                {
                    SourceNodeId = attemptedNode.InstanceId,
                    SourcePinId = "exec_out",
                    TargetNodeId = failNode.InstanceId,
                    TargetPinId = "exec_in"
                }
            ]
        };
    }

    /// <summary>
    /// Builds a pipeline graph where the Tick stage routes to FailInteraction(message).
    /// </summary>
    private static GlyphGraph BuildFailAtTickGraph(string message)
    {
        GlyphNodeInstance attemptedNode = new()
        {
            TypeId = InteractionAttemptedStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance startedNode = new()
        {
            TypeId = InteractionStartedStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance tickNode = new()
        {
            TypeId = InteractionTickStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance completedNode = new()
        {
            TypeId = InteractionCompletedStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance failNode = new()
        {
            TypeId = FailInteractionExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid(),
            PropertyOverrides = new Dictionary<string, string> { ["message"] = message }
        };

        return new GlyphGraph
        {
            EventType = GlyphEventType.InteractionPipeline,
            Name = "Fail-at-Tick Test Graph",
            Nodes = [attemptedNode, startedNode, tickNode, completedNode, failNode],
            Edges =
            [
                new GlyphEdge
                {
                    SourceNodeId = tickNode.InstanceId,
                    SourcePinId = "exec_out",
                    TargetNodeId = failNode.InstanceId,
                    TargetPinId = "exec_in"
                }
            ]
        };
    }

    /// <summary>
    /// Builds a pipeline graph with all 4 stage nodes and auto-chained exec edges but
    /// no action nodes wired. Tests that graphs execute without side effects.
    /// </summary>
    private static GlyphGraph BuildPassthroughPipelineGraph()
    {
        GlyphNodeInstance attempted = new()
        {
            TypeId = InteractionAttemptedStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance started = new()
        {
            TypeId = InteractionStartedStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance tick = new()
        {
            TypeId = InteractionTickStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };
        GlyphNodeInstance completed = new()
        {
            TypeId = InteractionCompletedStageExecutor.NodeTypeId,
            InstanceId = Guid.NewGuid()
        };

        return new GlyphGraph
        {
            EventType = GlyphEventType.InteractionPipeline,
            Name = "Passthrough Pipeline",
            Nodes = [attempted, started, tick, completed],
            Edges = [] // No inter-stage edges — stages are independent entry points
        };
    }

    // ==================== Repository / Service Helpers ====================

    private void RegisterBinding(
        string interactionTag,
        GlyphGraph graph,
        string? areaResRef = null,
        int priority = 0,
        bool isActive = true)
    {
        string graphJson = GlyphGraphSerializer.Serialize(graph);
        GlyphDefinition definition = new()
        {
            Id = Guid.NewGuid(),
            Name = $"Test-Pipeline",
            EventType = GlyphEventType.InteractionPipeline.ToString(),
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
        _hookService = new GlyphInteractionHookService(_bootstrap, _repository, new InteractionSessionManager(), new NullWorldEngineApi());
    }

    // ==================== Stub World Engine API ====================

    private class NullWorldEngineApi : IGlyphWorldEngineApi
    {
        public List<IndustryMembershipInfo> GetIndustryMemberships(Guid characterId) => [];
        public ProficiencyLevel? GetIndustryLevel(Guid characterId, string industryTag) => null;
        public bool IsIndustryMember(Guid characterId, string industryTag) => false;
        public List<string> GetLearnedKnowledgeTags(Guid characterId) => [];
        public bool HasKnowledge(Guid characterId, string knowledgeTag) => false;
        public bool HasUnlockedInteraction(Guid characterId, string interactionTag) => false;
        public KnowledgeProgressionInfo GetKnowledgeProgression(Guid characterId) => new(0, 0, 0, 0);
        public SpawnResourceNodeResult? SpawnResourceNode(uint triggerHandle) => null;
        public string? GetResourceNodeType(uint objectHandle) => null;
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
