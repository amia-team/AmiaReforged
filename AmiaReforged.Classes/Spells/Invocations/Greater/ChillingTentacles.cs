using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class ChillingTentacles
{
    public void CastChillingTentacles(uint nwnObjectId)
    {
        IntPtr chilling = EffectAreaOfEffect(51);
        IntPtr tentacles = EffectAreaOfEffect(AOE_PER_EVARDS_BLACK_TENTACLES, sOnEnterScript: "wlk_tentent",
            sHeartbeatScript: "wlk_tenthbea", sOnExitScript: "****");

        IntPtr location = GetSpellTargetLocation();
        float duration = RoundsToSeconds(GetCasterLevel(nwnObjectId));

        NwEffects.RemoveAoeWithTag(location, nwnObjectId, aoeTag: "VFX_PER_WLK_CHILLING", RADIUS_SIZE_COLOSSAL);
        NwEffects.RemoveAoeWithTag(location, nwnObjectId, aoeTag: "VFX_PER_EVARDS_BLACK_TENTACLES",
            RADIUS_SIZE_COLOSSAL);
        ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, chilling, location, duration);
        ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, tentacles, location, duration);
    }
}