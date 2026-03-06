using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;

/// <summary>
///     Bootstraps the trait subsystem by loading trait definitions from the database
///     into the in-memory <see cref="ITraitRepository"/> on server startup.
/// </summary>
[ServiceBinding(typeof(TraitBootstrapService))]
public class TraitBootstrapService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public TraitBootstrapService(TraitDefinitionLoadingService loader, ITraitRepository repository)
    {
        Log.Info("=== Trait Subsystem Bootstrap ===");

        loader.Load();

        int traitCount = repository.All().Count;
        Log.Info($"Loaded {traitCount} trait definition(s) from database.");

        if (loader.Failures().Count > 0)
        {
            foreach (var failure in loader.Failures())
            {
                Log.Warn($"Trait load failure: {failure.Message}");
            }
        }

        Log.Info("=== Trait Subsystem Bootstrap Complete ===");
    }
}
