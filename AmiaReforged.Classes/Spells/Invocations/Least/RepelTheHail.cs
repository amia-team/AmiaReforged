using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

public class RepelTheHail
{
    public int Run(uint nwnObjectId)
    {
        IntPtr repelHailEffect = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectSkillIncrease(SKILL_MOVE_SILENTLY, 4),
            EffectSkillIncrease(SKILL_HIDE, 4),
            EffectVisualEffect(VFX_DUR_GLOBE_MINOR),
            EffectConcealment(20, MISS_CHANCE_TYPE_VS_RANGED)
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, repelHailEffect, nwnObjectId,
            TurnsToSeconds(GetCasterLevel(nwnObjectId)));

        return 0;
    }
}