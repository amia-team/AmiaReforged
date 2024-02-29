using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Types;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Lesser;

public class FleeTheScene
{
    public void CastFleeTheScene(uint nwnObjectId)
    {
        int warlockLevels = GetLevelByClass(57, nwnObjectId);
        float duration = RoundsToSeconds(warlockLevels);

        IntPtr haste = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectVisualEffect(VFX_DUR_CESSATE_POSITIVE),
            EffectHaste(),
            EffectBonusFeat(FEAT_UNCANNY_DODGE_1)
        });
        IntPtr sanctuary = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectSanctuary(Warlock.CalculateDC(nwnObjectId)),
            EffectVisualEffect(VFX_DUR_SANCTUARY)
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, haste, nwnObjectId, duration);
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, sanctuary, nwnObjectId, 3f);
    }
}