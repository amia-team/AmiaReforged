using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

public class BoundOnesLuck
{
    public void CastBoundOnesLuck(uint nwnObjectId)
    {
        if (GetHasFeat(FEAT_PRESTIGE_DARK_BLESSING, nwnObjectId) == TRUE)
        {
            SendMessageToPC(nwnObjectId, WarlockConstants.String(message: "You already have Dark Blessing."));
            return;
        }

        int warlockLevels = GetLevelByClass(57, nwnObjectId);
        int savesBonus = warlockLevels / 7;

        if (warlockLevels == 30)
            savesBonus = 5;

        IntPtr luck = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectSavingThrowIncrease(SAVING_THROW_ALL, savesBonus),
            EffectVisualEffect(VFX_DUR_CESSATE_POSITIVE)
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, luck, nwnObjectId, HoursToSeconds(warlockLevels));
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_HEAD_ODD), nwnObjectId);
    }
}