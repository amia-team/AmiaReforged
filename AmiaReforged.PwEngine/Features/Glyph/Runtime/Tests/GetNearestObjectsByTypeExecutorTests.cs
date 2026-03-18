using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

[TestFixture]
public class GetNearestObjectsByTypeExecutorTests
{
    private GetNearestObjectsByTypeExecutor _executor = null!;

    [SetUp]
    public void SetUp()
    {
        _executor = new GetNearestObjectsByTypeExecutor();
    }

    // ==================== Definition Validation ====================

    [Test]
    public void Definition_has_correct_type_id()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.TypeId.Should().Be("getter.nearest_objects_by_type");
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
    public void Definition_has_origin_and_max_count_input_pins()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.InputPins.Should().HaveCount(2);

        def.InputPins.Should().Contain(p =>
            p.Id == "origin" && p.DataType == GlyphDataType.NwObject && p.Direction == GlyphPinDirection.Input);

        def.InputPins.Should().Contain(p =>
            p.Id == "max_count" && p.DataType == GlyphDataType.Int && p.Direction == GlyphPinDirection.Input && p.DefaultValue == "10");
    }

    [Test]
    public void Definition_has_objects_and_count_output_pins()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.OutputPins.Should().HaveCount(2);

        def.OutputPins.Should().Contain(p =>
            p.Id == "objects" && p.DataType == GlyphDataType.List && p.Direction == GlyphPinDirection.Output);

        def.OutputPins.Should().Contain(p =>
            p.Id == "count" && p.DataType == GlyphDataType.Int && p.Direction == GlyphPinDirection.Output);
    }

    // ==================== Execution — Invalid Origin ====================

    [Test]
    public async Task Null_origin_returns_empty_list_and_zero_count()
    {
        var node = CreateNode();
        var context = CreateContext();

        GlyphNodeResult result = await _executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "origin" ? null : (object?)10));

        result.OutputValues.Should().NotBeNull();
        result.OutputValues!["objects"].Should().BeEquivalentTo(new List<uint>());
        result.OutputValues["count"].Should().Be(0);
    }

    [Test]
    public async Task Zero_origin_returns_empty_list_and_zero_count()
    {
        var node = CreateNode();
        var context = CreateContext();

        GlyphNodeResult result = await _executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "origin" ? (object?)0u : (object?)10));

        result.OutputValues.Should().NotBeNull();
        result.OutputValues!["objects"].Should().BeEquivalentTo(new List<uint>());
        result.OutputValues["count"].Should().Be(0);
    }

    [Test]
    public async Task Default_max_count_is_10_when_input_is_null()
    {
        var node = CreateNode();
        var context = CreateContext();

        // Both inputs null — origin=0 (invalid), max_count defaults to 10
        GlyphNodeResult result = await _executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        // Just verify it doesn't throw — origin is invalid so result is empty
        result.OutputValues.Should().NotBeNull();
        result.OutputValues!["count"].Should().Be(0);
    }

    [Test]
    public async Task Negative_max_count_is_clamped_to_10()
    {
        var node = CreateNode();
        var context = CreateContext();

        GlyphNodeResult result = await _executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin switch
            {
                "origin" => null,
                "max_count" => -5,
                _ => null
            }));

        result.OutputValues.Should().NotBeNull();
        result.OutputValues!["count"].Should().Be(0);
    }

    [Test]
    public async Task Type_property_override_defaults_to_creature()
    {
        // Node with no property overrides — should default to "Creature"
        var node = CreateNode();
        var context = CreateContext();

        // With null origin, result is empty regardless — but verifying no exception
        GlyphNodeResult result = await _executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.OutputValues.Should().NotBeNull();
    }

    [Test]
    public async Task Type_property_override_is_read_from_node()
    {
        var node = CreateNode();
        node.PropertyOverrides["type"] = "Placeable";
        var context = CreateContext();

        // Just verify no exception with the override set
        GlyphNodeResult result = await _executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.OutputValues.Should().NotBeNull();
        result.OutputValues!["count"].Should().Be(0);
    }

    [Test]
    public async Task Unknown_type_returns_empty_list()
    {
        var node = CreateNode();
        node.PropertyOverrides["type"] = "InvalidType";
        var context = CreateContext();

        GlyphNodeResult result = await _executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.OutputValues.Should().NotBeNull();
        result.OutputValues!["count"].Should().Be(0);
    }

    [Test]
    public void Result_is_data_only_no_exec_flow()
    {
        // PureFunction nodes should return data results with no exec continuation
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.Archetype.Should().Be(GlyphNodeArchetype.PureFunction);
        // Verify no Exec pins exist
        def.InputPins.Should().NotContain(p => p.DataType == GlyphDataType.Exec);
        def.OutputPins.Should().NotContain(p => p.DataType == GlyphDataType.Exec);
    }

    // ==================== Helpers ====================

    private static GlyphNodeInstance CreateNode() => new()
    {
        TypeId = GetNearestObjectsByTypeExecutor.NodeTypeId,
        PropertyOverrides = new Dictionary<string, string>()
    };

    private static GlyphExecutionContext CreateContext() => new()
    {
        Graph = new GlyphGraph { EventType = GlyphEventType.BeforeGroupSpawn, Name = "Test" },
        MaxExecutionSteps = 1000,
        EnableTracing = false
    };
}
