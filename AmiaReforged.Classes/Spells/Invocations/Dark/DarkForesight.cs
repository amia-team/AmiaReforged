using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

public class DarkForesight
{
    public int CastDarkForesight(uint nwnObjectId)
    {
        int reduced = 10 + GetAbilityModifier(ABILITY_CHARISMA, nwnObjectId);

        int foresightLimit = GetCasterLevel(nwnObjectId) * reduced;
        float duration = TurnsToSeconds(GetCasterLevel(nwnObjectId));

        IntPtr darkForesight = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectDamageReduction(reduced, DAMAGE_POWER_PLUS_FIVE, foresightLimit),
            EffectVisualEffect(VFX_DUR_PROT_PREMONITION)
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, darkForesight, nwnObjectId, duration);
        return 0;
    }
}
