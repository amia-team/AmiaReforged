using AmiaReforged.Classes.Types.EssenceEffects;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types.Shapes;

public static class EldritchBlast
{
    public static void CastEldritchBlast(uint nwnObjectId, uint targetObject, EssenceType essenceType, EssenceEffectApplier effectApplier)
    {
        if (SpellFailure(nwnObjectId) == TRUE) return;

        SignalEvent(targetObject, EventSpellCastAt(nwnObjectId, 981));
        int damage = EldritchDamage.CalculateDamageAmount(nwnObjectId);
        int touchAttackRanged = TouchAttackRanged(targetObject);

        if (touchAttackRanged == 0) return;

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EssenceVfX.Beam(essenceType, nwnObjectId), targetObject, 1.1f);
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