using AmiaReforged.Races.Races.Script.Types;
using NWN.Core;

namespace AmiaReforged.Races.Races.Script;

public static class RaceEffects 
{
    private const string SubracePrefix = "subraceEffect";
    private static uint _player;

    public static void Apply(uint nwnObjectId)
    {
        _player = nwnObjectId;

        if (!RaceIsManaged()) return;

        RemoveTaggedEffects();
        SetEffectsToSupernaturalAndApply();
    }

    private static void RemoveTaggedEffects()
    {
        IntPtr effect = NWScript.GetFirstEffect(_player);

        while (NWScript.GetIsEffectValid(effect) == NWScript.TRUE)
        {
            if (NWScript.GetEffectTag(effect).Contains(SubracePrefix)) NWScript.RemoveEffect(_player, effect);

            effect = NWScript.GetNextEffect(_player);
        }
    }

    private static bool RaceIsManaged()
    {
        int playerRace = NWScript.GetRacialType(_player);

        return ManagedRaces.RaceEffects.ContainsKey(playerRace);
    }

    private static void SetEffectsToSupernaturalAndApply()
    {
        IEnumerable<IntPtr> supernaturalEffects = ConvertEffectsToSupernatural(GetListOfEffectsForRace());
        IEnumerable<IntPtr> taggedEffects = TagEffects(supernaturalEffects);

        foreach (IntPtr effect in taggedEffects) ApplyEffectPermanently(effect);
    }

    private static IEnumerable<IntPtr> ConvertEffectsToSupernatural(IEnumerable<IntPtr> raceEffects)
    {
        return raceEffects.Select(NWScript.SupernaturalEffect).Select(dummy => dummy)
            .ToList();
    }

    private static IEnumerable<IntPtr> GetListOfEffectsForRace()
    {
        int raceType = NWScript.GetRacialType(_player);

        IEffectCollector racialEffectCollector = ManagedRaces.RaceEffects[raceType];

        return racialEffectCollector.GatherEffectsForObject(_player);
    }

    private static IEnumerable<IntPtr> TagEffects(IEnumerable<IntPtr> supernaturalEffects)
    {
        List<IntPtr> taggedEffects = new List<IntPtr>();

        foreach (IntPtr effect in supernaturalEffects)
        {
            string subracePrefixWithCount = SubracePrefix + taggedEffects.Count;
            taggedEffects.Add(NWScript.TagEffect(effect, subracePrefixWithCount));
        }

        return taggedEffects;
    }

    private static void ApplyEffectPermanently(IntPtr effect)
    {
        NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_PERMANENT, effect, _player);
    }
}