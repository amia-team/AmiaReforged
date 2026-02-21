using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Module;

[ServiceBinding(typeof(ItemTracingService))]
public class ItemTracingService
{

    public ItemTracingService()
    {
        NwModule.Instance.OnAcquireItem += TrackItem;
    }

    private void TrackItem(ModuleEvents.OnAcquireItem obj)
    {
        if(obj.Item is null) return;
        if(obj.AcquiredBy is not NwCreature) return;
        if(!obj.AcquiredBy.IsPlayerControlled(out NwPlayer? player)) return;


    }

    private record OwnershipRecord(PersonaId AcquiredBy, string AcquiredFrom, PersonaId? PreviousOwner, DateTime AcquiredAt);
}
