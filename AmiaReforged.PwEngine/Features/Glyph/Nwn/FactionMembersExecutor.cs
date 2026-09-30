using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
namespace AmiaReforged.PwEngine.Features.Glyph.Nwn;
[GlyphNode]
public sealed partial class FactionMembersExecutor : GlyphNodeBase
{
    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = "nwn.faction_members", DisplayName = "FactionMembers", Category = "NWN / Adapters",
        Description = "Returns an atomic snapshot of NWN objects for typed foreach iteration. Invalid input returns an empty list.", Source = "NWScript.GetFirstFactionMember", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters = [Pins.InObject("member", "member"), Pins.InBool("pc_only", "Players only", "false")],
        Results = [Pins.Out("value", "Objects", GlyphDataType.List) with { ElementType = GlyphDataType.NwObject }],
        Exports = [new("nwn.faction_members", "value", ReceiverMethods: ["faction_members"])]
    };
    public override string TypeId => Descriptor.TypeId;
    public override GlyphNodeDefinition CreateDefinition() => Descriptor.CreateDefinition();
    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        uint target = await cx.InObject("member");
        bool pcOnly = await cx.InBool("pc_only");
        List<uint> objects = [];
        if (target != NWScript.OBJECT_INVALID)
        {
            for (uint item = GlyphNwnValue.NormalizeObject(NWScript.GetFirstFactionMember(target, pcOnly ? 1 : 0)); item != NWScript.OBJECT_INVALID; item = GlyphNwnValue.NormalizeObject(NWScript.GetNextFactionMember(target, pcOnly ? 1 : 0)))
                objects.Add(item);
        }
        return GlyphNodeResult.Data(new() { ["value"] = objects });
    }
}
