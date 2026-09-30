using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Action node that sends a text message to a creature via a specified channel.
/// Supports server messages, floating text, and shout.
/// </summary>
[GlyphNode]
public sealed partial class SendMessageExecutor : GlyphActionNode
{
    public const string NodeTypeId = "action.send_message";

    public override string TypeId => NodeTypeId;

    protected override async Task RunActionAsync(GlyphNodeContext cx)
    {
        uint creature = await cx.InObject(Inputs.Creature);
        string message = await cx.InString(Inputs.Message);
        string channel = await cx.InString(Inputs.Channel, "server");

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

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("message", null)
        ],
        DisplayName = "Send Message",
        Category = "Actions",
        Description = "Sends a text message to a creature. Channels: 'server' (system message), " +
                      "'floating' (floating text above creature), 'shout' (speak as creature).",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        Parameters =
        [
            Pins.InObject("creature", "Creature"),
            Pins.In("message", "Message", GlyphDataType.String),
            Pins.InString("channel", "Channel", "server"),
        ]
    };
}
