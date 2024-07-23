using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Nui;
using AmiaReforged.PwEngine.Systems.WindowingSystem;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Storage;

[ServiceBinding(typeof(StorageRegistrationService))]
public class StorageRegistrationService
{
    private readonly NuiManager _manager;
    private const string PwJobStorage = "pw_job_storage";

    public StorageRegistrationService(NuiManager manager)
    {
        _manager = manager;

        SetupStorageContainers();
    }

    private void SetupStorageContainers()
    {
        IEnumerable<NwPlaceable> storageObjects =
            NwObject.FindObjectsOfType<NwPlaceable>().Where(p => p.Tag == PwJobStorage).ToArray();

        foreach (NwPlaceable storageObject in storageObjects)
        {
            storageObject.OnUsed += OpenStorage;
        }
    }

    private void OpenStorage(PlaceableEvents.OnUsed obj)
    {
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;

        player.SendServerMessage(
            "You click da fingy. Also, if this is in production, file a bug report so we know to get rid of this message!");
        // This is here to avoid undefined behavior with the ledger. The ledger is operated in two modes:
        // A player ledger view, and a DM ledger view.
        if (obj.UsedBy.IsDMPossessed)
        {
            NwModule.Instance.SendMessageToAllDMs("You cannot use a ledger while possessing an NPC.");
        }

        if (player.IsDM)
        {
            // TODO: Use the DM ledger view for DMs, when it is implemented.
            player.SendServerMessage("DM ledger view not implemented yet.");
            return;
        }

        if (_manager.WindowIsOpen(player, typeof(LedgerController)))
            return;

        _manager.OpenWindow<LedgerView>(player);
    }
}