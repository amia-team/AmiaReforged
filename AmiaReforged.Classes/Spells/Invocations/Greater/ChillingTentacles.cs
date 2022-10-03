using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class ChillingTentacles
{
    public int Run(uint nwnObjectId)
    {
        IntPtr tentacles = EffectAreaOfEffect(51);

        IntPtr location = GetSpellTargetLocation();
        float duration = RoundsToSeconds(GetCasterLevel(nwnObjectId));

        NwEffects.RemoveAoeWithTag(location, GetLastSpellCaster(), "VFX_PER_WLK_TENTACLES", 20.0f);
        ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, tentacles, location, duration);
        return 0;
    }
}