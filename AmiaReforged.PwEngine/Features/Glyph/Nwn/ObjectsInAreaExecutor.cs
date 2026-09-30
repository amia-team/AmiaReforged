using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
namespace AmiaReforged.PwEngine.Features.Glyph.Nwn;
[GlyphNode]
public sealed partial class ObjectsInAreaExecutor : GlyphNodeBase
{
    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = "nwn.objects_in_area", DisplayName = "ObjectsInArea", Category = "NWN / Adapters",
        Description = "Returns an atomic snapshot of NWN objects for typed foreach iteration. Invalid input returns an empty list.", Source = "NWScript.GetFirstObjectInArea", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters = [Pins.InObject("area", "area"), Pins.InInt("object_type", "Object Type", "32767")],
        Results = [Pins.Out("value", "Objects", GlyphDataType.List) with { ElementType = GlyphDataType.NwObject }],
        Exports = [new("nwn.objects_in_area", "value", ReceiverMethods: ["objects"])]
    };
    public override string TypeId => Descriptor.TypeId;
    public override GlyphNodeDefinition CreateDefinition() => Descriptor.CreateDefinition();
    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        uint target = await cx.InObject("area");
        int objectType = await cx.InInt("object_type", NWScript.OBJECT_TYPE_ALL);
        List<uint> objects = [];
        if (target != NWScript.OBJECT_INVALID)
        {
            for (uint item = GlyphNwnValue.NormalizeObject(NWScript.GetFirstObjectInArea(target, objectType)); item != NWScript.OBJECT_INVALID; item = GlyphNwnValue.NormalizeObject(NWScript.GetNextObjectInArea(target, objectType)))
                objects.Add(item);
        }
        return GlyphNodeResult.Data(new() { ["value"] = objects });
    }
}
