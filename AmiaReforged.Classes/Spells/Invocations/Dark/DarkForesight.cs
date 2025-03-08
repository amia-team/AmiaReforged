using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

public class DarkForesight
{
    public int CastDarkForesight(uint nwnObjectId)
    {
        int foresightLimit = GetCasterLevel(nwnObjectId) > 15 ? 150 : GetCasterLevel(nwnObjectId) * 10;
        float duration = TurnsToSeconds(GetCasterLevel(nwnObjectId));

        IntPtr darkForesight = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectDamageReduction(10, DAMAGE_POWER_PLUS_FIVE, foresightLimit),
            EffectVisualEffect(VFX_DUR_PROT_PREMONITION)
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, darkForesight, nwnObjectId, duration);
        return 0;
    }
}