using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.Services;

[ServiceBinding(typeof(WarlockTestHandler))]
public class WarlockTestHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WarlockTestHandler()
    {
        NwModule.Instance.OnClientEnter += OnLoginGiveTestGoodies;

        Log.Info("Warlock Test Handler initialized.");
    }

    private async void OnLoginGiveTestGoodies(ModuleEvents.OnClientEnter obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Player.LoginCreature) <= 0) return;
        NwCreature? warlock = obj.Player.LoginCreature;

        if (!warlock.Name.Contains("Testypoo")) return;

        // add all pact summon spells
        for (int i = 1323; i <= 1328; i++)
        {
            warlock.AddFeat(NwFeat.FromFeatId(i));
            if (i == 1327) obj.Player.SendServerMessage("The eldritch has granted you testypoo powers. Comment out test handler when ready, buttface.");
        }

        warlock.GiveGold(10000000);
        if (warlock.IsDead) warlock.ApplyEffect(EffectDuration.Instant, Effect.Resurrection());
        warlock.HP = warlock.MaxHP;

        await NwTask.Delay(TimeSpan.FromMinutes(1));
        obj.Player.GiveXp(435000);
    }

}