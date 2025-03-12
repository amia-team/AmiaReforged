using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock.Types.EssenceEffects;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Warlock.Types.Shapes;

public static class EldritchSpear
{
    public static void CastEldritchSpear(uint caster, uint targetObject, EssenceType essence,
        EssenceEffectApplier effectApplier)
    {
        EssenceType essenceType = (EssenceType)GetLocalInt(GetItemPossessedBy(caster, sItemTag: "ds_pckey"),
            sVarName: "warlock_essence");

        int touchAttackRanged = WarlockConstants.RangedTouch(caster, targetObject);
        IntPtr location = GetLocation(targetObject);

        int damage = EldritchDamage.CalculateDamageAmount(caster);

        if (touchAttackRanged == FALSE) return;
        effectApplier.ApplyEffects(damage * touchAttackRanged);

        SignalEvent(targetObject, EventSpellCastAt(caster, 982));

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EssenceVfx.Beam(essence, caster), targetObject, 1.1f);
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_SILENT_LIGHTNING, caster, BODY_NODE_HAND),
            targetObject, 1.1f);

        uint currentTarget = GetFirstObjectInShape(SHAPE_SPELLCYLINDER, 40f, location, TRUE, OBJECT_TYPE_CREATURE,
            GetPosition(caster));

        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            if (NwEffects.IsValidSpellTarget(currentTarget, 3, caster))
            {
                SignalEvent(targetObject, EventSpellCastAt(caster, 982));

                EssenceEffectApplier aoeEffectApplier =
                    EssenceEffectFactory.CreateEssenceEffect(essenceType, currentTarget, caster);

                bool hasEvasion = GetHasFeat(FEAT_EVASION, currentTarget) == TRUE;
                bool hasImpEvasion = GetHasFeat(FEAT_IMPROVED_EVASION, currentTarget) == TRUE;
                bool passedSave = ReflexSave(currentTarget, WarlockConstants.CalculateDc(caster), 0, caster) == TRUE;

                if (passedSave)
                {
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_REFLEX_SAVE_THROW_USE),
                        currentTarget);
                    if (hasEvasion || hasImpEvasion)
                    {
                        currentTarget = GetNextObjectInShape(SHAPE_SPELLCYLINDER, 40f, location, TRUE,
                            OBJECT_TYPE_CREATURE, GetPosition(caster));
                        continue;
                    }
                }

                damage = EldritchDamage.CalculateDamageAmount(caster) / 2;
                damage = passedSave || hasImpEvasion ? damage / 2 : damage;
                aoeEffectApplier.ApplyEffects(damage);
            }

            currentTarget = GetNextObjectInShape(SHAPE_SPELLCYLINDER, 40f, location, TRUE, OBJECT_TYPE_CREATURE,
                GetPosition(caster));
        }
    }
}