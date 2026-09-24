using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Application;

[ServiceBinding(typeof(IQueryHandler<IsAreaInRegionQuery, bool>))]
public sealed class IsAreaInRegionQueryHandler(IRegionRepository repository)
    : IQueryHandler<IsAreaInRegionQuery, bool>
{
    // Delegates to the repository's canonical area→region lookup so matching semantics stay
    // single-sourced and consistent with the sibling GetChaosForAreaQueryHandler (task 037).
    public Task<bool> HandleAsync(IsAreaInRegionQuery query, CancellationToken cancellationToken = default)
        => Task.FromResult(repository.IsAreaRegistered(query.AreaResRef));
}
