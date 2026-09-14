using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Traits.Commands;

/// <summary>
/// Best-effort synchronizer for the in-memory trait-definition cache.
/// Trait writes go to the database; the runtime reads from
/// <see cref="ITraitRepository"/>, so handlers refresh it after every mutation.
/// Failures never fail the command — the cache rebuilds on next server restart.
/// </summary>
[ServiceBinding(typeof(TraitDefinitionCacheRefresher))]
public sealed class TraitDefinitionCacheRefresher
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly TraitDefinitionMapper _mapper;
    private readonly ITraitRepository _repository;

    public TraitDefinitionCacheRefresher(TraitDefinitionMapper mapper, ITraitRepository repository)
    {
        _mapper = mapper;
        _repository = repository;
    }

    public void Refresh(PersistedTraitDefinition persisted)
    {
        try
        {
            Trait trait = _mapper.ToDomain(persisted);
            _repository.Add(trait);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to refresh in-memory trait cache for '{Tag}'", persisted.Tag);
        }
    }

    public void Remove(string traitTag)
    {
        try
        {
            _repository.Remove(traitTag);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to remove trait '{Tag}' from in-memory cache", traitTag);
        }
    }
}
