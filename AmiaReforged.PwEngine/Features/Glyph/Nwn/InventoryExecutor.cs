using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
namespace AmiaReforged.PwEngine.Features.Glyph.Nwn;
[GlyphNode]
public sealed partial class InventoryExecutor : GlyphNodeBase
{
    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = "nwn.inventory", DisplayName = "Inventory", Category = "NWN / Adapters",
        Description = "Returns an atomic snapshot of NWN objects for typed foreach iteration. Invalid input returns an empty list.", Source = "NWScript.GetFirstItemInInventory", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters = [Pins.InObject("target", "target")],
        Results = [Pins.Out("value", "Objects", GlyphDataType.List) with { ElementType = GlyphDataType.NwObject }],
        Exports = [new("nwn.inventory", "value", ReceiverMethods: ["inventory"])]
    };
    public override string TypeId => Descriptor.TypeId;
    public override GlyphNodeDefinition CreateDefinition() => Descriptor.CreateDefinition();
    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        uint target = await cx.InObject("target");
        List<uint> objects = [];
        if (target != NWScript.OBJECT_INVALID)
        {
            for (uint item = GlyphNwnValue.NormalizeObject(NWScript.GetFirstItemInInventory(target)); item != NWScript.OBJECT_INVALID; item = GlyphNwnValue.NormalizeObject(NWScript.GetNextItemInInventory(target)))
                objects.Add(item);
        }
        return GlyphNodeResult.Data(new() { ["value"] = objects });
    }
}
