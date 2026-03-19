using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Integration.Tests;

[TestFixture]
public class IndustryKnowledgeNodeTests
{
    private static readonly Guid TestCharacterId = Guid.Parse("aaaa1111-2222-3333-4444-555566667777");

    // ==================== GetIndustryMembershipsExecutor ====================

    [Test]
    public async Task GetMemberships_returns_all_memberships_from_api()
    {
        var api = new StubWorldEngineApi
        {
            Memberships =
            [
                new IndustryMembershipInfo("mining", "Mining", ProficiencyLevel.Expert),
                new IndustryMembershipInfo("smithing", "Smithing", ProficiencyLevel.Novice),
            ]
        };

        var executor = new GetIndustryMembershipsExecutor();
        var node = new GlyphNodeInstance { TypeId = GetIndustryMembershipsExecutor.NodeTypeId };
        var context = CreateContext(api);

        var result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "character_id" ? TestCharacterId.ToString() : null));

        result.OutputValues!["membership_count"].Should().Be(2);
        result.OutputValues["industry_tags"].Should().Be("mining,smithing");
        result.OutputValues["primary_industry"].Should().Be("mining");
        result.OutputValues["primary_level"].Should().Be("Expert");
    }

    [Test]
    public async Task GetMemberships_returns_empty_when_no_api()
    {
        var executor = new GetIndustryMembershipsExecutor();
        var node = new GlyphNodeInstance { TypeId = GetIndustryMembershipsExecutor.NodeTypeId };
        var context = CreateContext(worldEngine: null);

        var result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "character_id" ? TestCharacterId.ToString() : null));

        result.OutputValues!["membership_count"].Should().Be(0);
        result.OutputValues["industry_tags"].Should().Be(string.Empty);
        result.OutputValues["primary_industry"].Should().Be(string.Empty);
    }

    [Test]
    public async Task GetMemberships_falls_back_to_context_character_id()
    {
        var api = new StubWorldEngineApi
        {
            Memberships = [new IndustryMembershipInfo("alchemy", "Alchemy", ProficiencyLevel.Journeyman)]
        };

        var executor = new GetIndustryMembershipsExecutor();
        var node = new GlyphNodeInstance { TypeId = GetIndustryMembershipsExecutor.NodeTypeId };
        var context = CreateContext(api);
        context.CharacterId = TestCharacterId.ToString();

        // Don't wire character_id input — should fall back to context.CharacterId
        var result = await executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.OutputValues!["membership_count"].Should().Be(1);
        result.OutputValues["primary_industry"].Should().Be("alchemy");
    }

    // ==================== GetIndustryLevelExecutor ====================

    [Test]
    public async Task GetLevel_returns_level_when_member()
    {
        var api = new StubWorldEngineApi { IndustryLevel = ProficiencyLevel.Master };

        var executor = new GetIndustryLevelExecutor();
        var node = new GlyphNodeInstance { TypeId = GetIndustryLevelExecutor.NodeTypeId };
        var context = CreateContext(api);

        var result = await executor.ExecuteAsync(node, context, pin => pin switch
        {
            "character_id" => Task.FromResult<object?>(TestCharacterId.ToString()),
            "industry_tag" => Task.FromResult<object?>("mining"),
            _ => Task.FromResult<object?>(null)
        });

        result.OutputValues!["level"].Should().Be("Master");
        result.OutputValues["level_value"].Should().Be((int)ProficiencyLevel.Master);
        result.OutputValues["is_member"].Should().Be(true);
    }

    [Test]
    public async Task GetLevel_returns_defaults_when_not_member()
    {
        var api = new StubWorldEngineApi { IndustryLevel = null };

        var executor = new GetIndustryLevelExecutor();
        var node = new GlyphNodeInstance { TypeId = GetIndustryLevelExecutor.NodeTypeId };
        var context = CreateContext(api);

        var result = await executor.ExecuteAsync(node, context, pin => pin switch
        {
            "character_id" => Task.FromResult<object?>(TestCharacterId.ToString()),
            "industry_tag" => Task.FromResult<object?>("unknown_industry"),
            _ => Task.FromResult<object?>(null)
        });

        result.OutputValues!["level"].Should().Be(string.Empty);
        result.OutputValues["level_value"].Should().Be(-1);
        result.OutputValues["is_member"].Should().Be(false);
    }

    // ==================== IsIndustryMemberExecutor ====================

    [Test]
    public async Task IsMember_returns_true_when_enrolled()
    {
        var api = new StubWorldEngineApi { IsMember = true };

        var executor = new IsIndustryMemberExecutor();
        var node = new GlyphNodeInstance { TypeId = IsIndustryMemberExecutor.NodeTypeId };
        var context = CreateContext(api);

        var result = await executor.ExecuteAsync(node, context, pin => pin switch
        {
            "character_id" => Task.FromResult<object?>(TestCharacterId.ToString()),
            "industry_tag" => Task.FromResult<object?>("mining"),
            _ => Task.FromResult<object?>(null)
        });

        result.OutputValues!["result"].Should().Be(true);
    }

    [Test]
    public async Task IsMember_returns_false_when_not_enrolled()
    {
        var api = new StubWorldEngineApi { IsMember = false };

        var executor = new IsIndustryMemberExecutor();
        var node = new GlyphNodeInstance { TypeId = IsIndustryMemberExecutor.NodeTypeId };
        var context = CreateContext(api);

        var result = await executor.ExecuteAsync(node, context, pin => pin switch
        {
            "character_id" => Task.FromResult<object?>(TestCharacterId.ToString()),
            "industry_tag" => Task.FromResult<object?>("mining"),
            _ => Task.FromResult<object?>(null)
        });

        result.OutputValues!["result"].Should().Be(false);
    }

    // ==================== HasKnowledgeExecutor ====================

    [Test]
    public async Task HasKnowledge_returns_true_when_learned()
    {
        var api = new StubWorldEngineApi { HasKnowledgeResult = true };

        var executor = new HasKnowledgeExecutor();
        var node = new GlyphNodeInstance { TypeId = HasKnowledgeExecutor.NodeTypeId };
        var context = CreateContext(api);

        var result = await executor.ExecuteAsync(node, context, pin => pin switch
        {
            "character_id" => Task.FromResult<object?>(TestCharacterId.ToString()),
            "knowledge_tag" => Task.FromResult<object?>("ore_identification"),
            _ => Task.FromResult<object?>(null)
        });

        result.OutputValues!["result"].Should().Be(true);
    }

    [Test]
    public async Task HasKnowledge_returns_false_when_not_learned()
    {
        var api = new StubWorldEngineApi { HasKnowledgeResult = false };

        var executor = new HasKnowledgeExecutor();
        var node = new GlyphNodeInstance { TypeId = HasKnowledgeExecutor.NodeTypeId };
        var context = CreateContext(api);

        var result = await executor.ExecuteAsync(node, context, pin => pin switch
        {
            "character_id" => Task.FromResult<object?>(TestCharacterId.ToString()),
            "knowledge_tag" => Task.FromResult<object?>("unknown_knowledge"),
            _ => Task.FromResult<object?>(null)
        });

        result.OutputValues!["result"].Should().Be(false);
    }

    [Test]
    public async Task HasKnowledge_returns_false_when_tag_is_empty()
    {
        var api = new StubWorldEngineApi { HasKnowledgeResult = true };

        var executor = new HasKnowledgeExecutor();
        var node = new GlyphNodeInstance { TypeId = HasKnowledgeExecutor.NodeTypeId };
        var context = CreateContext(api);

        var result = await executor.ExecuteAsync(node, context, pin => pin switch
        {
            "character_id" => Task.FromResult<object?>(TestCharacterId.ToString()),
            "knowledge_tag" => Task.FromResult<object?>(""),
            _ => Task.FromResult<object?>(null)
        });

        // Empty tag should short-circuit to false without calling the API
        result.OutputValues!["result"].Should().Be(false);
    }

    // ==================== HasUnlockedInteractionExecutor ====================

    [Test]
    public async Task HasUnlockedInteraction_returns_true_when_unlocked()
    {
        var api = new StubWorldEngineApi { HasUnlockedInteractionResult = true };

        var executor = new HasUnlockedInteractionExecutor();
        var node = new GlyphNodeInstance { TypeId = HasUnlockedInteractionExecutor.NodeTypeId };
        var context = CreateContext(api);

        var result = await executor.ExecuteAsync(node, context, pin => pin switch
        {
            "character_id" => Task.FromResult<object?>(TestCharacterId.ToString()),
            "interaction_tag" => Task.FromResult<object?>("prospecting"),
            _ => Task.FromResult<object?>(null)
        });

        result.OutputValues!["result"].Should().Be(true);
    }

    [Test]
    public async Task HasUnlockedInteraction_returns_false_when_not_unlocked()
    {
        var api = new StubWorldEngineApi { HasUnlockedInteractionResult = false };

        var executor = new HasUnlockedInteractionExecutor();
        var node = new GlyphNodeInstance { TypeId = HasUnlockedInteractionExecutor.NodeTypeId };
        var context = CreateContext(api);

        var result = await executor.ExecuteAsync(node, context, pin => pin switch
        {
            "character_id" => Task.FromResult<object?>(TestCharacterId.ToString()),
            "interaction_tag" => Task.FromResult<object?>("prospecting"),
            _ => Task.FromResult<object?>(null)
        });

        result.OutputValues!["result"].Should().Be(false);
    }

    // ==================== GetKnowledgeProgressionExecutor ====================

    [Test]
    public async Task GetProgression_returns_kp_values()
    {
        var api = new StubWorldEngineApi
        {
            Progression = new KnowledgeProgressionInfo(TotalKp: 15, EconomyKp: 10, LevelUpKp: 5, AccumulatedProgressionPoints: 350)
        };

        var executor = new GetKnowledgeProgressionExecutor();
        var node = new GlyphNodeInstance { TypeId = GetKnowledgeProgressionExecutor.NodeTypeId };
        var context = CreateContext(api);

        var result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "character_id" ? TestCharacterId.ToString() : null));

        result.OutputValues!["total_kp"].Should().Be(15);
        result.OutputValues["economy_kp"].Should().Be(10);
        result.OutputValues["levelup_kp"].Should().Be(5);
        result.OutputValues["accumulated_points"].Should().Be(350);
    }

    [Test]
    public async Task GetProgression_returns_zeros_when_no_api()
    {
        var executor = new GetKnowledgeProgressionExecutor();
        var node = new GlyphNodeInstance { TypeId = GetKnowledgeProgressionExecutor.NodeTypeId };
        var context = CreateContext(worldEngine: null);

        var result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "character_id" ? TestCharacterId.ToString() : null));

        result.OutputValues!["total_kp"].Should().Be(0);
        result.OutputValues["economy_kp"].Should().Be(0);
        result.OutputValues["levelup_kp"].Should().Be(0);
        result.OutputValues["accumulated_points"].Should().Be(0);
    }

    // ==================== GetLearnedKnowledgeExecutor ====================

    [Test]
    public async Task GetLearned_returns_tags_and_count()
    {
        var api = new StubWorldEngineApi
        {
            LearnedTags = ["ore_basics", "gem_cutting", "smelting"]
        };

        var executor = new GetLearnedKnowledgeExecutor();
        var node = new GlyphNodeInstance { TypeId = GetLearnedKnowledgeExecutor.NodeTypeId };
        var context = CreateContext(api);

        var result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "character_id" ? TestCharacterId.ToString() : null));

        result.OutputValues!["knowledge_count"].Should().Be(3);
        result.OutputValues["knowledge_tags"].Should().Be("ore_basics,gem_cutting,smelting");
    }

    [Test]
    public async Task GetLearned_returns_empty_when_no_knowledge()
    {
        var api = new StubWorldEngineApi { LearnedTags = [] };

        var executor = new GetLearnedKnowledgeExecutor();
        var node = new GlyphNodeInstance { TypeId = GetLearnedKnowledgeExecutor.NodeTypeId };
        var context = CreateContext(api);

        var result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "character_id" ? TestCharacterId.ToString() : null));

        result.OutputValues!["knowledge_count"].Should().Be(0);
        result.OutputValues["knowledge_tags"].Should().Be(string.Empty);
    }

    // ==================== Definition Tests ====================

    [Test]
    public void All_industry_knowledge_nodes_have_correct_definitions()
    {
        var defs = new GlyphNodeDefinition[]
        {
            new GetIndustryMembershipsExecutor().CreateDefinition(),
            new GetIndustryLevelExecutor().CreateDefinition(),
            new IsIndustryMemberExecutor().CreateDefinition(),
            new HasKnowledgeExecutor().CreateDefinition(),
            new HasUnlockedInteractionExecutor().CreateDefinition(),
            new GetKnowledgeProgressionExecutor().CreateDefinition(),
            new GetLearnedKnowledgeExecutor().CreateDefinition(),
        };

        foreach (var def in defs)
        {
            def.Archetype.Should().Be(GlyphNodeArchetype.PureFunction,
                $"{def.TypeId} should be PureFunction");
            def.ScriptCategory.Should().Be(GlyphScriptCategory.Interaction,
                $"{def.TypeId} should be Interaction");
            def.Category.Should().Be("Industries",
                $"{def.TypeId} should be in Industries category");

            // PureFunction nodes should have no exec pins
            def.InputPins.Should().NotContain(p => p.DataType == GlyphDataType.Exec,
                $"{def.TypeId} PureFunction should have no exec input pins");
            def.OutputPins.Should().NotContain(p => p.DataType == GlyphDataType.Exec,
                $"{def.TypeId} PureFunction should have no exec output pins");

            // All should accept character_id
            def.InputPins.Should().Contain(p => p.Id == "character_id",
                $"{def.TypeId} should accept character_id input");
        }
    }

    [Test]
    public void Industry_level_definition_accepts_industry_tag_input()
    {
        var def = new GetIndustryLevelExecutor().CreateDefinition();
        def.InputPins.Should().Contain(p => p.Id == "industry_tag");
        def.OutputPins.Should().Contain(p => p.Id == "level");
        def.OutputPins.Should().Contain(p => p.Id == "is_member");
    }

    [Test]
    public void Has_knowledge_definition_accepts_knowledge_tag_input()
    {
        var def = new HasKnowledgeExecutor().CreateDefinition();
        def.InputPins.Should().Contain(p => p.Id == "knowledge_tag");
        def.OutputPins.Should().Contain(p => p.Id == "result" && p.DataType == GlyphDataType.Bool);
    }

    // ==================== Helpers ====================

    private static GlyphExecutionContext CreateContext(IGlyphWorldEngineApi? worldEngine) => new()
    {
        Graph = new GlyphGraph { EventType = GlyphEventType.InteractionPipeline, Name = "Test" },
        WorldEngine = worldEngine,
        MaxExecutionSteps = 1000,
        EnableTracing = false
    };

    /// <summary>
    /// Stub implementation of <see cref="IGlyphWorldEngineApi"/> for testing.
    /// Returns pre-configured values regardless of the character/tag arguments.
    /// </summary>
    private class StubWorldEngineApi : IGlyphWorldEngineApi
    {
        public List<IndustryMembershipInfo> Memberships { get; init; } = [];
        public ProficiencyLevel? IndustryLevel { get; init; }
        public bool IsMember { get; init; }
        public List<string> LearnedTags { get; init; } = [];
        public bool HasKnowledgeResult { get; init; }
        public bool HasUnlockedInteractionResult { get; init; }
        public KnowledgeProgressionInfo Progression { get; init; } = new(0, 0, 0, 0);

        public List<IndustryMembershipInfo> GetIndustryMemberships(Guid characterId) => Memberships;
        public ProficiencyLevel? GetIndustryLevel(Guid characterId, string industryTag) => IndustryLevel;
        public bool IsIndustryMember(Guid characterId, string industryTag) => IsMember;
        public List<string> GetLearnedKnowledgeTags(Guid characterId) => LearnedTags;
        public bool HasKnowledge(Guid characterId, string knowledgeTag) => HasKnowledgeResult;
        public bool HasUnlockedInteraction(Guid characterId, string interactionTag) => HasUnlockedInteractionResult;
        public KnowledgeProgressionInfo GetKnowledgeProgression(Guid characterId) => Progression;
        public SpawnResourceNodeResult? SpawnResourceNode(uint triggerHandle) => null;
    }
}
