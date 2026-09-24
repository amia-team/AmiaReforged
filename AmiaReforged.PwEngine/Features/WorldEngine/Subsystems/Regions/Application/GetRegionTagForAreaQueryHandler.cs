using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Application;

[ServiceBinding(typeof(IQueryHandler<GetRegionTagForAreaQuery, string?>))]
public sealed class GetRegionTagForAreaQueryHandler(IRegionRepository repository)
    : IQueryHandler<GetRegionTagForAreaQuery, string?>
{
    // Delegates to the repository's canonical area→region lookup so matching semantics stay
    // single-sourced and consistent with the sibling GetChaosForAreaQueryHandler (task 037).
    public Task<string?> HandleAsync(GetRegionTagForAreaQuery query, CancellationToken cancellationToken = default)
        => Task.FromResult(
            repository.TryGetRegionForArea(query.AreaResRef, out RegionDefinition? region) && region is not null
                ? region.Tag.Value
                : null);
}
