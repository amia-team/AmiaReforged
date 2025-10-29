using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions.Application;

[ServiceBinding(typeof(IQueryHandler<GetRegionByTagQuery, RegionDefinition?>))]
public class GetRegionByTagQueryHandler(IRegionRepository repository)
    : IQueryHandler<GetRegionByTagQuery, RegionDefinition?>
{
    public Task<RegionDefinition?> HandleAsync(GetRegionByTagQuery query, CancellationToken cancellationToken = default)
    {
        RegionDefinition? region = repository.All().FirstOrDefault(r => r.Tag.Value == query.Tag.Value);
        return Task.FromResult(region);
    }
}
