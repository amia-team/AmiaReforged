using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Glyph;

[GlyphModule]
public sealed class ResourceNodeGlyphModule : IGlyphModule
{
    public void Configure(GlyphModuleBuilder glyph)
    {
        glyph.Add<SpawnResourceNodeExecutor>();
        glyph.Add<IsResourceNodeTypeExecutor>();
    }
}
