using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions.Application;

[ServiceBinding(typeof(IQueryHandler<GetAllRegionsQuery, List<RegionDefinition>>))]
public class GetAllRegionsQueryHandler(IRegionRepository repository)
    : IQueryHandler<GetAllRegionsQuery, List<RegionDefinition>>
{
    public Task<List<RegionDefinition>> HandleAsync(GetAllRegionsQuery query, CancellationToken cancellationToken = default)
    {
        List<RegionDefinition> regions = repository.All();
        return Task.FromResult(regions);
    }
}

