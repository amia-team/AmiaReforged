using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions.Application;

[ServiceBinding(typeof(IQueryHandler<GetSettlementsForRegionQuery, IReadOnlyCollection<SettlementId>>))]
public class GetSettlementsForRegionQueryHandler(IRegionRepository repository)
    : IQueryHandler<GetSettlementsForRegionQuery, IReadOnlyCollection<SettlementId>>
{
    public Task<IReadOnlyCollection<SettlementId>> HandleAsync(GetSettlementsForRegionQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<SettlementId> settlements = repository.GetSettlements(query.RegionTag);
        return Task.FromResult(settlements);
    }
}

