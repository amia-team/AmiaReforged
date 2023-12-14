using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

public class DarkForesight
{
    public int Run(uint nwnObjectId)
    {
        int casterLevel = GetCasterLevel(nwnObjectId);
        float duration = RoundsToSeconds(casterLevel);

        IntPtr darkForesight = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectDamageReduction(10, DAMAGE_POWER_PLUS_FIVE, casterLevel),
            EffectVisualEffect(VFX_DUR_PROT_PREMONITION)
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, darkForesight, nwnObjectId, duration);
        return 0;
    }
}
