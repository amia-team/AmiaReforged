using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

public class SeeTheUnseen
{
    public int CastSeeTheUnseen(uint nwnObjectId)
    {
        IntPtr seeUnseenEffects = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectVisualEffect(VFX_DUR_MAGICAL_SIGHT),
            EffectSeeInvisible(),
            EffectUltravision()
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, seeUnseenEffects, nwnObjectId, HoursToSeconds(GetCasterLevel(nwnObjectId)));
        return 0;
    }
}