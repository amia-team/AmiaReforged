using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
namespace AmiaReforged.PwEngine.Features.Glyph.Nwn;
[GlyphNode]
public sealed partial class ApplyEffectValueExecutor : GlyphNodeBase
{
    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = "nwn.apply_effect_to_object", DisplayName = "ApplyEffectValue", Category = "NWN / Adapters",
        Description = "Applies an Effect to an object. Positive duration defaults to temporary; zero defaults to permanent. duration_type overrides this selection.", Source = "NWScript.ApplyEffectToObject", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.Action,
        Parameters = [Pins.InObject("target", "Target"), Pins.In("effect", "Effect", GlyphDataType.Effect), Pins.InFloat("duration", "Duration", "0"), Pins.InInt("duration_type", "Duration Type", "-1")],
        Results = [],
        Exports = [new("nwn.apply_effect_to_object"), new("nwn.apply_effect")]
    };
    public override string TypeId => Descriptor.TypeId;
    public override GlyphNodeDefinition CreateDefinition() => Descriptor.CreateDefinition();
    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        uint target = await cx.InObject("target");
        GlyphNwnEffect effect = await cx.In<GlyphNwnEffect>("effect");
        float duration = (float)await cx.InFloat("duration");
        int durationType = await cx.InInt("duration_type", -1);
        if (durationType < 0) durationType = duration > 0 ? NWScript.DURATION_TYPE_TEMPORARY : NWScript.DURATION_TYPE_PERMANENT;
        if (target != NWScript.OBJECT_INVALID && effect.Handle != IntPtr.Zero)
            NWScript.ApplyEffectToObject(durationType, effect.Handle, target, duration);
        return GlyphNodeResult.Continue("exec_out");
    }
}
