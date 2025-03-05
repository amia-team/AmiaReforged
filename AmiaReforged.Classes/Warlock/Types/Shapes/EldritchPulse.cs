using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock.Types.EssenceEffects;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Warlock.Types.Shapes;

public static class EldritchPulse
{
    public static void CastEldritchPulse(uint caster, uint targetObject, EssenceType essence, EssenceEffectApplier effectApplier)
    {
        int damage = EldritchDamage.CalculateDamageAmount(caster);

        if (NwEffects.IsValidSpellTarget(targetObject, 3, caster))
        {
            // Single target effect
            int touchAttackRanged = WarlockConstants.RangedTouch(targetObject);
            if (touchAttackRanged == FALSE) return;
            effectApplier.ApplyEffects(damage * touchAttackRanged);
            SignalEvent(targetObject, EventSpellCastAt(caster, 1004));
        }

        DelayCommand(3.0f, () => Pulse(caster, targetObject, essence));
    }
    private static void Pulse(uint caster, uint targetObject, EssenceType essence)
    {
        IntPtr location = GetLocation(targetObject);
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_MEDIUM, location, TRUE);
        ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EssenceVfx.Pulse(essence), location);

        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            if (currentTarget == targetObject || currentTarget == caster)
            {
                currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_MEDIUM, location, TRUE);
                continue;
            }
            if (NwEffects.IsValidSpellTarget(currentTarget, 3, caster))
            {
                SignalEvent(currentTarget, EventSpellCastAt(caster, 1004));

                EssenceEffectApplier aoeApplier = EssenceEffectFactory.CreateEssenceEffect(essence, currentTarget, caster);

                bool passedFortSave = FortitudeSave(currentTarget, WarlockConstants.CalculateDc(caster), 0, caster) == TRUE;
                if (passedFortSave) ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FORTITUDE_SAVING_THROW_USE), currentTarget);

                int aoeDamage = EldritchDamage.CalculateDamageAmount(caster);
                aoeDamage = passedFortSave ? aoeDamage / 2 : aoeDamage;
                aoeApplier.ApplyEffects(aoeDamage);
            }

            currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_MEDIUM, location, TRUE);
        }
    }
}