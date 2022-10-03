using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

public class WordOfChanging
{
    public int Run(uint nwnObjectId)
    {
        int casterLevel = GetCasterLevel(nwnObjectId);
        int ab = (int)(casterLevel / 4 > 5 ? 5 : casterLevel / 4);
        float duration = RoundsToSeconds(casterLevel);

        IntPtr changingEffects = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectAttackIncrease(ab),
            EffectAbilityIncrease(ABILITY_STRENGTH, d4()),
            EffectAbilityIncrease(ABILITY_CONSTITUTION, d4()),
            EffectAbilityIncrease(ABILITY_DEXTERITY, d4()),
            EffectTemporaryHitpoints(d6(casterLevel)),
            EffectSpellFailure(),
            EffectVisualEffect(VFX_DUR_GHOST_SMOKE),
            EffectVisualEffect(VFX_DUR_GLOW_BLUE)
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, changingEffects, nwnObjectId, duration);
        return 0;
    }
}
