using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Nwn;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Traits;

[GlyphNode]
public sealed partial class AddTraitEffectExecutor : GlyphNodeBase
{
    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = "trait.add_effect", DisplayName = "Add Trait Effect", Category = "Traits",
        Description = "Contributes an NWN Effect to this trait's resolution. The trait system tags and applies it permanently as supernatural, and removes it on the next rebuild. Unavailable during death.",
        Archetype = GlyphNodeArchetype.Action,
        RestrictToEventType = GlyphEventType.TraitEffectResolution, ScriptCategory = GlyphScriptCategory.Trait,
        Parameters = [Pins.In("effect", "Effect", GlyphDataType.Effect)],
        Exports = [new("trait.add_effect", AllowedStages: ["main", "client_enter", "level_up", "respawn", "confirmed"])]
    };
    public override string TypeId => Descriptor.TypeId;
    public override GlyphNodeDefinition CreateDefinition() => Descriptor.CreateDefinition();

    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        if (cx.Execution.CurrentPipelineStage == TraitDeathStageExecutor.NodeTypeId)
            throw new InvalidOperationException("Trait effects cannot be contributed during death.");
        TraitGlyphContext trait = cx.Execution.Get<TraitGlyphContext>()
            ?? throw new InvalidOperationException("Trait effect resolution context is required.");
        GlyphNwnEffect effect = await cx.In<GlyphNwnEffect>("effect");
        if (effect.Handle != IntPtr.Zero) trait.Effects.Add(effect);
        return GlyphNodeResult.Continue("exec_out");
    }
}
