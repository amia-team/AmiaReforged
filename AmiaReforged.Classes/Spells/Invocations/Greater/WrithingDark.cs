using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class WrithingDark
{
    public int Run(uint nwnObjectId)
    {
        string DARKNESS_PROHIBITED = "NO_DARKNESS";
        if (GetLocalInt(GetArea(GetLastSpellCaster()), DARKNESS_PROHIBITED) == TRUE) {
            SendMessageToPC(GetLastSpellCaster(), "The Darkness Spell Fizzles!");
            return 0;
        }
            
        IntPtr shadows = EffectAreaOfEffect(48);
        IntPtr darkness = EffectAreaOfEffect(AOE_PER_DARKNESS);

        IntPtr location = GetSpellTargetLocation();
        float duration = RoundsToSeconds(GetCasterLevel(nwnObjectId));

        NwEffects.RemoveAoeWithTag(location, GetLastSpellCaster(), "VFX_PER_WLK_DARK", RADIUS_SIZE_COLOSSAL);
        NwEffects.RemoveAoeWithTag(location, GetLastSpellCaster(), "VFX_PER_DARKNESS", RADIUS_SIZE_COLOSSAL);
        ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, shadows, location, duration);
        ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, darkness, location, duration);

        return 0;
    }
}
