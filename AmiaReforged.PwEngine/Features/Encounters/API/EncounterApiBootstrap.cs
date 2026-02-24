using AmiaReforged.PwEngine.Features.Encounters.Services;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Encounters.API;

/// <summary>
/// Bootstraps the encounter API by setting static service references on the controllers.
/// The controllers use static methods (required by the route table's
/// reflection-based discovery), so dependencies must be provided via static fields.
/// </summary>
[ServiceBinding(typeof(EncounterApiBootstrap))]
public class EncounterApiBootstrap
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EncounterApiBootstrap(
        ISpawnProfileRepository repository,
        DynamicEncounterService encounterService,
        IMutationRepository mutationRepository,
        MutationApplicator mutationApplicator)
    {
        SpawnProfileController.Repository = repository;
        SpawnProfileController.EncounterService = encounterService;

        MutationController.Repository = mutationRepository;
        MutationController.Applicator = mutationApplicator;

        // Pre-load mutation cache
        _ = mutationApplicator.RefreshCacheAsync();

        Log.Info("Encounter API bootstrap complete — controllers wired to services.");
    }
}
