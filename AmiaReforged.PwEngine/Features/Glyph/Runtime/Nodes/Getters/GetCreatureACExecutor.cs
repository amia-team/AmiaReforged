using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets the armor class of a creature. Pure data node.
/// </summary>
[GlyphNode]
public partial class GetCreatureACExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.creature_ac";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput(Inputs.Creature);
        uint creature = Convert.ToUInt32(creatureValue);

        int ac = creature != NWScript.OBJECT_INVALID
            ? NWScript.GetAC(creature)
            : 0;

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["ac"] = ac
        });
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("creature.ac", "ac", null, PropertyAliases: [new("creature.ac", "creature.ac", "creature")])
        ],
        DisplayName = "Get Creature AC",
        Category = "Getters",
        Description = "Returns the current armor class of a creature.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters =
        [
            Pins.In("creature", "Creature", GlyphDataType.NwObject)
        ],
        Results =
        [
            Pins.Out("ac", "Armor Class", GlyphDataType.Int)
        ]
    };
}
