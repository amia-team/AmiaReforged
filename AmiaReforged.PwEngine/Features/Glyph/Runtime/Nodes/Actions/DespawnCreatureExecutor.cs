using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Destroys a creature after an optional delay. Useful for custom despawn logic.
/// </summary>
[GlyphNode]
public partial class DespawnCreatureExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.despawn_creature";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        object? delayValue = await resolveInput("delay_seconds");

        uint creature = Nwn.GlyphNwnValue.NormalizeObject(Convert.ToUInt32(creatureValue));
        float delay = Convert.ToSingle(delayValue);

        if (creature != NWScript.OBJECT_INVALID)
        {
            NWScript.DestroyObject(creature, System.Math.Max(0, delay));
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId, DisplayName = "DespawnCreatureExecutor", Category = "NWN / Compatibility",
        Description = "NWScript DestroyObject with the established runtime pin contract.",
        Source = "NWScript.DestroyObject", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.Action,
        Parameters = [Pins.InObject("creature", "Creature"), Pins.InFloat("delay_seconds", "Delay", "0")], Results = [],
        Exports = [new("nwn.destroy_object", null, ReceiverMethods: ["destroy"])]
    };
}
