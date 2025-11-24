using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaPersistence;

[ServiceBinding(typeof(TriggerAllowanceService))]
public class TriggerAllowanceService
{
    public const string AllowTriggerTag = "allow_plcs";

    public TriggerAllowanceService()
    {
        foreach (NwTrigger nwTrigger in NwObject.FindObjectsWithTag<NwTrigger>(AllowTriggerTag))
        {
            nwTrigger.OnEnter += HandleOnEnter;
        }
    }

    private void HandleOnEnter(TriggerEvents.OnEnter obj)
    {

    }
}
