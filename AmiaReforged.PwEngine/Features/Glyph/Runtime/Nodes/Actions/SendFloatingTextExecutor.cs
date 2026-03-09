using AmiaReforged.PwEngine.Features.Glyph.Core;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Sends floating text above a creature. Useful for feedback during encounters
/// (e.g., "Enraged!" above a buffed creature).
/// </summary>
public class SendFloatingTextExecutor : IGlyphNodeExecutor
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public const string NodeTypeId = "action.send_floating_text";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        object? messageValue = await resolveInput("message");

        uint creature = Convert.ToUInt32(creatureValue);
        string message = messageValue?.ToString() ?? string.Empty;

        Log.Debug("[Glyph] SendFloatingText: creature=0x{Creature:X}, message=\"{Message}\", valid={Valid}",
            creature, message, creature != NWScript.OBJECT_INVALID);

        if (creature != NWScript.OBJECT_INVALID && !string.IsNullOrEmpty(message))
        {
            NWScript.FloatingTextStringOnCreature(message, creature);
            Log.Debug("[Glyph] SendFloatingText: called FloatingTextStringOnCreature successfully");
        }
        else
        {
            Log.Warn("[Glyph] SendFloatingText: skipped — creature={Creature} (invalid={IsInvalid}), message=\"{Message}\"",
                creature, creature == NWScript.OBJECT_INVALID, message);
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Send Floating Text",
        Category = "Actions",
        Description = "Displays floating text above a creature.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "message", Name = "Message", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
