using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Application;

[ServiceBinding(typeof(IQueryHandler<GetChaosForAreaQuery, ChaosState>))]
public class GetChaosForAreaQueryHandler(IRegionRepository repository)
    : IQueryHandler<GetChaosForAreaQuery, ChaosState>
{
    public Task<ChaosState> HandleAsync(GetChaosForAreaQuery query, CancellationToken cancellationToken = default)
    {
        // Unregistered areas receive no chaos state — only the default (all zeros).
        if (!repository.TryGetRegionForArea(query.AreaResRef, out RegionDefinition? region) || region is null)
        {
            return Task.FromResult(ChaosState.Default);
        }

        // Area-level chaos override takes precedence (case-insensitive area matching).
        AreaDefinition? areaDef = region.Areas
            .FirstOrDefault(a => string.Equals(a.ResRef.Value, query.AreaResRef, StringComparison.OrdinalIgnoreCase));

        ChaosState? areaChaos = areaDef?.Environment.Chaos;
        if (areaChaos is { })
        {
            return Task.FromResult(areaChaos);
        }

        // Fall back to the region's default chaos, or ChaosState.Default.
        return Task.FromResult(region.DefaultChaos ?? ChaosState.Default);
    }
}
