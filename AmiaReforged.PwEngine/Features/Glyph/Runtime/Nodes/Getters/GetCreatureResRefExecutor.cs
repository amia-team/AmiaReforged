using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets the blueprint ResRef of a creature. Pure data node — no execution flow.
/// </summary>
public class GetCreatureResRefExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.creature_resref";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        uint creature = Convert.ToUInt32(creatureValue);

        string resref = creature != NWScript.OBJECT_INVALID
            ? NWScript.GetResRef(creature)
            : string.Empty;

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["resref"] = resref
        });
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Creature ResRef",
        Category = "Getters",
        Description = "Returns the blueprint ResRef of a creature.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        InputPins =
        [
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "resref", Name = "ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output }
        ]
    };
}
