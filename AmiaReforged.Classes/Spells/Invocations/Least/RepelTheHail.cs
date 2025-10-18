using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

public class RepelTheHail
{
    public int CastRepelTheHail(uint nwnObjectId)
    {
        int warlockLevels = GetLevelByClass(57, nwnObjectId);
        int chaModBonus = GetAbilityModifier(ABILITY_CHARISMA, nwnObjectId) / 2;
        int concealment = 25 + warlockLevels + chaModBonus;

        IntPtr repelHailEffect = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectSkillIncrease(SKILL_MOVE_SILENTLY, 4),
            EffectSkillIncrease(SKILL_HIDE, 4),
            EffectVisualEffect(VFX_DUR_GLOBE_MINOR),
            EffectConcealment(concealment, MISS_CHANCE_TYPE_VS_RANGED)
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, repelHailEffect, nwnObjectId, TurnsToSeconds(warlockLevels));

        return 0;
    }
}
