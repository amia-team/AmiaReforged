using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
namespace AmiaReforged.PwEngine.Features.Glyph.Nwn;
[GlyphNode]
public sealed partial class AreasExecutor : GlyphNodeBase
{
    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = "nwn.areas", DisplayName = "Areas", Category = "NWN / Adapters",
        Description = "Returns an atomic snapshot of NWN objects for typed foreach iteration. Invalid input returns an empty list.", Source = "NWScript.GetFirstArea", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters = [],
        Results = [Pins.Out("value", "Objects", GlyphDataType.List) with { ElementType = GlyphDataType.NwObject }],
        Exports = [new("nwn.areas", "value")]
    };
    public override string TypeId => Descriptor.TypeId;
    public override GlyphNodeDefinition CreateDefinition() => Descriptor.CreateDefinition();
    public override Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        List<uint> objects = [];
        for (uint item = GlyphNwnValue.NormalizeObject(NWScript.GetFirstArea()); item != NWScript.OBJECT_INVALID; item = GlyphNwnValue.NormalizeObject(NWScript.GetNextArea()))
            objects.Add(item);
        return Task.FromResult(GlyphNodeResult.Data(new() { ["value"] = objects }));
    }
}
