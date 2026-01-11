using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.FifthCircle;

/**
 * Innate level: 5
 * School: necromancy
 * Descriptor: negative
 * Components: verbal, somatic
 * Range: medium (20 meters)
 * Area of effect: large (5 meter radius)
 * Duration: instant
 * Save: fortitude 1/2
 * Spell resistance: yes
 *
 * Description: All enemies within the area of effect are struck with negative energy that causes 5d8 points of
 * negative energy damage, +1 point per caster level. A successful Fortitude save reduces the damage by half.
 * Negative energy spells have a reverse effect on undead, healing them instead of harming them.
 * Each necromancy spell focus adds another 1d8.
 */

[ServiceBinding(typeof(ISpell))]
public class CircleOfDoom(ShifterDcService shifterDcService) : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_CircDoom";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster == null || eventData.TargetLocation == null) return;

        int casterLevel = eventData.Caster.CasterLevel;
        int dc = eventData.SaveDC;
        int dice = 5;

        if (eventData.Caster is NwCreature casterCreature)
        {
            casterLevel = shifterDcService.GetShifterCasterLevel(casterCreature, casterLevel);
            dc = shifterDcService.GetShifterDc(casterCreature, dc);

            if (casterCreature.KnowsFeat(Feat.EpicSpellFocusNecromancy!))
            {
                dice += 3;
            }
            else if (casterCreature.KnowsFeat(Feat.GreaterSpellFocusNecromancy!))
            {
                dice += 2;
            }
            else if (casterCreature.KnowsFeat(Feat.SpellFocusNecromancy!))
            {
                dice += 1;
            }
        }

        Effect healVfx = Effect.VisualEffect(VfxType.ImpHealingM);
        Effect damageVfx = Effect.VisualEffect(VfxType.ImpNegativeEnergy);

        eventData.TargetLocation.ApplyEffect(EffectDuration.Instant,
            Effect.VisualEffect(VfxType.FnfLosEvil10, fScale: RadiusSize.Large / RadiusSize.Medium));

        foreach (NwCreature targetCreature in eventData.TargetLocation.GetObjectsInShapeByType<NwCreature>(Shape.Sphere,
                     RadiusSize.Large, false))
        {
            if (targetCreature.Race.RacialType == RacialType.Undead)
            {
               _ = ApplyHeal(eventData.Caster, targetCreature, healVfx, casterLevel, dice, eventData.MetaMagicFeat);
               continue;
            }

            if (ResistedSpell) continue;

            if (eventData.Caster is NwCreature caster)
            {
                if (!SpellUtils.IsValidHostileTarget(targetCreature, caster))
                    continue;
            }

            _ = ApplyDamage(eventData.Caster, targetCreature, damageVfx, dc, casterLevel, dice, eventData.MetaMagicFeat);
        }
    }

    private static async Task ApplyHeal(NwGameObject caster, NwCreature targetCreature, Effect healVfx,
        int casterLevel, int dice, MetaMagic metaMagic)
    {
        await NwTask.Delay(SpellUtils.GetRandomDelay());

        int healAmount = CalculateDamage(casterLevel, dice, metaMagic);


        await caster.WaitForObjectContext();
        Effect healEffect = Effect.Heal(healAmount);

        targetCreature.ApplyEffect(EffectDuration.Instant, healEffect);
        targetCreature.ApplyEffect(EffectDuration.Instant, healVfx);
    }

    private static async Task ApplyDamage(NwGameObject caster, NwCreature targetCreature, Effect damageVfx,
        int dc, int casterLevel, int dice, MetaMagic metaMagic)
    {
        await NwTask.Delay(SpellUtils.GetRandomDelay());

        SavingThrowResult savingThrowResult = targetCreature.RollSavingThrow(SavingThrow.Fortitude, dc,
            SavingThrowType.Negative, caster);

        int damageAmount = CalculateDamage(casterLevel, dice, metaMagic);

        if (savingThrowResult == SavingThrowResult.Success)
        {
            damageAmount /= 2;
        }

        await caster.WaitForObjectContext();
        Effect damageEffect = Effect.Damage(damageAmount, DamageType.Negative);

        targetCreature.ApplyEffect(EffectDuration.Instant, damageEffect);
        targetCreature.ApplyEffect(EffectDuration.Instant, damageVfx);
    }

    private static int CalculateDamage(int casterLevel, int dice, MetaMagic metaMagic)
    {
        int damageRoll = SpellUtils.MaximizeSpell(metaMagic,8, dice);
        int totalDamage = damageRoll + casterLevel;
        totalDamage = SpellUtils.EmpowerSpell(metaMagic, totalDamage);

        return totalDamage;
    }


    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}
