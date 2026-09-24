using AmiaReforged.PwEngine.Features.WorldEngine.Application.AreaGraph.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaGraph;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.AreaGraph.Handlers;

[ServiceBinding(typeof(IQueryHandler<GetAreaGraphQuery, AreaGraphData>))]
public sealed class GetAreaGraphQueryHandler : IQueryHandler<GetAreaGraphQuery, AreaGraphData>
{
    private readonly AreaGraphCacheService _cacheService;

    public GetAreaGraphQueryHandler(AreaGraphCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public Task<AreaGraphData> HandleAsync(GetAreaGraphQuery query, CancellationToken cancellationToken = default)
    {
        // Ordinary read: use the cache boundary without forcing a rebuild.
        // Forced refresh (refresh=true / explicit POST) is handled separately.
        return _cacheService.GetOrBuildAsync(forceRefresh: false);
    }
}
