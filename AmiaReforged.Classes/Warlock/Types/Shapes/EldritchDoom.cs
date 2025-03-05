using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock.Types.EssenceEffects;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Warlock.Types.Shapes;

public static class EldritchDoom
{
    public static void CastEldritchDoom(uint caster, IntPtr location, EssenceType essence)
    {
        ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EssenceVfx.Doom(essence), location);
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, TRUE);

        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            if (NwEffects.IsValidSpellTarget(currentTarget, 3, caster))
            {
                EssenceEffectApplier effectApplier =
                EssenceEffectFactory.CreateEssenceEffect(essence, currentTarget, caster);

                SignalEvent(currentTarget, EventSpellCastAt(caster, 1003));

                bool hasEvasion = GetHasFeat(FEAT_EVASION, currentTarget) == TRUE;
                bool hasImpEvasion = GetHasFeat(FEAT_IMPROVED_EVASION, currentTarget) == TRUE;
                bool passedSave = ReflexSave(currentTarget, WarlockConstants.CalculateDC(caster), 0, caster) == TRUE;

                if (passedSave)
                {
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_REFLEX_SAVE_THROW_USE), currentTarget);
                    if (hasEvasion || hasImpEvasion)
                    {
                        currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, TRUE);
                        continue;
                    }
                }

                int damage = EldritchDamage.CalculateDamageAmount(caster);
                damage = passedSave || hasImpEvasion ? damage / 2 : damage;
                effectApplier.ApplyEffects(damage);
            }

            currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, TRUE);
        }
    }
}