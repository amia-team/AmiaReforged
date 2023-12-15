using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

public class LeapsAndBounds
{
    public int CastLeapsAndBounds(uint nwnObjectId)
    {
        int warlockLevels = GetLevelByClass(57, nwnObjectId);

        IntPtr leaps = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectAbilityIncrease(ABILITY_DEXTERITY, 4),
            EffectSkillIncrease(SKILL_TUMBLE, 8),
            EffectVisualEffect(VFX_DUR_CESSATE_POSITIVE)
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, leaps, nwnObjectId, HoursToSeconds(warlockLevels));
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_IMPROVE_ABILITY_SCORE), nwnObjectId);

        return 0;
    }
}