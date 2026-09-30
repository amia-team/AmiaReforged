using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
namespace AmiaReforged.PwEngine.Features.Glyph.Nwn;
[GlyphNode]
public sealed partial class ObjectsByTagExecutor : GlyphNodeBase
{
    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = "nwn.objects_by_tag", DisplayName = "ObjectsByTag", Category = "NWN / Adapters",
        Description = "Snapshots all live objects with a tag, in NWScript index order.", Source = "NWScript.GetObjectByTag", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters = [Pins.InString("tag", "Tag")],
        Results = [Pins.Out("value", "Objects", GlyphDataType.List) with { ElementType = GlyphDataType.NwObject }],
        Exports = [new("nwn.objects_by_tag", "value")]
    };
    public override string TypeId => Descriptor.TypeId;
    public override GlyphNodeDefinition CreateDefinition() => Descriptor.CreateDefinition();
    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        string tag = await cx.InString("tag");
        List<uint> objects = [];
        for (int nth = 0; ; nth++)
        {
            uint item = GlyphNwnValue.NormalizeObject(NWScript.GetObjectByTag(tag, nth));
            if (item == NWScript.OBJECT_INVALID) break;
            objects.Add(item);
        }
        return GlyphNodeResult.Data(new() { ["value"] = objects });
    }
}
