using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Logic;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Math;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

/// <summary>
/// Characterization tests for Compare / BooleanOp / Not / StringContains.
/// Written against the pre-migration implementations; must keep passing after migration.
/// </summary>
[TestFixture]
public class MathLogicExecutorTests
{
    private static (GlyphNodeInstance node, GlyphExecutionContext ctx) Setup(string typeId) => (
        new GlyphNodeInstance { TypeId = typeId },
        new GlyphExecutionContext
        {
            Graph = new GlyphGraph { Name = "test", EventType = GlyphEventType.BeforeGroupSpawn },
        });

    private static Task<GlyphNodeResult> RunCompare(double? a, double? b, string? op)
    {
        CompareExecutor executor = new();
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup(CompareExecutor.NodeTypeId);
        return executor.ExecuteAsync(node, ctx, pin => Task.FromResult<object?>(pin switch
        {
            "a" => a,
            "b" => b,
            "operator" => op,
            _ => null,
        }));
    }

    [Test]
    public void Compare_definition()
    {
        GlyphNodeDefinition def = new CompareExecutor().CreateDefinition();
        def.TypeId.Should().Be("math.compare");
        def.InputPins.Should().Contain(p => p.Id == "a" && p.DefaultValue == "0");
        def.InputPins.Should().Contain(p => p.Id == "b" && p.DefaultValue == "0");
        def.InputPins.Should().Contain(p => p.Id == "operator" && p.DefaultValue == "==");
        def.OutputPins.Should().ContainSingle(p => p.Id == "result" && p.DataType == GlyphDataType.Bool);
    }

    [TestCase("==", 5, 5, true)]
    [TestCase("!=", 5, 6, true)]
    [TestCase("!=", 5, 5, false)]
    [TestCase("<", 3, 4, true)]
    [TestCase(">", 4, 3, true)]
    [TestCase("<=", 4, 4, true)]
    [TestCase(">=", 3, 4, false)]
    public async Task Compare_operators(string op, double a, double b, bool expected)
    {
        GlyphNodeResult result = await RunCompare(a, b, op);
        result.OutputValues["result"].Should().Be(expected);
    }

    [Test]
    public async Task Compare_equality_uses_epsilon()
    {
        GlyphNodeResult result = await RunCompare(1.0, 1.0 + 0.00005, "==");
        result.OutputValues["result"].Should().Be(true);
    }

    [Test]
    public async Task Compare_unknown_operator_yields_false()
    {
        GlyphNodeResult result = await RunCompare(1, 1, "??");
        result.OutputValues["result"].Should().Be(false);
    }

    [Test]
    public async Task Compare_null_inputs_are_treated_as_zero()
    {
        GlyphNodeResult result = await RunCompare(null, null, "==");
        result.OutputValues["result"].Should().Be(true);
    }

    private static Task<GlyphNodeResult> RunBooleanOp(object? a, object? b, string? op)
    {
        BooleanOpExecutor executor = new();
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup(BooleanOpExecutor.NodeTypeId);
        return executor.ExecuteAsync(node, ctx, pin => Task.FromResult<object?>(pin switch
        {
            "a" => a,
            "b" => b,
            "operator" => op,
            _ => null,
        }));
    }

    [Test]
    public void BooleanOp_definition()
    {
        GlyphNodeDefinition def = new BooleanOpExecutor().CreateDefinition();
        def.TypeId.Should().Be("math.boolean_op");
        def.InputPins.Should().Contain(p => p.Id == "a" && p.DataType == GlyphDataType.Bool);
        def.InputPins.Should().Contain(p => p.Id == "b" && p.DataType == GlyphDataType.Bool);
        def.InputPins.Should().Contain(p => p.Id == "operator" && p.DefaultValue == "AND");
        def.OutputPins.Should().ContainSingle(p => p.Id == "result" && p.DataType == GlyphDataType.Bool);
    }

    [TestCase("AND", true, true, true)]
    [TestCase("AND", true, false, false)]
    [TestCase("OR", false, false, false)]
    [TestCase("OR", true, false, true)]
    [TestCase("XOR", true, true, false)]
    [TestCase("XOR", true, false, true)]
    [TestCase("and", true, true, true, Description = "operator is case-insensitive")]
    [TestCase("NAND", true, true, false, Description = "unknown operator yields false")]
    public async Task BooleanOp_operators(string op, bool a, bool b, bool expected)
    {
        GlyphNodeResult result = await RunBooleanOp(a, b, op);
        result.OutputValues["result"].Should().Be(expected);
    }

    [Test]
    public async Task BooleanOp_null_inputs_are_treated_as_false()
    {
        GlyphNodeResult result = await RunBooleanOp(null, null, "OR");
        result.OutputValues["result"].Should().Be(false);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(null, true, Description = "null coerces to false, inverted to true")]
    public async Task Not_inverts_input(object? input, bool expected)
    {
        NotExecutor executor = new();
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup(NotExecutor.NodeTypeId);
        GlyphNodeResult result = await executor.ExecuteAsync(node, ctx, _ => Task.FromResult(input));
        result.OutputValues["result"].Should().Be(expected);
    }

    [Test]
    public void Not_definition()
    {
        GlyphNodeDefinition def = new NotExecutor().CreateDefinition();
        def.TypeId.Should().Be("math.not");
        def.InputPins.Should().ContainSingle(p => p.Id == "value" && p.DefaultValue == "false");
        def.OutputPins.Should().ContainSingle(p => p.Id == "result" && p.DataType == GlyphDataType.Bool);
    }

    private static Task<GlyphNodeResult> RunStringContains(string? text, params string?[] patterns)
    {
        StringContainsExecutor executor = new();
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup(StringContainsExecutor.NodeTypeId);
        return executor.ExecuteAsync(node, ctx, pin =>
        {
            if (pin == "text")
            {
                return Task.FromResult<object?>(text);
            }

            if (pin.StartsWith("pattern_", StringComparison.Ordinal)
                && int.TryParse(pin["pattern_".Length..], out int i)
                && i < patterns.Length)
            {
                return Task.FromResult<object?>(patterns[i]);
            }

            return Task.FromResult<object?>(null);
        });
    }

    [Test]
    public void StringContains_definition()
    {
        GlyphNodeDefinition def = new StringContainsExecutor().CreateDefinition();
        def.TypeId.Should().Be("logic.string_contains");
        def.InputPins.Should().Contain(p => p.Id == "text" && p.DataType == GlyphDataType.String);
        def.InputPins.Count(p => p.Id.StartsWith("pattern_", StringComparison.Ordinal))
            .Should().Be(StringContainsExecutor.MaxPatterns);
        def.OutputPins.Select(p => p.Id).Should().BeEquivalentTo("result", "matched");
    }

    [Test]
    public async Task StringContains_matches_case_insensitively_and_reports_pattern()
    {
        GlyphNodeResult result = await RunStringContains("Hello World", "world");
        result.OutputValues["result"].Should().Be(true);
        result.OutputValues["matched"].Should().Be("world");
    }

    [Test]
    public async Task StringContains_no_match_yields_false_and_empty_pattern()
    {
        GlyphNodeResult result = await RunStringContains("Hello World", "xyz");
        result.OutputValues["result"].Should().Be(false);
        result.OutputValues["matched"].Should().Be(string.Empty);
    }

    [Test]
    public async Task StringContains_empty_text_short_circuits_to_false()
    {
        GlyphNodeResult result = await RunStringContains("", "a");
        result.OutputValues["result"].Should().Be(false);
        result.OutputValues["matched"].Should().Be(string.Empty);
    }

    [Test]
    public async Task StringContains_skips_empty_patterns_and_uses_first_match()
    {
        GlyphNodeResult result = await RunStringContains("abcdef", "", "cde", "bcd");
        result.OutputValues["result"].Should().Be(true);
        result.OutputValues["matched"].Should().Be("cde");
    }
}
