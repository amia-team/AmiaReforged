using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;

[GlyphNode]
public partial class OnTraitGrantedEventExecutor : GlyphEventNode
{
    public const string NodeTypeId = "event.on_trait_granted";
    public static GlyphEventDescriptor Event { get; } = new("trait.on_granted", GlyphEventType.OnTraitGranted,
        GlyphScriptCategory.Trait, NodeTypeId, Capabilities: [typeof(TraitGlyphContext), typeof(GlyphCharacterContext)]);
    public static GlyphContextSchema Context { get; } = new([
        new("character_id", "Character ID", GlyphDataType.String,
            ctx => ctx.Get<GlyphCharacterContext>() is { } data ? data.CharacterId ?? string.Empty : string.Empty),
        new("trait_tag", "Trait Tag", GlyphDataType.String,
            ctx => ctx.Get<TraitGlyphContext>() is { } data ? data.TraitTag ?? string.Empty : string.Empty),
        new("target_creature", "Target Creature", GlyphDataType.NwObject,
            ctx => ctx.Get<TraitGlyphContext>() is { } data ? data.TargetCreature : 0u, Aliases: ["creature"]),
    ]);
    protected override GlyphEventDescriptor EventContract => Event;
    public override GlyphContextSchema Schema => Context;
    public override string SourceDisplayName => "On Trait Granted";
    protected override string Description => "Entry point for scripts that run when a trait is granted to a character. " +
                      "Provides the character ID, trait tag, and target creature reference.";
}
