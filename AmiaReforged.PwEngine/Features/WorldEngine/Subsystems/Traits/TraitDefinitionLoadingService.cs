using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;

/// <summary>
/// Loads trait definitions from the database into the in-memory <see cref="ITraitRepository"/>.
/// Falls back to JSON file loading if no database definitions exist and RESOURCE_PATH is set.
/// </summary>
[ServiceBinding(typeof(TraitDefinitionLoadingService))]
public class TraitDefinitionLoadingService(
    ITraitRepository repository,
    PwContextFactory contextFactory,
    TraitDefinitionMapper mapper) : IDefinitionLoader
{
    private readonly List<FileLoadResult> _failures = [];

    public void Load()
    {
        _failures.Clear();

        try
        {
            LoadFromDatabase();
        }
        catch (Exception ex)
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, $"Database load failed: {ex.Message}"));
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
                repository.Add(trait);
            }
            catch (Exception ex)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail, $"Failed to map trait '{persisted.Tag}': {ex.Message}"));
            }
        }
    }

    public List<FileLoadResult> Failures()
    {
        return _failures;
    }
}
