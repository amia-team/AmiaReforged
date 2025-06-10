using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(WarlockSummonAiHandler))]
public class WarlockSummonAiHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WarlockSummonAiHandler()
    {
        NwModule.Instance.OnAssociateAdd += OnSummonAttackNearest;

        Log.Info(message: "Warlock Summon AI Handler initialized.");
    }

    private static void OnSummonAttackNearest(OnAssociateAdd obj)
    {
        if (!obj.Associate.ResRef.Contains("wlk")) return;

        NwCreature summon = obj.Associate;
        NwCreature warlock = obj.Owner;

        if (!warlock.IsInCombat) return;
        
        NwCreature nearestHostile =
            summon.GetNearestCreatures().First(creature => creature.IsReactionTypeHostile(summon));
        summon.ActionAttackTarget(nearestHostile);
    }
}