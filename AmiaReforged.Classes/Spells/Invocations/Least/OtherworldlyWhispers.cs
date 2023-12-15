using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

public class OtherworldlyWhispers
{
    public int CastOtherworldlyWhispers(uint nwnObjectId)
    {
        int warlockLevels = GetLevelByClass(57, nwnObjectId);
        const int bonus = 10;

        IntPtr whispers = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectSkillIncrease(SKILL_LORE, bonus),
            EffectSkillIncrease(SKILL_SPELLCRAFT, bonus),
            EffectVisualEffect(VFX_DUR_CESSATE_POSITIVE)
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, whispers, nwnObjectId, HoursToSeconds(warlockLevels));
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_NIGHTMARE_HEAD_HIT), nwnObjectId);

        return 0;
    }
}