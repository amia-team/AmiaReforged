﻿using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class WrithingDark
{
    public void CastWrithingDark(uint nwnObjectId)
    {
        IntPtr aoe = NwEffects.LinkEffectList(new List<IntPtr>
            {
                EffectAreaOfEffect(AOE_PER_DARKNESS, "****", "****", "****"),
                EffectAreaOfEffect(48)  // VFX_PER_WLK_DARK
            });

        IntPtr location = GetSpellTargetLocation();
        float duration = RoundsToSeconds(GetCasterLevel(nwnObjectId));

        NwEffects.RemoveAoeWithTag(location, nwnObjectId, "VFX_PER_WLK_DARK", RADIUS_SIZE_COLOSSAL);
        NwEffects.RemoveAoeWithTag(location, nwnObjectId, "VFX_PER_DARKNESS", RADIUS_SIZE_COLOSSAL);
        ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, aoe, location, duration);
    }
}
