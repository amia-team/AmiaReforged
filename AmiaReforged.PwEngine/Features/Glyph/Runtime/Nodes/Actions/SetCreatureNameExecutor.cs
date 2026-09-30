using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Renames a creature by calling NWScript.SetName. Works in any encounter event.
/// </summary>
[GlyphNode]
public partial class SetCreatureNameExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.set_creature_name";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput(Inputs.Creature);
        object? nameValue = await resolveInput(Inputs.Name);

        uint creature = Nwn.GlyphNwnValue.NormalizeObject(Convert.ToUInt32(creatureValue));
        string name = nameValue?.ToString() ?? string.Empty;

        if (creature != NWScript.OBJECT_INVALID)
        {
            NWScript.SetName(creature, name);
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId, Source = "NWScript.SetName", Backend = "NWScript adapter",
        Exports = [
            new("nwn.set_name", ReceiverMethods: ["set_name"]),
            new("set_name", null)
        ],
        DisplayName = "Set Creature Name",
        Category = "Actions",
        Description = "Changes the display name of a creature.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        Parameters =
        [
            Pins.In("creature", "Creature", GlyphDataType.NwObject),
            Pins.In("name", "Name", GlyphDataType.String)
        ]
    };
}
