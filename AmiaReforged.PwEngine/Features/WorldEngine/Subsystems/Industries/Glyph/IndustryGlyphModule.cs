using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Glyph;

[GlyphModule]
public sealed class IndustryGlyphModule : IGlyphModule
{
    public void Configure(GlyphModuleBuilder glyph)
    {
        glyph.Add<GetIndustryMembershipsExecutor>();
        glyph.Add<GetIndustryLevelExecutor>();
        glyph.Add<IsIndustryMemberExecutor>();
    }
}
