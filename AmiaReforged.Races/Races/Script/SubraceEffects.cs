using AmiaReforged.Races.Races.Types.RacialEffects;
using NLog;
using NWN.Core;

namespace AmiaReforged.Races.Races.Script;

public static class SubraceEffects 
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static void Apply(uint nwnObjectId)
    {
        Log.Info("Applying subrace effects");
        List<IntPtr> listOfSubraceEffects = ResolveEffectsForObject(nwnObjectId);

        if (!listOfSubraceEffects.Any())
        {
            Log.Info("No subrace effects found");
            return;
        }

        EffectApplier.Apply(nwnObjectId, listOfSubraceEffects);
        Log.Info("Subrace effects applied");
    }

    private static List<IntPtr> ResolveEffectsForObject(uint nwnObjectId)
    {
        Log.Info("Resolving subrace effects");
        string lower = NWScript.GetSubRace(nwnObjectId).ToLower();
        Log.Info(lower);
        return lower switch
        {
            "aasimar" => new AasimarEffects().GatherEffectsForObject(nwnObjectId),
            "tiefling" => new TieflingEffects().GatherEffectsForObject(nwnObjectId),
            "fey'ri" => new FeyriEffects().GatherEffectsForObject(nwnObjectId),
            "feyri" => new FeyriEffects().GatherEffectsForObject(nwnObjectId),
            "feytouched" => new FeytouchedEffects().GatherEffectsForObject(nwnObjectId),
            "centaur" => new CentaurEffects().GatherEffectsForObject(nwnObjectId),
            "avariel" => new AvarielEffects().GatherEffectsForObject(nwnObjectId),
            "half dragon" => new HalfDragonEffects().GatherEffectsForObject(nwnObjectId),
            "half-dragon" => new HalfDragonEffects().GatherEffectsForObject(nwnObjectId),
            "dragon" => new HalfDragonEffects().GatherEffectsForObject(nwnObjectId),
            _ => new()
        };
    }
}