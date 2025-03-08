using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
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
        IntPtr loudVfx = EffectVisualEffect(2133); // VFX_FNF_LOUD_DECAY

        // Declaring variables for the summon part of the spell
        uint summon = GetObjectByTag(sTag: "wlkAberrant");
        int summonTier = warlockLevels / 5;
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

        ApplyEffectAtLocation(DURATION_TYPE_INSTANT, loudVfx, location);
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, location);

        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            if (NwEffects.IsValidSpellTarget(currentTarget, 2, caster))
            {
                int damage = d6(warlockLevels / 2);

                if (GetResRef(currentTarget) == "wlkaberrant")
                {
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectHeal(damage / 2), currentTarget);
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_HEAD_MIND), currentTarget);
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, location);
                    continue;
                }

                SignalEvent(currentTarget, EventSpellCastAt(caster, 1008));

                if (NwEffects.ResistSpell(caster, currentTarget))
                {
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, location);
                    continue;
                }

                bool passedFortSave = FortitudeSave(currentTarget, WarlockConstants.CalculateDc(caster),
                    SAVING_THROW_TYPE_SONIC, caster) == TRUE;

                if (passedFortSave) ApplyDelayedVfx(3f, currentTarget);

                damage = passedFortSave ? damage / 2 : damage;
                IntPtr loudDamage = NwEffects.LinkEffectList(new List<IntPtr>
                {
                    EffectDamage(damage, DAMAGE_TYPE_SONIC),
                    EffectVisualEffect(VFX_IMP_SONIC)
                });

                ApplyDelayedDamage(3f, loudDamage, currentTarget);
            }

            currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_COLOSSAL, location);
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
                () => FloatingTextStringOnCreature(
                    WarlockConstants.String(message: "Violet Fungi can be summoned again."), caster, 0));
            // Summon new
            SummonUtility.SummonMany(caster, summonDuration, summonCount, summonResRef: "wlkaberrant", location, 1f, 9f,
                3f, 4f);
            DelayCommand(4.1f, () => SummonUtility.SetSummonsFacing(summonCount, location));
        }
    }

    private void ApplyDelayedDamage(float delay, IntPtr loudDamage, uint currentTarget)
    {
        DelayCommand(delay, () => ApplyEffectToObject(DURATION_TYPE_INSTANT, loudDamage, currentTarget));
    }

    private void ApplyDelayedVfx(float delay, uint currentTarget)
    {
        DelayCommand(delay,
            () => ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FORTITUDE_SAVING_THROW_USE),
                currentTarget));
    }
}