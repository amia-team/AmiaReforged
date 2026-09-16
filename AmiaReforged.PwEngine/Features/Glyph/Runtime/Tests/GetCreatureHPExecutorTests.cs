using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;
using FluentAssertions;
using NUnit.Framework;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

/// <summary>
/// GetCreatureHP touches NWScript for valid creatures, which requires a live server.
/// These tests pin the definition shape and the invalid-object fallback path
/// (the only path reachable without a server).
/// </summary>
[TestFixture]
public class GetCreatureHPExecutorTests
{
    private readonly GetCreatureHPExecutor _executor = new();

    private static (GlyphNodeInstance node, GlyphExecutionContext ctx) Setup() => (
        new GlyphNodeInstance { TypeId = GetCreatureHPExecutor.NodeTypeId },
        new GlyphExecutionContext
        {
            Graph = new GlyphGraph { Name = "test", EventType = GlyphEventType.BeforeGroupSpawn },
        });

    [Test]
    public void Definition_has_creature_input_and_hp_outputs()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.TypeId.Should().Be("getter.creature_hp");
        def.Archetype.Should().Be(GlyphNodeArchetype.PureFunction);
        def.InputPins.Select(p => p.Id).Should().BeEquivalentTo("creature");
        def.OutputPins.Select(p => p.Id).Should().BeEquivalentTo("current_hp", "max_hp");
    }

    [Test]
    public async Task Invalid_creature_returns_zero_hp_without_touching_nwscript()
    {
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        GlyphNodeResult result = await _executor.ExecuteAsync(node, ctx,
            _ => Task.FromResult<object?>(NWScript.OBJECT_INVALID));

        result.OutputValues["current_hp"].Should().Be(0);
        result.OutputValues["max_hp"].Should().Be(0);
    }

    [Test]
    public async Task Unconnected_creature_resolves_to_invalid_and_returns_zero_hp()
    {
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        GlyphNodeResult result = await _executor.ExecuteAsync(node, ctx, _ => Task.FromResult<object?>(null));

        result.OutputValues["current_hp"].Should().Be(0);
        result.OutputValues["max_hp"].Should().Be(0);
    }
}
