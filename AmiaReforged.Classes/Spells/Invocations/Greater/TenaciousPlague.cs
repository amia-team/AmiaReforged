using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class TenaciousPlague
{
    public int CastTenaciousPlague(uint nwnObjectId)
    {
        IntPtr areaOfEffect = EffectAreaOfEffect(50);

        IntPtr location = GetSpellTargetLocation();
        float duration = RoundsToSeconds(3);

        ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, areaOfEffect, location, duration);
        return 0;
    }
}