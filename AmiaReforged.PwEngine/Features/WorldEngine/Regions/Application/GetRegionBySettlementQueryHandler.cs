using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions.Application;

[ServiceBinding(typeof(IQueryHandler<GetRegionBySettlementQuery, RegionDefinition?>))]
public class GetRegionBySettlementQueryHandler(IRegionRepository repository)
    : IQueryHandler<GetRegionBySettlementQuery, RegionDefinition?>
{
    public Task<RegionDefinition?> HandleAsync(GetRegionBySettlementQuery query, CancellationToken cancellationToken = default)
    {
        bool found = repository.TryGetRegionBySettlement(query.SettlementId, out RegionDefinition? region);
        return Task.FromResult(found ? region : null);
    }
}

