using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

public class LoudDecay
{
    public void CastLoudDecay(uint nwnObjectId)
    {
        IntPtr location = GetSpellTargetLocation();
        uint caster = nwnObjectId;

        // Declaring variables for the damage part of the spell
        int warlockLevels = GetLevelByClass(57, nwnObjectId);
        int damage = d6(warlockLevels/2);
        IntPtr loudDamage = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectDamage(damage, DAMAGE_TYPE_SONIC),
            EffectVisualEffect(VFX_IMP_SONIC)
        });
        IntPtr loudVFX = EffectVisualEffect(2124); // VFX_FNF_LOUD_DECAY

        // Declaring variables for the summon part of the spell
        uint summon = GetObjectByTag("wlkAberrant");
        int summonTier = warlockLevels / 5;
        int summonCount = warlockLevels switch
        {
            >= 1 and < 15 => 1,
            >= 15 and < 30 => 2,
            >= 30 => 3,
            _ => 0
        };
        float summonDuration = RoundsToSeconds(5 + warlockLevels / 2);
        float summonCooldown = TurnsToSeconds(1);
        IntPtr cooldownEffect = TagEffect(SupernaturalEffect(EffectVisualEffect(VFX_DUR_CESSATE_NEUTRAL)), "wlk_summon_cd");

        if (NwEffects.IsPolymorphed(nwnObjectId))
        {
            SendMessageToPC(nwnObjectId, "You cannot cast while polymorphed.");
            return;
        }

        //---------------------------
        // * HOSTILE SPELL EFFECT
        //---------------------------

        ApplyEffectAtLocation(DURATION_TYPE_INSTANT, loudVFX, location);
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, location, FALSE, OBJECT_TYPE_CREATURE);

        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            if (GetResRef(currentTarget) == "wlkaberrant")
            {
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectHeal(damage/2), currentTarget);
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_HEAD_MIND), currentTarget);
                currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, location, FALSE, OBJECT_TYPE_CREATURE);
                continue;
            }
            if (NwEffects.IsValidSpellTarget(currentTarget, 2, caster))
            {
                SignalEvent(currentTarget, EventSpellCastAt(caster, 1008));

                if (NwEffects.ResistSpell(caster, currentTarget))
                {
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, location, FALSE, OBJECT_TYPE_CREATURE);
                    continue;
                }

                bool passedFortSave = FortitudeSave(currentTarget, NwEffects.CalculateDC(caster), SAVING_THROW_TYPE_SONIC, caster) == TRUE;
                if (passedFortSave) ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FORTITUDE_SAVING_THROW_USE), currentTarget);
                damage = d6(warlockLevels/2);
                damage = passedFortSave ? damage / 2 : damage;
                ApplyDelayedDamage(3f, loudDamage, currentTarget);
                ApplyDelayedVFX(3f, currentTarget);
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
            DelayCommand(summonCooldown, () => FloatingTextStringOnCreature(NwEffects.WarlockString("Violet Fungi can be summoned again."), caster, 0));
            // Summon new
            SummonEffects.SummonMany(caster, summonDuration, summonCount, "wlkaberrant", location, 1f, 9f, 3f, 4f, VFX_FNF_GAS_EXPLOSION_NATURE, 5.5f);
            DelayCommand(4.1f, () => SummonEffects.SetSummonsFacing(summonCount, location));
        }
    }
    private void ApplyDelayedDamage(float delay, IntPtr loudDamage, uint currentTarget)
    {
        DelayCommand(delay, () => ApplyEffectToObject(DURATION_TYPE_INSTANT, loudDamage, currentTarget));
    }
    private void ApplyDelayedVFX(float delay, uint currentTarget)
    {
        DelayCommand(delay, () => ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FORTITUDE_SAVING_THROW_USE), currentTarget));
    }
}