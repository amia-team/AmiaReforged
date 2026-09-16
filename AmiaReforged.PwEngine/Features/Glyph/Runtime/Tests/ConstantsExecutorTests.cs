using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Constants;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

/// <summary>
/// Characterization tests for the four constant nodes. Written against the
/// pre-migration implementations; must keep passing after migration.
/// </summary>
[TestFixture]
public class ConstantsExecutorTests
{
    private static (GlyphNodeInstance node, GlyphExecutionContext ctx) Setup(string typeId) => (
        new GlyphNodeInstance { TypeId = typeId },
        new GlyphExecutionContext
        {
            Graph = new GlyphGraph { Name = "test", EventType = GlyphEventType.BeforeGroupSpawn },
        });

    private static void CheckDefinition(
        IGlyphNodeExecutor executor, string typeId, GlyphDataType dataType, string defaultValue)
    {
        GlyphNodeDefinition def = executor.CreateDefinition();
        def.TypeId.Should().Be(typeId);
        def.Category.Should().Be("Constants");
        def.InputPins.Should().ContainSingle(p =>
            p.Id == "value" && p.DataType == dataType && p.DefaultValue == defaultValue);
        def.OutputPins.Should().ContainSingle(p => p.Id == "out" && p.DataType == dataType);
    }

    [Test] public void Int_definition() => CheckDefinition(new IntConstantExecutor(), "constant.int", GlyphDataType.Int, "0");
    [Test] public void Float_definition() => CheckDefinition(new FloatConstantExecutor(), "constant.float", GlyphDataType.Float, "0.0");
    [Test] public void Bool_definition() => CheckDefinition(new BoolConstantExecutor(), "constant.bool", GlyphDataType.Bool, "false");
    [Test] public void String_definition() => CheckDefinition(new StringConstantExecutor(), "constant.string", GlyphDataType.String, "");

    [Test]
    public async Task Int_passes_value_through_and_defaults_to_zero()
    {
        IntConstantExecutor executor = new();
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup(IntConstantExecutor.NodeTypeId);

        GlyphNodeResult set = await executor.ExecuteAsync(node, ctx, _ => Task.FromResult<object?>(42));
        set.OutputValues["out"].Should().Be(42);

        GlyphNodeResult unset = await executor.ExecuteAsync(node, ctx, _ => Task.FromResult<object?>(null));
        unset.OutputValues["out"].Should().Be(0);
    }

    [Test]
    public async Task Float_passes_value_through_and_defaults_to_zero()
    {
        FloatConstantExecutor executor = new();
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup(FloatConstantExecutor.NodeTypeId);

        GlyphNodeResult set = await executor.ExecuteAsync(node, ctx, _ => Task.FromResult<object?>(2.5));
        set.OutputValues["out"].Should().Be(2.5);

        GlyphNodeResult unset = await executor.ExecuteAsync(node, ctx, _ => Task.FromResult<object?>(null));
        unset.OutputValues["out"].Should().Be(0.0);
    }

    [TestCase(true, true)]
    [TestCase(false, false)]
    [TestCase("true", true)]
    [TestCase("TRUE", true)]
    [TestCase(1, true)]
    [TestCase(null, false)]
    public async Task Bool_coerces_expected_inputs(object? input, bool expected)
    {
        BoolConstantExecutor executor = new();
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup(BoolConstantExecutor.NodeTypeId);

        GlyphNodeResult result = await executor.ExecuteAsync(node, ctx, _ => Task.FromResult(input));
        result.OutputValues["out"].Should().Be(expected);
    }

    [Test]
    public async Task String_passes_value_through_and_defaults_to_empty()
    {
        StringConstantExecutor executor = new();
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup(StringConstantExecutor.NodeTypeId);

        GlyphNodeResult set = await executor.ExecuteAsync(node, ctx, _ => Task.FromResult<object?>("hi"));
        set.OutputValues["out"].Should().Be("hi");

        GlyphNodeResult unset = await executor.ExecuteAsync(node, ctx, _ => Task.FromResult<object?>(null));
        unset.OutputValues["out"].Should().Be(string.Empty);
    }
}
