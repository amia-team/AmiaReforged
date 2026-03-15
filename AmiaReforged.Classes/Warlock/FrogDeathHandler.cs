using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(FrogDeathHandler))]
public class FrogDeathHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public FrogDeathHandler()
    {
        Log.Info(message: "Frog Death Handler initialized.");
    }

    [ScriptHandler(scriptName: "wlk_frog_ondeath")]
    public void OnFrogDeathRussianDoll(CallInfo callInfo)
    {
        if (!callInfo.TryGetEvent(out CreatureEvents.OnDeath? obj)
            || obj.KilledCreature.AssociateType != AssociateType.Summoned
            || IsRussianDoll(obj.KilledCreature.ResRef)
            || obj.KilledCreature.Master is not { } warlock
            || obj.KilledCreature.Location is not { } newSummonLocation) return;

        NwCreature frogSummonKilledInAction = obj.KilledCreature;

        string slaadTier = frogSummonKilledInAction.ResRef;

        double? remainingFrogSeconds =
            warlock.ActiveEffects.FirstOrDefault(e => e.Tag == "frogduration")?.DurationRemaining;

        if (remainingFrogSeconds == null) return;
        TimeSpan remainingFrogDuration = TimeSpan.FromSeconds(remainingFrogSeconds.Value);

        string? nextFrogToSummon = GetNextFrogToSummon(slaadTier);
        if (nextFrogToSummon == null) return;

        _ = SummonNextFrog(warlock, nextFrogToSummon, remainingFrogDuration, newSummonLocation);
    }

    private async Task SummonNextFrog(NwCreature warlock, string nextFrogToSummon, TimeSpan remainingFrogDuration,
        Location summonLocation)
    {
        await warlock.WaitForObjectContext();

        Effect slaadSummon =
            Effect.SummonCreature(nextFrogToSummon, VfxType.ImpPolymorph!, delay: TimeSpan.FromSeconds(2));

        summonLocation.ApplyEffect(EffectDuration.Temporary, slaadSummon, remainingFrogDuration);
    }

    private static bool IsRussianDoll(string creatureResRef)
        => creatureResRef is "wlkslaadblue" or "wlkslaadgreen" or "wlkslaadgray";

    private static string? GetNextFrogToSummon(string summonResRef)
        => summonResRef switch
            {
                "wlkslaadblue" => "wlkslaadred",
                "wlkslaadgreen" => "wlkslaadblue",
                "wlkslaadgray" => "wlkslaadgreen",
                _ => null
            };

}
