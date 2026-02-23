using AmiaReforged.System.Encounters.Scripts;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(EncounterService))]
public class EncounterService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Must match <c>DynamicEncounterService.DynamicHandledFlag</c> in AmiaReforged.PwEngine.
    /// When the dynamic system handles a trigger, it sets this local int to TRUE so the
    /// legacy system skips.
    /// </summary>
    private const string DynamicHandledFlag = "dynamic_handled";

    public EncounterService()
    {
        IEnumerable<NwTrigger> triggers = NwObject.FindObjectsWithTag<NwTrigger>("db_spawntrigger");

        foreach (NwTrigger nwTrigger in triggers)
        {
            nwTrigger.OnEnter += SpawnTriggerOnEnter;
        }

        Log.Info(message: "Encounter service initialized.");
    }

    private static void SpawnTriggerOnEnter(TriggerEvents.OnEnter obj)
    {
        // If the dynamic encounter system already handled this trigger event, skip.
        if (NWScript.GetLocalInt(obj.Trigger, DynamicHandledFlag) == NWScript.TRUE) return;

        if (!obj.EnteringObject.IsPlayerControlled(out NwPlayer player)) return;
        if (player.IsDM || player.IsPlayerDM) return;
        CreatureSpawner spawner = new(obj.Trigger, player);
        spawner.SpawnCreaturesForTrigger();
    }
}
