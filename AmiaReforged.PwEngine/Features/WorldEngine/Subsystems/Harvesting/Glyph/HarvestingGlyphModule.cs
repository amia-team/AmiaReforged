using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Glyph;

[ServiceBinding(typeof(IGlyphModule))]
public class HarvestingGlyphModule(IQueryDispatcher queries) : IGlyphModule
{
    public void Configure(GlyphModuleBuilder glyph)
    {
        glyph.Add(new GetAreaHasNodeTypeExecutor(queries));
    }
}
