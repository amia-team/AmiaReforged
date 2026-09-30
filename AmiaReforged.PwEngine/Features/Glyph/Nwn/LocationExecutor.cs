using System.Numerics;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
namespace AmiaReforged.PwEngine.Features.Glyph.Nwn;
[GlyphNode]
public sealed partial class LocationExecutor : GlyphNodeBase
{
    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = "nwn.location", DisplayName = "Location", Category = "NWN / Adapters",
        Description = "Constructs an NWN location from an area, coordinates in meters and facing in degrees.", Source = "NWScript.Location", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters = [Pins.InObject("area", "Area"), Pins.InFloat("x", "X", null), Pins.InFloat("y", "Y", null), Pins.InFloat("z", "Z", "0"), Pins.InFloat("facing", "Facing", "0")],
        Results = [Pins.Out("value", "Location", GlyphDataType.Location)],
        Exports = [new("nwn.location", "value")]
    };
    public override string TypeId => Descriptor.TypeId;
    public override GlyphNodeDefinition CreateDefinition() => Descriptor.CreateDefinition();
    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        uint area = await cx.InObject("area");
        float x = (float)await cx.InFloat("x"), y = (float)await cx.InFloat("y"), z = (float)await cx.InFloat("z");
        float facing = (float)await cx.InFloat("facing");
        GlyphNwnLocation location = area == NWScript.OBJECT_INVALID ? default : new(NWScript.Location(area, new Vector3(x, y, z), facing));
        return GlyphNodeResult.Data(new() { ["value"] = location });
    }
}
