using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;
using FluentAssertions;
using NUnit.Framework;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

/// <summary>
/// SendMessage calls NWScript for valid creatures, which requires a live server.
/// These tests pin the definition shape and the early-return paths
/// (invalid creature / empty message) that never reach NWScript.
/// </summary>
[TestFixture]
public class SendMessageExecutorTests
{
    private readonly SendMessageExecutor _executor = new();

    private static (GlyphNodeInstance node, GlyphExecutionContext ctx) Setup() => (
        new GlyphNodeInstance { TypeId = SendMessageExecutor.NodeTypeId },
        new GlyphExecutionContext
        {
            Graph = new GlyphGraph { Name = "test", EventType = GlyphEventType.BeforeGroupSpawn },
        });

    private Task<GlyphNodeResult> Run(object? creature, object? message, object? channel)
    {
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        return _executor.ExecuteAsync(node, ctx, pin => Task.FromResult<object?>(pin switch
        {
            "creature" => creature,
            "message" => message,
            "channel" => channel,
            _ => null,
        }));
    }

    [Test]
    public void Definition_has_exec_and_creature_message_channel_pins()
    {
        GlyphNodeDefinition def = _executor.CreateDefinition();
        def.TypeId.Should().Be("action.send_message");
        def.Archetype.Should().Be(GlyphNodeArchetype.Action);
        def.InputPins.Should().Contain(p => p.Id == "exec_in" && p.DataType == GlyphDataType.Exec);
        def.InputPins.Should().Contain(p => p.Id == "creature" && p.DataType == GlyphDataType.NwObject);
        def.InputPins.Should().Contain(p => p.Id == "message" && p.DataType == GlyphDataType.String);
        def.InputPins.Should().Contain(p => p.Id == "channel" && p.DefaultValue == "server");
        def.OutputPins.Select(p => p.Id).Should().BeEquivalentTo("exec_out");
    }

    [Test]
    public async Task Invalid_creature_skips_send_and_continues()
    {
        GlyphNodeResult result = await Run(NWScript.OBJECT_INVALID, "hello", "server");
        result.NextExecPinId.Should().Be("exec_out");
    }

    [Test]
    public async Task Empty_message_skips_send_and_continues()
    {
        GlyphNodeResult result = await Run(NWScript.OBJECT_INVALID, "", "server");
        result.NextExecPinId.Should().Be("exec_out");
    }

    [Test]
    public async Task Unconnected_creature_resolves_to_invalid_and_continues()
    {
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        GlyphNodeResult result = await _executor.ExecuteAsync(node, ctx,
            pin => Task.FromResult<object?>(pin == "message" ? "hello" : null));
        result.NextExecPinId.Should().Be("exec_out");
    }
}
