using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets the display name (and original name) of a creature. Pure data node.
/// </summary>
public class GetCreatureNameExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.creature_name";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
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

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Creature Name",
        Category = "Getters",
        Description = "Returns the current display name and original blueprint name of a creature.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        InputPins =
        [
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "name", Name = "Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "original_name", Name = "Original Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output }
        ]
    };
}
