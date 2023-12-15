using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class WallOfPerilousFlame
{
    public void CastWallOfFlame(uint nwnObjectId)
    {
        IntPtr areaOfEffect = EffectAreaOfEffect(47);

        IntPtr location = GetSpellTargetLocation();
        float duration = RoundsToSeconds(6);

        ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, areaOfEffect, location, duration);
    }
}