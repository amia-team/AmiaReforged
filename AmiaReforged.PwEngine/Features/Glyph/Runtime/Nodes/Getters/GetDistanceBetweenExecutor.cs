using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns the distance in meters between two NWN game objects. Pure data node.
/// Returns 0.0 if either object is invalid.
/// </summary>
[GlyphNode]
public partial class GetDistanceBetweenExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.distance_between";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? objectAValue = await resolveInput(Inputs.ObjectA);
        object? objectBValue = await resolveInput(Inputs.ObjectB);

        uint objectA = Nwn.GlyphNwnValue.NormalizeObject(Convert.ToUInt32(objectAValue));
        uint objectB = Nwn.GlyphNwnValue.NormalizeObject(Convert.ToUInt32(objectBValue));

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

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId, Source = "NWScript.GetDistanceBetween", Backend = "NWScript adapter",
        Exports = [
            new("nwn.get_distance_between", "distance"),

        ],
        DisplayName = "Get Distance Between",
        Category = "Getters",
        Description = "Returns the distance in meters between two game objects. Returns 0 if either object is invalid.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters =
        [
            Pins.In("object_a", "Object A", GlyphDataType.NwObject),
            Pins.In("object_b", "Object B", GlyphDataType.NwObject)
        ],
        Results =
        [
            Pins.Out("distance", "Distance", GlyphDataType.Float)
        ]
    };
}
