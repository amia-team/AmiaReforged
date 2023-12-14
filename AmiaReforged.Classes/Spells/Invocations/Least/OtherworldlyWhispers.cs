using NWN.Core;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

public class OtherworldlyWhispers
{
    public int Run(uint nwnObjectId)
    {
        const int bonus = 10;
        IntPtr loreup = NWScript.EffectSkillIncrease(NWScript.SKILL_LORE, bonus);
        IntPtr spellcup =
            NWScript.EffectLinkEffects(NWScript.EffectSkillIncrease(NWScript.SKILL_SPELLCRAFT, bonus), loreup);

        NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, spellcup, nwnObjectId,
            NWScript.HoursToSeconds(NWScript.GetCasterLevel(nwnObjectId)));

        return 0;
    }
}