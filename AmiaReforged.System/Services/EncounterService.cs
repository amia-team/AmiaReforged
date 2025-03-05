using AmiaReforged.System.Encounters.Scripts;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(EncounterService))]
public class EncounterService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
        if (!obj.EnteringObject.IsPlayerControlled(out NwPlayer player)) return;
        if (player.IsDM || player.IsPlayerDM) return;
        CreatureSpawner spawner = new(obj.Trigger, player);
        spawner.SpawnCreaturesForTrigger();
    }
}