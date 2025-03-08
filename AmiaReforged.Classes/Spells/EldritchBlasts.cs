using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock.Types;
using AmiaReforged.Classes.Warlock.Types.EssenceEffects;
using AmiaReforged.Classes.Warlock.Types.Shapes;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells;

public class EldritchBlasts
{
    public void CastEldritchBlasts(uint nwnObjectId)
    {
        int spellId = GetSpellId();
        EssenceType essence = (EssenceType)GetLocalInt(GetItemPossessedBy(nwnObjectId, sItemTag: "ds_pckey"),
            sVarName: "warlock_essence");
        uint targetObject = GetSpellTargetObject();
        bool hasEldritchMastery = GetHasFeat(1298, nwnObjectId) == TRUE;

        if (NwEffects.IsPolymorphed(nwnObjectId))
        {
            SendMessageToPC(nwnObjectId, szMessage: "You cannot cast while polymorphed.");
            return;
        }

        EssenceEffectApplier essenceEffects =
            EssenceEffectFactory.CreateEssenceEffect(essence, targetObject, nwnObjectId);

        switch (spellId)
        {
            case 981:
                EldritchBlast.CastEldritchBlast(nwnObjectId, targetObject, essence, essenceEffects);
                break;
            case 982:
                EldritchSpear.CastEldritchSpear(nwnObjectId, targetObject, essence, essenceEffects);
                break;
            case 1003:
                EldritchDoom.CastEldritchDoom(nwnObjectId, GetSpellTargetLocation(), essence);
                break;
            case 1004:
                EldritchPulse.CastEldritchPulse(nwnObjectId, targetObject, essence, essenceEffects);
                break;
            case 1005:
                EldritchChain.CastEldritchChain(nwnObjectId, targetObject, essence, essenceEffects);
                break;
        }

        if (hasEldritchMastery)
        {
            IntPtr masteryEffect = EffectLinkEffects(EssenceVfx.Mastery(essence, nwnObjectId), EffectAttackIncrease(3));
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, SupernaturalEffect(masteryEffect), nwnObjectId, 3f);
        }
    }
}