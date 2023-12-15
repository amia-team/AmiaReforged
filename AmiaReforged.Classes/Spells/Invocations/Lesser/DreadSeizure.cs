using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Lesser;

public class DreadSeizure
{
    public void CastDreadSeizure(uint nwnObjectId)
    {
        int warlockLevels = GetLevelByClass(57, nwnObjectId);
        float duration = RoundsToSeconds(warlockLevels);

        // Remove aura to prevent stacking
        IntPtr aura = GetFirstEffect(nwnObjectId);
        while (GetIsEffectValid(aura) == TRUE){
            if (GetEffectSpellId(aura) == 987)
            RemoveEffect(nwnObjectId, aura);
            aura = GetNextEffect(nwnObjectId);
        }

        ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_FNF_PWKILL), GetLocation(nwnObjectId));
        IntPtr aoe = EffectAreaOfEffect(AOE_MOB_CIRCEVIL, "wlk_dreadenter", "****", "wlk_dreadexit");

        // Apply the VFX impact and effects.
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_IMP_AURA_NEGATIVE_ENERGY), nwnObjectId, duration);
        DelayCommand(0.1f, () => ApplyEffectToObject(DURATION_TYPE_TEMPORARY, aoe, nwnObjectId, duration));
    }
}