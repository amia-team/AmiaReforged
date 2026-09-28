using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;
using FluentAssertions;
using NUnit.Framework;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

[TestFixture]
public class IsPlayerExecutorTests
{
    private IsPlayerExecutor _executor = null!;

    [SetUp]
    public void SetUp() => _executor = new IsPlayerExecutor();

    // ==================== Definition Validation ====================

    [Test]
    public void Definition_has_correct_type_id()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.TypeId.Should().Be("getter.is_player");
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
    public void Definition_has_object_input_and_bool_result_pins()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.InputPins.Should().HaveCount(1);
        def.InputPins.Should().Contain(p =>
            p.Id == "object" && p.DataType == GlyphDataType.NwObject && p.Direction == GlyphPinDirection.Input);

        def.OutputPins.Should().HaveCount(1);
        def.OutputPins.Should().Contain(p =>
            p.Id == "result" && p.DataType == GlyphDataType.Bool && p.Direction == GlyphPinDirection.Output);
    }

    // ==================== Execution — Invalid / No-Result Handling ====================

    [Test]
    public async Task Null_object_returns_false()
    {
        GlyphNodeResult result = await Run(objectValue: null);
        ((bool)result.OutputValues!["result"]).Should().BeFalse();
    }

    [Test]
    public async Task Zero_object_returns_false()
    {
        GlyphNodeResult result = await Run(objectValue: 0u);
        ((bool)result.OutputValues!["result"]).Should().BeFalse();
    }

    [Test]
    public async Task Invalid_object_id_returns_false()
    {
        GlyphNodeResult result = await Run(objectValue: NWScript.OBJECT_INVALID);
        ((bool)result.OutputValues!["result"]).Should().BeFalse();
    }

    [Test]
    public void Result_is_data_only_no_exec_flow()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.Archetype.Should().Be(GlyphNodeArchetype.PureFunction);
        def.InputPins.Should().NotContain(p => p.DataType == GlyphDataType.Exec);
        def.OutputPins.Should().NotContain(p => p.DataType == GlyphDataType.Exec);
    }

    // ==================== Helpers ====================

    private static async Task<GlyphNodeResult> Run(object? objectValue)
    {
        IsPlayerExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = IsPlayerExecutor.NodeTypeId };
        GlyphExecutionContext context = new()
        {
            Graph = new GlyphGraph { EventType = GlyphEventType.BeforeGroupSpawn, Name = "Test" },
            MaxExecutionSteps = 1000,
            EnableTracing = false
        };

        return await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "object" ? objectValue : null));
    }
}
