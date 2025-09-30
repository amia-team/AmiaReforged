using AmiaReforged.Classes.Warlock.Types.EssenceEffects;
using NLog;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Warlock.Types.Shapes;

public static class EldritchBlast
{
    public static void CastEldritchBlast(uint nwnObjectId, uint targetObject, EssenceType essenceType,
        EssenceEffectApplier effectApplier)
    {
        LogManager.GetCurrentClassLogger().Info("Casting Eldritch Blast.");

        SignalEvent(targetObject, EventSpellCastAt(nwnObjectId, 981));
        LogManager.GetCurrentClassLogger().Info("EventSpellCastAt signal sent.");

        int damage = EldritchDamage.CalculateDamageAmount(nwnObjectId);
        LogManager.GetCurrentClassLogger().Info($"Damage calculated: {damage}.");

        int touchAttackRanged = WarlockUtils.RangedTouch(nwnObjectId, targetObject);

        if (touchAttackRanged == FALSE) return;

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EssenceVfx.Beam(essenceType, nwnObjectId), targetObject, 1.1f);
        effectApplier.ApplyEffects(damage * touchAttackRanged);
    }
}
