using AmiaReforged.PwEngine.Database;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.AreaPersistence;

[ServiceBinding(typeof(PersistentPlcSpawner))]
public class PersistentPlcSpawner
{
    private const string PersistPlcLocalInt = "persist_plc";
    private const string SavePlcTag = "persistent_plc_spawner";

    private PlaceablePersistenceService _plcPersistenceService;

    public PersistentPlcSpawner(PlaceablePersistenceService plcPersistenceService)
    {
        _plcPersistenceService = plcPersistenceService;

        NwModule.Instance.OnActivateItem += HandlePersistentSpawner;
    }

    private void HandlePersistentSpawner(ModuleEvents.OnActivateItem obj)
    {
        if(obj.ActivatedItem.Tag !=  SavePlcTag) return;


    }
}
