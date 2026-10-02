using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Glyph;

[ServiceBinding(typeof(IGlyphModule))]
public sealed class RegionGlyphModule(IQueryDispatcher queries) : IGlyphModule
{
    public void Configure(GlyphModuleBuilder glyph) => glyph.Add(new GetRegionExecutor(queries));
}
