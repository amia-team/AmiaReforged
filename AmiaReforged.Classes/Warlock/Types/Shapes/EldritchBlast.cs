using AmiaReforged.Classes.Warlock.Types.EssenceEffects;
using Anvil.API;
using NLog;
using NLog.Fluent;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Warlock.Types.Shapes;

public static class EldritchBlast
{
    public static void CastEldritchBlast(uint nwnObjectId, uint targetObject, EssenceType essenceType,
        EssenceEffectApplier effectApplier)
    {
        LogManager.GetCurrentClassLogger().Info("Casting Eldritch Blast.");
        if (SpellFailure(nwnObjectId) == TRUE) return;

        SignalEvent(targetObject, EventSpellCastAt(nwnObjectId, 981));
        LogManager.GetCurrentClassLogger().Info("EventSpellCastAt signal sent.");

        int damage = EldritchDamage.CalculateDamageAmount(nwnObjectId);
        LogManager.GetCurrentClassLogger().Info($"Damage calculated: {damage}.");

        int touchAttackRanged = WarlockConstants.RangedTouch(nwnObjectId, targetObject);

        if (touchAttackRanged == FALSE) return;

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EssenceVfx.Beam(essenceType, nwnObjectId), targetObject, 1.1f);
        effectApplier.ApplyEffects(damage * touchAttackRanged);
    }

    private static int SpellFailure(uint nwnObjectId)
    {
        if (d100() <= GetArcaneSpellFailure(nwnObjectId))
        {
            SendMessageToPC(nwnObjectId, szMessage: "Arcane spell failure!");
            return TRUE;
        }

        return FALSE;
    }
}