using AmiaReforged.Races.Races.Types.RacialEffects;
using NWN.Core;

namespace AmiaReforged.Races.Races.Script
{
    public static class SubraceEffects 
    {
        public static void Apply(uint nwnObjectId)
        {
            List<IntPtr> listOfSubraceEffects = ResolveEffectsForObject(nwnObjectId);

            if (!listOfSubraceEffects.Any()) return;

            EffectApplier.Apply(nwnObjectId, listOfSubraceEffects);
        }

        private static List<IntPtr> ResolveEffectsForObject(uint nwnObjectId)
        {
            return NWScript.GetSubRace(nwnObjectId).ToLower() switch
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
                _ => new List<IntPtr>()
            };
        }
    }
}