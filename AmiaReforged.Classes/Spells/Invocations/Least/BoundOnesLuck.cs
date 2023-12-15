using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

public class BoundOnesLuck
{
    public void CastBoundOnesLuck(uint nwnObjectId)
    {
        if (GetHasFeat(FEAT_PRESTIGE_DARK_BLESSING, nwnObjectId) == TRUE){
            SendMessageToPC(nwnObjectId, NwEffects.WarlockString("You already have Dark Blessing."));
            return;
        }

        int warlockLevels = GetLevelByClass(57, nwnObjectId);
        int chaMod = GetAbilityModifier(ABILITY_CHARISMA, nwnObjectId);
        int savesBonus = chaMod > warlockLevels ? warlockLevels : chaMod;

        IntPtr luck = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectSavingThrowIncrease(SAVING_THROW_ALL, savesBonus),
            EffectVisualEffect(VFX_DUR_CESSATE_POSITIVE)
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, luck, nwnObjectId, HoursToSeconds(warlockLevels));
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_HEAD_ODD), nwnObjectId);
    }
}