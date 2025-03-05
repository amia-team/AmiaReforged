using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

public class LightsCalling
{
    public void CastLightsCalling(uint nwnObjectId)
    {
        if (NwEffects.IsPolymorphed(nwnObjectId))
        {
            SendMessageToPC(nwnObjectId, "You cannot cast while polymorphed.");
            return;
        }

        // Declaring variables for the damage part of the spell
        uint caster = nwnObjectId;
        int warlockLevels = GetLevelByClass(57, caster);
        float effectDuration = warlockLevels < 10 ? RoundsToSeconds(1) : RoundsToSeconds(warlockLevels / 10);
        IntPtr location = GetSpellTargetLocation();
        IntPtr hostileEffects = NwEffects.LinkEffectList(new List <IntPtr>
        {
            EffectBlindness(),
            EffectVisualEffect(VFX_DUR_CESSATE_NEGATIVE)
        });

        // Declaring variables for the summon part of the spell
        float summonDuration = RoundsToSeconds(SummonUtility.PactSummonDuration(caster));
        float summonCooldown = TurnsToSeconds(1);
        IntPtr cooldownEffect = TagEffect(SupernaturalEffect(EffectVisualEffect(VFX_DUR_CESSATE_NEUTRAL)), "wlk_summon_cd");

        //---------------------------
        // * HOSTILE SPELL EFFECT
        //---------------------------

        ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_FNF_SUNBEAM), location);
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, location, FALSE, OBJECT_TYPE_CREATURE);
        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            if (NwEffects.IsValidSpellTarget(currentTarget, 3, caster))
            {
                SignalEvent(currentTarget, EventSpellCastAt(caster, 1009));

                if (GetHasSpellEffect(SPELL_PROTECTION_FROM_GOOD | SPELL_UNHOLY_AURA, currentTarget) == TRUE)
                {
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_GLOBE_USE), currentTarget);
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, location, FALSE, OBJECT_TYPE_CREATURE);
                    continue;
                }
                if (NwEffects.ResistSpell(caster, currentTarget)){
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, location, FALSE, OBJECT_TYPE_CREATURE);
                    continue;
                }
                if (GetRacialType(currentTarget) == RACIAL_TYPE_UNDEAD && NwEffects.IsValidSpellTarget(currentTarget, 2, caster))
                {
                    bool passedWillSave = FortitudeSave(currentTarget, WarlockConstants.CalculateDC(caster), SAVING_THROW_TYPE_GOOD, caster) == TRUE;

                    if (passedWillSave)
                    {
                        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_WILL_SAVING_THROW_USE), currentTarget);
                        currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, location, FALSE, OBJECT_TYPE_CREATURE);
                        continue;
                    }
                    if (!passedWillSave)
                    {
                        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectTurned(), currentTarget, effectDuration);
                        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_PDK_FEAR), currentTarget, effectDuration);
                        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_SUNSTRIKE), currentTarget);
                    }
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, location, FALSE, OBJECT_TYPE_CREATURE);
                    continue;
                }

                bool passedFortSave = FortitudeSave(currentTarget, WarlockConstants.CalculateDC(caster), SAVING_THROW_TYPE_GOOD, caster) == TRUE;

                if (passedFortSave)
                {
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_WILL_SAVING_THROW_USE), currentTarget);
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, location, FALSE, OBJECT_TYPE_CREATURE);
                    continue;
                }
                if (!passedFortSave)
                {
                    ApplyEffectToObject(DURATION_TYPE_TEMPORARY, hostileEffects, currentTarget, effectDuration);
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_BLIND_DEAF_M), currentTarget);
                }
            }
            currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, location, FALSE, OBJECT_TYPE_CREATURE);
        }

        //---------------------------
        // * SUMMONING
        //---------------------------

        // If summonCooldown is off and spell has hit a valid target, summon; else don't summon
        if (NwEffects.GetHasEffectByTag("wlk_summon_cd", caster) == FALSE)
        {
            // Apply cooldown
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, cooldownEffect, caster, summonCooldown);
            DelayCommand(summonCooldown, () => FloatingTextStringOnCreature(WarlockConstants.String("Shattered Guardian can be summoned again."), caster, 0));
            // Summon
            float delay = NwEffects.RandomFloat(1, 2);
            ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, EffectSummonCreature("wlkCelestial", -1, delay, 1), location, summonDuration);
            // Apply effects
            DelayCommand(delay + 2.5f, () => SetPhenoType(19, GetAssociate(ASSOCIATE_TYPE_SUMMONED, caster)));
        }
    }
}