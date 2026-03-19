using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

/// <summary>
/// Tests for the SpawnResourceNode executor. Since the executor delegates all heavy logic
/// to <see cref="IGlyphWorldEngineApi.SpawnResourceNode"/>, these tests verify:
/// - Correct pin/definition shape (NwObject trigger pin, no area_resref)
/// - Graceful handling when WorldEngine is null
/// - Graceful handling when SpawnResourceNode returns null (failure)
/// - Correct unpacking of success results into output pins
/// </summary>
[TestFixture]
public class SpawnResourceNodeExecutorTests
{
    private SpawnResourceNodeExecutor _executor = null!;

    [SetUp]
    public void SetUp()
    {
        _executor = new SpawnResourceNodeExecutor();
    }

    private static GlyphNodeInstance CreateNode() => new()
    {
        TypeId = SpawnResourceNodeExecutor.NodeTypeId,
        PropertyOverrides = new Dictionary<string, string>(),
    };

    private static GlyphExecutionContext CreateContext(IGlyphWorldEngineApi? worldEngine = null) => new()
    {
        Graph = new GlyphGraph { EventType = GlyphEventType.InteractionPipeline, Name = "Test" },
        MaxExecutionSteps = 100,
        WorldEngine = worldEngine,
    };

    // ──────────────────────────────────────────────────────────────
    //  Definition shape tests
    // ──────────────────────────────────────────────────────────────

    [Test]
    public void Definition_has_correct_metadata()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();

        def.TypeId.Should().Be("action.spawn_resource_node");
        def.DisplayName.Should().Be("Spawn Resource Node");
        def.Category.Should().Be("Actions");
        def.Archetype.Should().Be(GlyphNodeArchetype.Action);
        def.ScriptCategory.Should().Be(GlyphScriptCategory.Interaction);
        def.ColorClass.Should().Be("node-action");
    }

    [Test]
    public void Definition_has_correct_input_pins()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();

        def.InputPins.Should().HaveCount(2);
        def.InputPins[0].Id.Should().Be("exec_in");
        def.InputPins[0].DataType.Should().Be(GlyphDataType.Exec);
        def.InputPins[1].Id.Should().Be("trigger");
        def.InputPins[1].DataType.Should().Be(GlyphDataType.NwObject);
    }

    [Test]
    public void Definition_has_correct_output_pins()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();

        def.OutputPins.Should().HaveCount(10);

        string[] expectedIds = ["exec_out", "success", "node_id", "node_name",
            "definition_tag", "quality", "uses", "spawn_x", "spawn_y", "spawn_z"];
        def.OutputPins.Select(p => p.Id).Should().BeEquivalentTo(expectedIds,
            options => options.WithStrictOrdering());
    }

    // ──────────────────────────────────────────────────────────────
    //  Executor behaviour tests
    // ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Null_WorldEngine_sets_success_false()
    {
        GlyphNodeInstance node = CreateNode();
        GlyphExecutionContext context = CreateContext(worldEngine: null);

        GlyphNodeResult result = await _executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin switch
            {
                "trigger" => (uint)12345,
                _ => null,
            }));

        result.NextExecPinId.Should().Be("exec_out");
        result.OutputValues.Should().NotBeNull();
        result.OutputValues!["success"].Should().Be(false);
        result.OutputValues["node_id"].Should().Be(string.Empty);
    }

    [Test]
    public async Task WorldEngine_returns_null_sets_success_false()
    {
        StubWorldEngineApi stub = new() { SpawnResult = null };
        GlyphNodeInstance node = CreateNode();
        GlyphExecutionContext context = CreateContext(worldEngine: stub);

        GlyphNodeResult result = await _executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin switch
            {
                "trigger" => (uint)99999,
                _ => null,
            }));

        result.NextExecPinId.Should().Be("exec_out");
        result.OutputValues!["success"].Should().Be(false);
        result.OutputValues["node_id"].Should().Be(string.Empty);
        result.OutputValues["node_name"].Should().Be(string.Empty);
        result.OutputValues["uses"].Should().Be(0);
    }

    [Test]
    public async Task Successful_spawn_unpacks_all_output_values()
    {
        Guid expectedId = Guid.NewGuid();
        SpawnResourceNodeResult spawnResult = new(
            NodeId: expectedId,
            Name: "Rich Copper Vein",
            DefinitionTag: "ore_vein_copper_native",
            QualityLabel: "Rich",
            Uses: 55,
            X: 10.5f,
            Y: 20.3f,
            Z: 0f);

        StubWorldEngineApi stub = new() { SpawnResult = spawnResult };
        GlyphNodeInstance node = CreateNode();
        GlyphExecutionContext context = CreateContext(worldEngine: stub);

        GlyphNodeResult result = await _executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin switch
            {
                "trigger" => (uint)42,
                _ => null,
            }));

        result.NextExecPinId.Should().Be("exec_out");
        result.OutputValues!["success"].Should().Be(true);
        result.OutputValues["node_id"].Should().Be(expectedId.ToString());
        result.OutputValues["node_name"].Should().Be("Rich Copper Vein");
        result.OutputValues["definition_tag"].Should().Be("ore_vein_copper_native");
        result.OutputValues["quality"].Should().Be("Rich");
        result.OutputValues["uses"].Should().Be(55);
        result.OutputValues["spawn_x"].Should().Be(10.5f);
        result.OutputValues["spawn_y"].Should().Be(20.3f);
        result.OutputValues["spawn_z"].Should().Be(0f);
    }

    [Test]
    public async Task Passes_trigger_handle_to_WorldEngine()
    {
        StubWorldEngineApi stub = new()
        {
            SpawnResult = new SpawnResourceNodeResult(
                Guid.NewGuid(), "Test Node", "test_tag", "Average", 50, 5f, 5f, 0f),
        };
        GlyphNodeInstance node = CreateNode();
        GlyphExecutionContext context = CreateContext(worldEngine: stub);

        await _executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin switch
            {
                "trigger" => (uint)7777,
                _ => null,
            }));

        stub.LastTriggerHandle.Should().Be((uint)7777);
    }

    // ──────────────────────────────────────────────────────────────
    //  Test stub
    // ──────────────────────────────────────────────────────────────

    private class StubWorldEngineApi : IGlyphWorldEngineApi
    {
        public SpawnResourceNodeResult? SpawnResult { get; init; }
        public uint? LastTriggerHandle { get; private set; }

        public SpawnResourceNodeResult? SpawnResourceNode(uint triggerHandle)
        {
            LastTriggerHandle = triggerHandle;
            return SpawnResult;
        }

        // Unused stubs — return defaults
        public List<IndustryMembershipInfo> GetIndustryMemberships(Guid characterId) => [];
        public ProficiencyLevel? GetIndustryLevel(Guid characterId, string industryTag) => null;
        public bool IsIndustryMember(Guid characterId, string industryTag) => false;
        public List<string> GetLearnedKnowledgeTags(Guid characterId) => [];
        public bool HasKnowledge(Guid characterId, string knowledgeTag) => false;
        public bool HasUnlockedInteraction(Guid characterId, string interactionTag) => false;
        public KnowledgeProgressionInfo GetKnowledgeProgression(Guid characterId) => new(0, 0, 0, 0);
    }
}
