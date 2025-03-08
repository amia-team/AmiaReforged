using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

public class PrimordialGust
{
    public void CastPrimordialGust(uint nwnObjectId)
    {
        // Declaring variables for the damage part of the spell
        uint caster = nwnObjectId;
        IntPtr location = GetSpellTargetLocation();
        int warlockLevels = GetLevelByClass(57, caster);

        // Impact VFX onhit
        IntPtr primordialVfx = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectVisualEffect(VFX_COM_HIT_FROST),
            EffectVisualEffect(VFX_COM_HIT_ELECTRICAL),
            EffectVisualEffect(VFX_COM_HIT_FIRE)
        });

        // Declaring variables for the summon part of the spell
        int summonCount = warlockLevels switch
        {
            >= 1 and < 15 => 1,
            >= 15 and < 30 => 2,
            >= 30 => 3,
            _ => 0
        };
        float summonDuration = RoundsToSeconds(SummonUtility.PactSummonDuration(caster));
        float summonCooldown = TurnsToSeconds(1);
        IntPtr cooldownEffect = TagEffect(SupernaturalEffect(EffectVisualEffect(VFX_DUR_CESSATE_NEUTRAL)),
            sNewTag: "wlk_summon_cd");

        if (NwEffects.IsPolymorphed(nwnObjectId))
        {
            SendMessageToPC(nwnObjectId, szMessage: "You cannot cast while polymorphed.");
            return;
        }

        //---------------------------
        // * HOSTILE SPELL EFFECT
        //---------------------------
        const int validObjectTypes = OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR | OBJECT_TYPE_PLACEABLE;
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPELLCONE, 11f, location, TRUE, validObjectTypes);

        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            // Damage variable
            int damage = d4(warlockLevels / 3);
            IntPtr primordialDamage = NwEffects.LinkEffectList(new List<IntPtr>
            {
                EffectDamage(damage, DAMAGE_TYPE_COLD),
                EffectDamage(damage, DAMAGE_TYPE_ELECTRICAL),
                EffectDamage(damage, DAMAGE_TYPE_FIRE)
            });

            if (GetObjectType(currentTarget) == OBJECT_TYPE_DOOR ||
                GetObjectType(currentTarget) == OBJECT_TYPE_PLACEABLE)
            {
                ApplyEffectToObject(DURATION_TYPE_INSTANT, primordialDamage, currentTarget);
                ApplyEffectToObject(DURATION_TYPE_INSTANT, primordialVfx, currentTarget);
                currentTarget = GetNextObjectInShape(SHAPE_SPELLCONE, 11f, location, TRUE);
                continue;
            }

            if (GetResRef(currentTarget) == "wlkelemental")
            {
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_HEAD_FIRE), currentTarget);
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectHeal(damage), currentTarget);
                currentTarget = GetNextObjectInShape(SHAPE_SPELLCONE, 11f, location, TRUE);
                continue;
            }

            if (NwEffects.IsValidSpellTarget(currentTarget, 2, caster))
            {
                bool passedReflexSave = ReflexSave(currentTarget, WarlockConstants.CalculateDc(caster),
                    SAVING_THROW_TYPE_FIRE | SAVING_THROW_TYPE_COLD | SAVING_THROW_TYPE_ELECTRICITY, caster) == TRUE;
                bool hasEvasion = GetHasFeat(FEAT_EVASION, currentTarget) == TRUE;
                bool hasImpEvasion = GetHasFeat(FEAT_IMPROVED_EVASION, currentTarget) == TRUE;

                SignalEvent(currentTarget, EventSpellCastAt(caster, 1012));

                if (passedReflexSave)
                {
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_REFLEX_SAVE_THROW_USE),
                        currentTarget);

                    if (hasEvasion || hasImpEvasion)
                    {
                        currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, location, TRUE);
                        continue;
                    }
                }

                damage = d4(warlockLevels / 3);
                damage = passedReflexSave || hasImpEvasion ? damage / 2 : damage;
                primordialDamage = NwEffects.LinkEffectList(new List<IntPtr>
                {
                    EffectDamage(damage, DAMAGE_TYPE_COLD),
                    EffectDamage(damage, DAMAGE_TYPE_ELECTRICAL),
                    EffectDamage(damage, DAMAGE_TYPE_FIRE)
                });
                ApplyEffectToObject(DURATION_TYPE_INSTANT, primordialDamage, currentTarget);
                ApplyEffectToObject(DURATION_TYPE_INSTANT, primordialVfx, currentTarget);
            }

            currentTarget = GetNextObjectInShape(SHAPE_SPELLCONE, 11f, location, TRUE, validObjectTypes);
        }

        //---------------------------
        // * SUMMONING
        //---------------------------

        // If summonCooldown is off and spell has hit a valid target, summon; else don't summon
        if (NwEffects.GetHasEffectByTag(effectTag: "wlk_summon_cd", caster) == FALSE)
        {
            // Apply cooldown
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, cooldownEffect, caster, summonCooldown);
            DelayCommand(summonCooldown,
                () => FloatingTextStringOnCreature(WarlockConstants.String(message: "Mephits can be summoned again."),
                    caster, 0));
            SummonUtility.SummonMany(caster, summonDuration, summonCount, summonResRef: "wlkelemental", location, 0.5f,
                2f, 0.5f, 1.5f);
            DelayCommand(1.6f, () => SummonUtility.SetSummonsFacing(summonCount, location));
        }
    }
}