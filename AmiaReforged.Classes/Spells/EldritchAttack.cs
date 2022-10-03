using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Types;
using AmiaReforged.Classes.Types.EssenceEffects;
using AmiaReforged.Classes.Types.Shapes;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells;

public class EldritchAttack
{
    public void Run(uint nwnObjectId)
    {
        int spellId = GetSpellId();
        EssenceType essenceType =
            (EssenceType)GetLocalInt(GetItemPossessedBy(nwnObjectId, "ds_pckey"), "warlock_essence");
        uint targetObject = GetSpellTargetObject();

        if (NwEffects.IsPolymorphed(nwnObjectId))
        {
            SendMessageToPC(nwnObjectId, "You cannot invoke while polymorphed.");
            return;
        }

        EssenceVisuals essenceVisuals = EssenceVfxFactory.CreateEssence(essenceType, nwnObjectId);
        EssenceEffectApplier essenceEffects =
            EssenceEffectFactory.CreateEssenceEffect(essenceType, targetObject, nwnObjectId);

        switch (spellId)
        {
            case 1003:
                EldritchDoom.CastEldritchDoom(nwnObjectId, GetSpellTargetLocation(), essenceVisuals);
                break;
            case 1004:
                EldritchSphere.CastEldritchSphere(nwnObjectId, essenceVisuals);
                break;
            case 1005:
                EldritchChain.CastEldritchChain(nwnObjectId, targetObject, essenceVisuals, essenceEffects);
                break;
            case 982:
                SignalEvent(targetObject, EventSpellCastAt(nwnObjectId, 981));

                EldritchBlast.CastEldritchBlast(targetObject, essenceVisuals, essenceEffects,
                    EldritchDamage.CalculateDamageAmount(nwnObjectId));
                break;
            case 981:
                if (NoSpellFailure(nwnObjectId))
                {
                    SignalEvent(targetObject, EventSpellCastAt(nwnObjectId, 981));
                    EldritchBlast.CastEldritchBlast(targetObject, essenceVisuals, essenceEffects,
                        EldritchDamage.CalculateDamageAmount(nwnObjectId));
                }

                break;
        }
    }

    private static bool NoSpellFailure(uint nwnObjectId)
    {
        if (d100() > GetArcaneSpellFailure(nwnObjectId)) return true;
        SendMessageToPC(nwnObjectId, "Arcane spell failure!");
        return false;
    }
}