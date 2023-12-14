using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class CausticMire
{
    public int Run(uint nwnObjectId)
    {
        IntPtr mire = EffectAreaOfEffect(49);

        IntPtr location = GetSpellTargetLocation();
        float duration = RoundsToSeconds(GetCasterLevel(nwnObjectId));
        NwEffects.RemoveAoeWithTag(location, GetLastSpellCaster(), "VFX_PER_CAUSTMIRE", RADIUS_SIZE_COLOSSAL);
        ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, mire, location, duration);
        return 0;
    }
}