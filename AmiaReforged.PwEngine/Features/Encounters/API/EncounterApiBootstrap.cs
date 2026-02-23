using AmiaReforged.PwEngine.Features.Encounters.Services;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Encounters.API;

/// <summary>
/// Bootstraps the encounter API by setting static service references on the controller.
/// The <see cref="SpawnProfileController"/> uses static methods (required by the route table's
/// reflection-based discovery), so dependencies must be provided via static fields.
/// </summary>
[ServiceBinding(typeof(EncounterApiBootstrap))]
public class EncounterApiBootstrap
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EncounterApiBootstrap(
        ISpawnProfileRepository repository,
        DynamicEncounterService encounterService)
    {
        SpawnProfileController.Repository = repository;
        SpawnProfileController.EncounterService = encounterService;

        Log.Info("Encounter API bootstrap complete — controller wired to services.");
    }
}
