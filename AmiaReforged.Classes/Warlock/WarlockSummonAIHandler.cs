using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(WarlockSummonAIHandler))]
public class WarlockSummonAIHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WarlockSummonAIHandler()
    {
        NwModule.Instance.OnAssociateAdd += OnSummonAttackNearest;
        
        Log.Info("Warlock Summon AI Handler initialized.");
    }

    static void OnSummonAttackNearest(OnAssociateAdd obj)
    {
        if (!obj.Associate.ResRef.Contains("wlk")) return;
        if (obj.Associate.AssociateType != AssociateType.Henchman || obj.Associate.AssociateType != AssociateType.Summoned) return;
        
        NwCreature summon = obj.Associate;
        NwCreature warlock = obj.Owner;
        
        if (warlock.IsInCombat)
        {
            NwCreature nearestHostile = summon.GetNearestCreatures().First(creature => creature.IsReactionTypeHostile(summon));
            summon.ActionAttackTarget(nearestHostile);
        }
    }
}
