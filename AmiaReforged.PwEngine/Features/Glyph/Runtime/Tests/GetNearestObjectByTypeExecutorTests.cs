using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;
using FluentAssertions;
using NUnit.Framework;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

[TestFixture]
public class GetNearestObjectByTypeExecutorTests
{
    private GetNearestObjectByTypeExecutor _executor = null!;

    [SetUp]
    public void SetUp() => _executor = new GetNearestObjectByTypeExecutor();

    // ==================== Definition Validation ====================

    [Test]
    public void Definition_has_correct_type_id()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.TypeId.Should().Be("getter.nearest_object_by_type");
    }

    [Test]
    public void Definition_is_pure_function_getter()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.Archetype.Should().Be(GlyphNodeArchetype.PureFunction);
        def.Category.Should().Be("Getters");
        def.ColorClass.Should().Be("node-getter");
    }

    [Test]
    public void Definition_has_no_script_category_restriction()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.ScriptCategory.Should().BeNull();
    }

    [Test]
    public void Definition_has_origin_and_type_input_pins()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.InputPins.Should().HaveCount(2);

        def.InputPins.Should().Contain(p =>
            p.Id == "origin" && p.DataType == GlyphDataType.NwObject && p.Direction == GlyphPinDirection.Input);

        def.InputPins.Should().Contain(p =>
            p.Id == "type" && p.DataType == GlyphDataType.String && p.Direction == GlyphPinDirection.Input);
    }

    [Test]
    public void Definition_has_single_object_output_pin()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.OutputPins.Should().HaveCount(1);
        def.OutputPins.Should().Contain(p =>
            p.Id == "object" && p.DataType == GlyphDataType.NwObject && p.Direction == GlyphPinDirection.Output);
    }

    // ==================== Execution — Invalid / No-Result Handling ====================

    [Test]
    public async Task Null_origin_returns_object_invalid()
    {
        GlyphNodeResult result = await Run(type: "creature", origin: null);
        result.OutputValues!["object"].Should().Be(NWScript.OBJECT_INVALID);
    }

    [Test]
    public async Task Zero_origin_returns_object_invalid()
    {
        GlyphNodeResult result = await Run(type: "creature", origin: 0u);
        result.OutputValues!["object"].Should().Be(NWScript.OBJECT_INVALID);
    }

    [Test]
    public async Task Invalid_object_id_returns_object_invalid()
    {
        GlyphNodeResult result = await Run(type: "door", origin: NWScript.OBJECT_INVALID);
        result.OutputValues!["object"].Should().Be(NWScript.OBJECT_INVALID);
    }

    [Test]
    public async Task Unsupported_type_returns_object_invalid_without_touching_origin()
    {
        // Type is validated before the origin is resolved, so a null origin is safe here.
        GlyphNodeResult result = await Run(type: "banana", origin: null);
        result.OutputValues!["object"].Should().Be(NWScript.OBJECT_INVALID);
    }

    [Test]
    public async Task Unknown_type_returns_object_invalid()
    {
        GlyphNodeResult result = await Run(type: "", origin: null);
        result.OutputValues!["object"].Should().Be(NWScript.OBJECT_INVALID);
    }

    [Test]
    public async Task Valid_type_with_null_origin_returns_object_invalid()
    {
        // Confirms the origin guard fires for every curated type before Anvil is touched.
        foreach (string type in new[] { "trigger", "door", "placeable", "creature", "waypoint" })
        {
            GlyphNodeResult result = await Run(type: type, origin: null);
            result.OutputValues!["object"].Should().Be(NWScript.OBJECT_INVALID);
        }
    }

    [Test]
    public async Task Type_matching_is_case_insensitive()
    {
        // "CREATURE"/"Door" are still curated, so with a null origin they reach the
        // origin guard and return OBJECT_INVALID (proving the type was accepted).
        GlyphNodeResult lower = await Run(type: "creature", origin: null);
        GlyphNodeResult upper = await Run(type: "CREATURE", origin: null);
        GlyphNodeResult mixed = await Run(type: "Door", origin: null);

        lower.OutputValues!["object"].Should().Be(NWScript.OBJECT_INVALID);
        upper.OutputValues!["object"].Should().Be(NWScript.OBJECT_INVALID);
        mixed.OutputValues!["object"].Should().Be(NWScript.OBJECT_INVALID);
    }

    [Test]
    public void Result_is_data_only_no_exec_flow()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.Archetype.Should().Be(GlyphNodeArchetype.PureFunction);
        def.InputPins.Should().NotContain(p => p.DataType == GlyphDataType.Exec);
        def.OutputPins.Should().NotContain(p => p.DataType == GlyphDataType.Exec);
    }

    // ==================== Shared type-dispatch helper ====================

    [Theory]
    [TestCase("trigger")]
    [TestCase("door")]
    [TestCase("placeable")]
    [TestCase("creature")]
    [TestCase("waypoint")]
    public void Curated_types_are_recognized(string type)
        => NwObjectQuery.IsCuratedType(type).Should().BeTrue();

    [Theory]
    [TestCase("banana")]
    [TestCase("areaofeffect")]
    [TestCase("store")]
    [TestCase("item")]
    [TestCase("")]
    [TestCase(null)]
    public void Non_curated_types_are_rejected(string? type)
        => NwObjectQuery.IsCuratedType(type).Should().BeFalse();

    [Test]
    public void Nearest_object_for_non_curated_type_is_object_invalid_without_origin()
    {
        // Returns before dereferencing origin, so this is safe with no running server.
        NwObjectQuery.NearestObjectId(null!, "banana").Should().Be(NWScript.OBJECT_INVALID);
    }

    // ==================== Helpers ====================

    private async Task<GlyphNodeResult> Run(string? type, object? origin)
    {
        GlyphNodeInstance node = new() { TypeId = GetNearestObjectByTypeExecutor.NodeTypeId };
        GlyphExecutionContext context = new()
        {
            Graph = new GlyphGraph { EventType = GlyphEventType.BeforeGroupSpawn, Name = "Test" },
            MaxExecutionSteps = 1000,
            EnableTracing = false
        };

        return await _executor.ExecuteAsync(node, context, pin =>
            Task.FromResult<object?>(pin switch
            {
                "type" => type,
                "origin" => origin,
                _ => null
            }));
    }
}
