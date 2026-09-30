using AmiaReforged.PwEngine.Features.Glyph.Core;
using NLog;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Sends floating text above a creature. Useful for feedback during encounters
/// (e.g., "Enraged!" above a buffed creature).
/// </summary>
[GlyphNode]
public partial class SendFloatingTextExecutor : IGlyphNodeExecutor
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public const string NodeTypeId = "action.send_floating_text";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput(Inputs.Creature);
        object? messageValue = await resolveInput(Inputs.Message);

        uint creature = Convert.ToUInt32(creatureValue);
        string message = messageValue?.ToString() ?? string.Empty;

        Log.Info("[Glyph] SendFloatingText: creature=0x{Creature:X}, message=\"{Message}\", valid={Valid}",
            creature, message, creature != NWScript.OBJECT_INVALID);

        if (creature != NWScript.OBJECT_INVALID && !string.IsNullOrEmpty(message))
        {
            NWScript.FloatingTextStringOnCreature(message, creature);
            Log.Info("[Glyph] SendFloatingText: called FloatingTextStringOnCreature successfully");
        }
        else
        {
            Log.Warn("[Glyph] SendFloatingText: skipped — creature={Creature} (invalid={IsInvalid}), message=\"{Message}\"",
                creature, creature == NWScript.OBJECT_INVALID, message);
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("floating_text", null)
        ],
        DisplayName = "Send Floating Text",
        Category = "Actions",
        Description = "Displays floating text above a creature.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        Parameters =
        [
            Pins.In("creature", "Creature", GlyphDataType.NwObject),
            Pins.In("message", "Message", GlyphDataType.String, "")
        ]
    };
}
