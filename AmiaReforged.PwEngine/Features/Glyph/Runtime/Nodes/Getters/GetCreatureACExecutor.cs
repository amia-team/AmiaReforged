using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets the armor class of a creature. Pure data node.
/// </summary>
public class GetCreatureACExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.creature_ac";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        uint creature = Convert.ToUInt32(creatureValue);

        int ac = creature != NWScript.OBJECT_INVALID
            ? NWScript.GetAC(creature)
            : 0;

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["ac"] = ac
        });
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Creature AC",
        Category = "Getters",
        Description = "Returns the current armor class of a creature.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        InputPins =
        [
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "ac", Name = "Armor Class", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
