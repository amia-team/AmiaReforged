using AmiaReforged.Classes.Warlock.Types.EssenceEffects;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Warlock.Types.Shapes;

public static class EldritchBlast
{
    public static void CastEldritchBlast(uint nwnObjectId, uint targetObject, EssenceType essenceType, EssenceEffectApplier effectApplier)
    {
        if (SpellFailure(nwnObjectId) == TRUE) return;

        SignalEvent(targetObject, EventSpellCastAt(nwnObjectId, 981));
        int damage = EldritchDamage.CalculateDamageAmount(nwnObjectId);
        int touchAttackRanged = WarlockConstants.RangedTouch(targetObject);

        if (touchAttackRanged == FALSE) return;

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EssenceVfx.Beam(essenceType, nwnObjectId), targetObject, 1.1f);
        effectApplier.ApplyEffects(damage * touchAttackRanged);
    }
    private static int SpellFailure(uint nwnObjectId)
    {
        if (d100() <= GetArcaneSpellFailure(nwnObjectId))
        {
            SendMessageToPC(nwnObjectId, "Arcane spell failure!");
            return TRUE;
        }
        return FALSE;
    }
}