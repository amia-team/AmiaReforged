using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using NWN.Core;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

public class BoundOnesLuck
{
    public void CastBoundOnesLuck(uint nwnObjectId)
    {
        if (GetHasFeat(FEAT_PRESTIGE_DARK_BLESSING, nwnObjectId) == TRUE)
        {
            SendMessageToPC(nwnObjectId, WarlockUtils.String(message: "You already have Dark Blessing."));
            return;
        }

        int warlockLevels = GetLevelByClass(57, nwnObjectId);
        int savesCap = warlockLevels / 7;

        if (warlockLevels == 30)
            savesCap += 6;

        int save = Math.Min(savesCap, GetAbilityModifier(ABILITY_CHARISMA, nwnObjectId));

        IntPtr luck = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectSavingThrowIncrease(SAVING_THROW_ALL, save),
            EffectVisualEffect(VFX_DUR_CESSATE_POSITIVE)
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, luck, nwnObjectId, HoursToSeconds(warlockLevels));
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_HEAD_ODD), nwnObjectId);
    }
}
