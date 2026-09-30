using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem.Glyph;

[GlyphModule]
public sealed class KnowledgeGlyphModule : IGlyphModule
{
    public void Configure(GlyphModuleBuilder glyph)
    {
        glyph.Add<HasKnowledgeExecutor>();
        glyph.Add<HasUnlockedInteractionExecutor>();
        glyph.Add<GetKnowledgeProgressionExecutor>();
        glyph.Add<GetLearnedKnowledgeExecutor>();
    }
}
