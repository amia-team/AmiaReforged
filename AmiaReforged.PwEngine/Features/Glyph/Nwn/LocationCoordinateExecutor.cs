using System.Numerics;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
namespace AmiaReforged.PwEngine.Features.Glyph.Nwn;
[GlyphNode]
public sealed partial class LocationCoordinateExecutor : GlyphNodeBase
{
    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = "nwn.location_coordinates", DisplayName = "LocationCoordinate", Category = "NWN / Adapters",
        Description = "Returns the coordinates of a Location; an invalid location has zero coordinates.", Source = "NWScript.GetPositionFromLocation", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters = [Pins.In("location", "Location", GlyphDataType.Location)],
        Results = [Pins.Out("x", "X", GlyphDataType.Float), Pins.Out("y", "Y", GlyphDataType.Float), Pins.Out("z", "Z", GlyphDataType.Float)],
        Exports = [new("nwn.location_x", "x", ReceiverMethods: ["get_x"], ReceiverType: GlyphDataType.Location), new("nwn.location_y", "y", ReceiverMethods: ["get_y"], ReceiverType: GlyphDataType.Location), new("nwn.location_z", "z", ReceiverMethods: ["get_z"], ReceiverType: GlyphDataType.Location)]
    };
    public override string TypeId => Descriptor.TypeId;
    public override GlyphNodeDefinition CreateDefinition() => Descriptor.CreateDefinition();
    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        GlyphNwnLocation location = await cx.In<GlyphNwnLocation>("location");
        Vector3 position = location.Handle == IntPtr.Zero ? default : NWScript.GetPositionFromLocation(location.Handle);
        return GlyphNodeResult.Data(new() { ["x"] = (double)position.X, ["y"] = (double)position.Y, ["z"] = (double)position.Z });
    }
}
