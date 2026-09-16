using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

[TestFixture]
public class BranchExecutorTests
{
    private readonly BranchExecutor _executor = new();

    private static (GlyphNodeInstance node, GlyphExecutionContext ctx) Setup() => (
        new GlyphNodeInstance { TypeId = BranchExecutor.NodeTypeId },
        new GlyphExecutionContext
        {
            Graph = new GlyphGraph { Name = "test", EventType = GlyphEventType.BeforeGroupSpawn },
        });

    [Test]
    public void Definition_has_exec_in_and_bool_condition_inputs()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.TypeId.Should().Be("flow.branch");
        def.Archetype.Should().Be(GlyphNodeArchetype.FlowControl);
        def.InputPins.Should().Contain(p => p.Id == "exec_in" && p.DataType == GlyphDataType.Exec);
        def.InputPins.Should().Contain(p =>
            p.Id == "condition" && p.DataType == GlyphDataType.Bool && p.DefaultValue == "false");
        def.OutputPins.Select(p => p.Id).Should().BeEquivalentTo("true", "false");
    }

    [Test]
    public async Task True_condition_follows_true_pin()
    {
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        GlyphNodeResult result = await _executor.ExecuteAsync(node, ctx, _ => Task.FromResult<object?>(true));
        result.NextExecPinId.Should().Be("true");
    }

    [Test]
    public async Task False_condition_follows_false_pin()
    {
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        GlyphNodeResult result = await _executor.ExecuteAsync(node, ctx, _ => Task.FromResult<object?>(false));
        result.NextExecPinId.Should().Be("false");
    }

    [Test]
    public async Task Unconnected_condition_defaults_to_false_branch()
    {
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        GlyphNodeResult result = await _executor.ExecuteAsync(node, ctx, _ => Task.FromResult<object?>(null));
        result.NextExecPinId.Should().Be("false");
    }

    [Test]
    public async Task Truthy_string_condition_follows_true_pin()
    {
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        GlyphNodeResult result = await _executor.ExecuteAsync(node, ctx, _ => Task.FromResult<object?>("true"));
        result.NextExecPinId.Should().Be("true");
    }
}
