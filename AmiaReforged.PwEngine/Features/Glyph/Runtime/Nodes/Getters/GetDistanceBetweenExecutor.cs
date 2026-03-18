using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns the distance in meters between two NWN game objects. Pure data node.
/// Returns 0.0 if either object is invalid.
/// </summary>
public class GetDistanceBetweenExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.distance_between";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? objectAValue = await resolveInput("object_a");
        object? objectBValue = await resolveInput("object_b");

        uint objectA = Convert.ToUInt32(objectAValue);
        uint objectB = Convert.ToUInt32(objectBValue);

        float distance = 0f;
        if (objectA != NWScript.OBJECT_INVALID && objectB != NWScript.OBJECT_INVALID)
        {
            distance = NWScript.GetDistanceBetween(objectA, objectB);
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["distance"] = (double)distance
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Distance Between",
        Category = "Getters",
        Description = "Returns the distance in meters between two game objects. Returns 0 if either object is invalid.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        InputPins =
        [
            new GlyphPin { Id = "object_a", Name = "Object A", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "object_b", Name = "Object B", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "distance", Name = "Distance", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Output }
        ]
    };
}
