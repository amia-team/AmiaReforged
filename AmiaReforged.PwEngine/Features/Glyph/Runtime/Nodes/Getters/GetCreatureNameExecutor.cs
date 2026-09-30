using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets the display name (and original name) of a creature. Pure data node.
/// </summary>
[GlyphNode]
public partial class GetCreatureNameExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.creature_name";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput(Inputs.Creature);
        uint creature = Convert.ToUInt32(creatureValue);

        string name = creature != NWScript.OBJECT_INVALID
            ? NWScript.GetName(creature)
            : string.Empty;

        string originalName = creature != NWScript.OBJECT_INVALID
            ? NWScript.GetName(creature, NWScript.TRUE)
            : string.Empty;

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["name"] = name,
            ["original_name"] = originalName
        });
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("creature.name", "name", null, PropertyAliases: [new("creature.name", "creature.name", "creature")])
        ],
        DisplayName = "Get Creature Name",
        Category = "Getters",
        Description = "Returns the current display name and original blueprint name of a creature.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters =
        [
            Pins.In("creature", "Creature", GlyphDataType.NwObject)
        ],
        Results =
        [
            Pins.Out("name", "Name", GlyphDataType.String),
            Pins.Out("original_name", "Original Name", GlyphDataType.String)
        ]
    };
}
