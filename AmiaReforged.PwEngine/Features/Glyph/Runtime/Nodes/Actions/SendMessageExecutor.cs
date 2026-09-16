using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Action node that sends a text message to a creature via a specified channel.
/// Supports server messages, floating text, and shout.
/// </summary>
public sealed class SendMessageExecutor : GlyphActionNode
{
    public const string NodeTypeId = "action.send_message";

    public override string TypeId => NodeTypeId;

    protected override async Task RunActionAsync(GlyphNodeContext cx)
    {
        uint creature = await cx.InObject("creature");
        string message = await cx.InString("message");
        string channel = await cx.InString("channel", "server");

        if (creature == NWScript.OBJECT_INVALID || string.IsNullOrEmpty(message))
            return;

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
    }

    public override GlyphNodeDefinition CreateDefinition() => new()
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
            Pins.ExecIn(),
            Pins.InObject("creature", "Creature"),
            Pins.In("message", "Message", GlyphDataType.String),
            Pins.InString("channel", "Channel", "server"),
        ],
        OutputPins =
        [
            Pins.ExecOut("exec_out", "Then"),
        ]
    };
}
