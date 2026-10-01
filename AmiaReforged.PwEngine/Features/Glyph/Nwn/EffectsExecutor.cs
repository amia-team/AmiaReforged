using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
namespace AmiaReforged.PwEngine.Features.Glyph.Nwn;
[GlyphNode]
public sealed partial class EffectsExecutor : GlyphNodeBase
{
    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = "nwn.effects", DisplayName = "Effects", Category = "NWN / Adapters",
        Description = "Snapshots Effects as List<Effect>. Each foreach element retains Effect static typing.", Source = "NWScript.GetFirstEffect", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters = [Pins.InObject("target", "Target")],
        Results = [Pins.Out("value", "Effects", GlyphDataType.List) with { ElementType = GlyphDataType.Effect }],
        Exports = [new("nwn.effects", "value")]
    };
    public override string TypeId => Descriptor.TypeId;
    public override GlyphNodeDefinition CreateDefinition() => Descriptor.CreateDefinition();
    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        uint target = await cx.InObject("target");
        List<GlyphNwnEffect> effects = [];
        if (target != NWScript.OBJECT_INVALID)
        {
            for (IntPtr effect = NWScript.GetFirstEffect(target); effect != IntPtr.Zero && NWScript.GetIsEffectValid(effect) != 0; effect = NWScript.GetNextEffect(target))
                effects.Add(new(effect));
        }
        return GlyphNodeResult.Data(new() { ["value"] = effects });
    }
}
