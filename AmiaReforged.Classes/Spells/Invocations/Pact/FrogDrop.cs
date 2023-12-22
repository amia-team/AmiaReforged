using AmiaReforged.Classes.EffectUtils;
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
        uint target = GetSpellTargetObject();
        int warlockLevels = GetLevelByClass(57, caster);
        float effectDuration = warlockLevels < 10 ? RoundsToSeconds(1) : RoundsToSeconds(warlockLevels / 10);
        IntPtr location = GetSpellTargetLocation();
        bool passedReflexSave = ReflexSave(target, NwEffects.CalculateDC(caster), SAVING_THROW_TYPE_CHAOS, caster) == TRUE;

        // Impact VFX onhit
        IntPtr frogDrop = NwEffects.LinkEffectList(new List<IntPtr>
        {
                 EffectVisualEffect(VFX_IMP_DAZED_S),
                 EffectKnockdown()
        });

        // Declaring variables for summon effects
        uint summon = GetAssociate(ASSOCIATE_TYPE_SUMMONED, caster);
        float summonDuration = RoundsToSeconds(5 + warlockLevels / 2);
        float summonCooldown = TurnsToSeconds(1);
        IntPtr cooldownEffect = TagEffect(SupernaturalEffect(EffectVisualEffect(VFX_DUR_CESSATE_NEUTRAL)), "wlk_summon_cd");
        IntPtr slaadSwarm = EffectSwarm(FALSE, "wlkSlaadRed");
        if (warlockLevels > 10 && warlockLevels < 20) slaadSwarm = EffectSwarm(FALSE, "wlkSlaadBlue", "wlkSlaadRed");
        if (warlockLevels > 20 && warlockLevels < 30) slaadSwarm = EffectSwarm(FALSE, "wlkSlaadGreen", "wlkSlaadBlue", "wlkSlaadRed");
        if (warlockLevels == 30) slaadSwarm = EffectSwarm(FALSE, "wlkSlaadGray", "wlkSlaadGreen", "wlkSlaadBlue", "wlkSlaadRed");

        SignalEvent(target, EventSpellCastAt(caster, 1010));

        if (!NwEffects.IsValidSpellTarget(target, 2, caster)) return;

        //---------------------------
        // * SUMMONING
        //---------------------------

        // If summonCooldown is active, don't summon; else summon and set summonCooldown
        if (NwEffects.GetHasEffectByTag("wlk_summon_cd", caster) == FALSE)
        {
            // Apply cooldown
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, cooldownEffect, caster, summonCooldown);
            DelayCommand(summonCooldown, () => FloatingTextStringOnCreature(NwEffects.WarlockString("Hatched Slaad can be summoned again."), caster, 0));

            // Summon new
            DelayCommand(3f, () => ApplyEffectToObject(DURATION_TYPE_TEMPORARY, slaadSwarm, target, summonDuration));
        }

        //---------------------------
        // * HOSTILE SPELL EFFECT
        //---------------------------

        if (GetHasSpellEffect(SPELL_PROTECTION__FROM_CHAOS, target) == TRUE)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_GLOBE_USE), target);
            return;
        }
        if (passedReflexSave)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_WILL_SAVING_THROW_USE), target);
            return;
        }
        if (!passedReflexSave) ApplyEffectToObject(DURATION_TYPE_TEMPORARY, frogDrop, target, effectDuration);
    }
}
