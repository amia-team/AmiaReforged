using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
namespace AmiaReforged.PwEngine.Features.Glyph.Nwn;
[GlyphNode]
public sealed partial class NearestObjectExecutor : GlyphNodeBase
{
    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = "nwn.get_nearest_object_by_type", DisplayName = "NearestObject", Category = "NWN / Adapters",
        Description = "Returns the nth nearest object matching an OBJECT_TYPE mask. Origin is explicit; invalid input/no match returns OBJECT.INVALID.", Source = "NWScript.GetNearestObject", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters = [Pins.InObject("origin", "Origin"), Pins.InInt("object_type", "Object Type", "32767"), Pins.InInt("nth", "Nth", "1")],
        Results = [Pins.Out("value", "Object", GlyphDataType.NwObject)],
        Exports = [new("nwn.get_nearest_object_by_type", "value"), new("nwn.get_nearest_object", "value")]
    };
    public override string TypeId => Descriptor.TypeId;
    public override GlyphNodeDefinition CreateDefinition() => Descriptor.CreateDefinition();
    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        uint origin = await cx.InObject("origin");
        int type = await cx.InInt("object_type", NWScript.OBJECT_TYPE_ALL);
        int nth = await cx.InInt("nth", 1);
        uint result = origin == NWScript.OBJECT_INVALID ? NWScript.OBJECT_INVALID : NWScript.GetNearestObject(type, origin, nth);
        return GlyphNodeResult.Data(new() { ["value"] = GlyphNwnValue.NormalizeObject(result) });
    }
}
