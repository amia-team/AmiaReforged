using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;

/// <summary>
/// Loads trait definitions from the database into the in-memory <see cref="ITraitRepository"/>.
/// Used at startup by <see cref="TraitBootstrapService"/> to populate the in-memory cache.
/// </summary>
[ServiceBinding(typeof(TraitDefinitionLoadingService))]
public class TraitDefinitionLoadingService(
    ITraitRepository repository,
    PwContextFactory contextFactory,
    TraitDefinitionMapper mapper)
{
    private readonly List<string> _failures = [];

    public void Load()
    {
        _failures.Clear();

        try
        {
            LoadFromDatabase();
        }
        catch (Exception ex)
        {
            _failures.Add($"Database load failed: {ex.Message}");
        }
    }

    private void LoadFromDatabase()
    {
        using PwEngineContext ctx = contextFactory.CreateDbContext();

        List<PersistedTraitDefinition> definitions = ctx.TraitDefinitions.ToList();

        foreach (PersistedTraitDefinition persisted in definitions)
        {
            try
            {
                Trait trait = mapper.ToDomain(persisted);
                // Use cache-only add to avoid re-persisting data we just read
                if (repository is DbTraitRepository dbRepo)
                    dbRepo.AddToCache(trait);
                else
                    repository.Add(trait);
            }
            catch (Exception ex)
            {
                _failures.Add($"Failed to map trait '{persisted.Tag}': {ex.Message}");
            }
        }
    }

    public List<string> Failures() => _failures;
}
