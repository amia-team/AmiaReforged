using NWN.Core;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

public class LeapsAndBounds
{
    public int Run(uint nwnObjectId)
    {
        IntPtr dexup = NWScript.EffectAbilityIncrease(NWScript.ABILITY_DEXTERITY, 4);
        IntPtr tumup = NWScript.EffectLinkEffects(NWScript.EffectSkillIncrease(NWScript.SKILL_TUMBLE, 4), dexup);

        NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, tumup, nwnObjectId,
            NWScript.HoursToSeconds(NWScript.GetCasterLevel(nwnObjectId)));

        return 0;
    }
}