using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Math;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

[TestFixture]
public class MathOpExecutorTests
{
    private readonly MathOpExecutor _executor = new();

    private static (GlyphNodeInstance node, GlyphExecutionContext ctx) Setup() => (
        new GlyphNodeInstance { TypeId = MathOpExecutor.NodeTypeId },
        new GlyphExecutionContext
        {
            Graph = new GlyphGraph { Name = "test", EventType = GlyphEventType.BeforeGroupSpawn },
        });

    private Task<GlyphNodeResult> Run(double? a, double? b, string? op)
    {
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        return _executor.ExecuteAsync(node, ctx, pin => Task.FromResult<object?>(pin switch
        {
            "a" => a,
            "b" => b,
            "operator" => op,
            _ => null,
        }));
    }

    [Test]
    public void Definition_has_a_b_and_operator_pins()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.TypeId.Should().Be("math.math_op");
        def.Archetype.Should().Be(GlyphNodeArchetype.PureFunction);
        def.InputPins.Should().Contain(p => p.Id == "a" && p.DefaultValue == "0");
        def.InputPins.Should().Contain(p => p.Id == "b" && p.DefaultValue == "0");
        def.InputPins.Should().Contain(p => p.Id == "operator" && p.DefaultValue == "+");
        def.OutputPins.Select(p => p.Id).Should().BeEquivalentTo("result");
    }

    [TestCase("+", 3, 4, 7)]
    [TestCase("-", 10, 4, 6)]
    [TestCase("*", 3, 4, 12)]
    [TestCase("/", 12, 4, 3)]
    [TestCase("%", 10, 3, 1)]
    public async Task Operators_compute_expected_results(string op, double a, double b, double expected)
    {
        GlyphNodeResult result = await Run(a, b, op);
        result.OutputValues["result"].Should().Be(expected);
        result.NextExecPinId.Should().BeNull("pure nodes carry no exec flow");
    }

    [Test]
    public async Task Division_by_zero_yields_zero()
    {
        GlyphNodeResult result = await Run(5, 0, "/");
        result.OutputValues["result"].Should().Be(0);
    }

    [Test]
    public async Task Unknown_operator_yields_zero()
    {
        GlyphNodeResult result = await Run(5, 5, "^");
        result.OutputValues["result"].Should().Be(0);
    }

    [Test]
    public async Task Unconnected_inputs_default_to_zero_plus_zero()
    {
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        GlyphNodeResult result = await _executor.ExecuteAsync(node, ctx, _ => Task.FromResult<object?>(null));
        result.OutputValues["result"].Should().Be(0);
    }
}
