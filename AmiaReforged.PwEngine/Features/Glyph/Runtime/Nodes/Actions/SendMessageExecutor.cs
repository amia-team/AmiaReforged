using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Action node that sends a text message to a creature via a specified channel.
/// Supports server messages, floating text, and shout.
/// </summary>
public class SendMessageExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.send_message";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        object? messageValue = await resolveInput("message");
        object? channelValue = await resolveInput("channel");

        uint creature = Convert.ToUInt32(creatureValue);
        string message = messageValue?.ToString() ?? string.Empty;
        string channel = channelValue?.ToString() ?? "server";

        if (creature == NWScript.OBJECT_INVALID || string.IsNullOrEmpty(message))
            return GlyphNodeResult.Continue("exec_out");

        switch (channel.ToLowerInvariant())
        {
            case "floating":
                NWScript.FloatingTextStringOnCreature(message, creature);
                break;
            case "shout":
                // Speak as the creature
                NWScript.AssignCommand(creature,
                    () => NWScript.SpeakString(message, NWScript.TALKVOLUME_SHOUT));
                break;
            case "server":
            default:
                NWScript.SendMessageToPC(creature, message);
                break;
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Send Message",
        Category = "Actions",
        Description = "Sends a text message to a creature. Channels: 'server' (system message), " +
                      "'floating' (floating text above creature), 'shout' (speak as creature).",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "message", Name = "Message", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "channel", Name = "Channel", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "server" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
