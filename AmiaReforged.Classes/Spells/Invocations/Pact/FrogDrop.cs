using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Types;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

public class FrogDrop
{
    public void CastFrogDrop(uint nwnObjectId)
    {
        if (NwEffects.IsPolymorphed(nwnObjectId)){
            SendMessageToPC(nwnObjectId, "You cannot cast while polymorphed.");
            return;
        }

        // Declaring variables for the damage part of the spell
        uint caster = nwnObjectId;
        int warlockLevels = GetLevelByClass(57, caster);
        float effectDuration = warlockLevels < 10 ? RoundsToSeconds(1) : RoundsToSeconds(warlockLevels / 10);
        IntPtr location = GetSpellTargetLocation();

        // Impact VFX onhit
        IntPtr frogDrop = NwEffects.LinkEffectList(new List<IntPtr>
        {
                 EffectVisualEffect(VFX_IMP_DAZED_S),
                 EffectKnockdown()
        });

        // Declaring variables for summon effects
        uint summon = GetAssociate(ASSOCIATE_TYPE_SUMMONED, caster);
        int summonTier = SummonUtility.GetSummonTier(caster);
        string slaadTier = summonTier switch
        {
            1 or 2 => "wlkslaadred",
            3 or 4 => "wlkslaadblue",
            5 or 6 => "wlkslaadgreen",
            7 => "wlkslaadgray",
            _ => "wlkslaadred"
        };
        float summonDuration = RoundsToSeconds(SummonUtility.PactSummonDuration(caster));
        float summonCooldown = TurnsToSeconds(1);
        IntPtr cooldownEffect = TagEffect(SupernaturalEffect(EffectVisualEffect(VFX_DUR_CESSATE_NEUTRAL)), "wlk_summon_cd");
        IntPtr slaadSummon = EffectSummonCreature(slaadTier, VFX_IMP_POLYMORPH);
        
        //---------------------------
        // * HOSTILE SPELL EFFECT
        //---------------------------
        ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_FNF_GAS_EXPLOSION_NATURE), location);
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_SMALL, location, FALSE, OBJECT_TYPE_CREATURE);
        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            if (NwEffects.IsValidSpellTarget(currentTarget, 3, caster))
            {
                SignalEvent(currentTarget, EventSpellCastAt(caster, 1010));

                if (GetHasSpellEffect(SPELL_PROTECTION__FROM_CHAOS, currentTarget) == TRUE)
                {
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_GLOBE_USE), currentTarget);
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_SMALL, location, FALSE, OBJECT_TYPE_CREATURE);
                    continue;
                }

                if (NwEffects.ResistSpell(caster, currentTarget))
                {
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_SMALL, location, FALSE, OBJECT_TYPE_CREATURE);
                    continue;
                }

                bool passedReflexSave = ReflexSave(currentTarget, Warlock.CalculateDC(caster), SAVING_THROW_TYPE_CHAOS, caster) == TRUE;

                if (passedReflexSave)
                {
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_WILL_SAVING_THROW_USE), currentTarget);
                    currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_SMALL, location, FALSE, OBJECT_TYPE_CREATURE);
                    continue;
                }

                if (!passedReflexSave) ApplyEffectToObject(DURATION_TYPE_TEMPORARY, frogDrop, currentTarget, effectDuration);
            }
            currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_SMALL, location, FALSE, OBJECT_TYPE_CREATURE);
        }

        //---------------------------
        // * SUMMONING
        //---------------------------

        // If summonCooldown is active, don't summon; else summon and set summonCooldown
        if (NwEffects.GetHasEffectByTag("wlk_summon_cd", caster) == FALSE)
        {
            // Apply cooldown
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, cooldownEffect, caster, summonCooldown);
            DelayCommand(summonCooldown, () => FloatingTextStringOnCreature(Warlock.String("Slaad can be summoned again."), caster, 0));

            // Summon new
            DelayCommand(2.5f, () => ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, slaadSummon, location, summonDuration));
        }
    }
}
