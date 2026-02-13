using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.FifthCircle;

/**
 * Innate level: 5
 * School: conjuration
 * Descriptor: positive
 * Components: verbal, somatic
 * Range: medium (20 meters)
 * Area of effect: large (5 meter radius)
 * Duration: instant
 * Save: fortitude 1/2 (undead only)
 * Spell resistance: yes (undead only)
 *
 * Description: All allies within the area of effect are healed for 5d8 +1 per caster level hit points.
 * Healing spells have a reverse effect on undead, harming them instead of healing them.
 * Undead may make a fortitude save for half damage. Each conjuration spell focus adds another 1d8.
 */

[ServiceBinding(typeof(ISpell))]
public class HealingCircle(ShifterDcService shifterDcService) : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_HealCirc";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature casterCreature || eventData.TargetLocation == null) return;

        int casterLevel = eventData.Caster.CasterLevel;
        int dc = eventData.SaveDC;
        int dice = 5;

        casterLevel = shifterDcService.GetShifterCasterLevel(casterCreature, casterLevel);
        dc = shifterDcService.GetShifterDc(casterCreature, dc);

        if (casterCreature.KnowsFeat(Feat.EpicSpellFocusConjuration!))
        {
            dice += 3;
        }
        else if (casterCreature.KnowsFeat(Feat.GreaterSpellFocusConjuration!))
        {
            dice += 2;
        }
        else if (casterCreature.KnowsFeat(Feat.SpellFocusConjuration!))
        {
            dice += 1;
        }

        Effect healVfx = Effect.VisualEffect(VfxType.ImpHealingM);
        Effect damageVfx = Effect.VisualEffect(VfxType.ImpSunstrike);

        eventData.TargetLocation.ApplyEffect(EffectDuration.Instant,
            Effect.VisualEffect(VfxType.FnfLosHoly10, fScale: RadiusSize.Large / RadiusSize.Medium));

        foreach (NwCreature targetCreature in eventData.TargetLocation.GetObjectsInShapeByType<NwCreature>(Shape.Sphere,
                     RadiusSize.Large, false))
        {
            if (targetCreature.Race.RacialType == RacialType.Undead)
            {
                if (casterCreature.IsReactionTypeFriendly(targetCreature)) continue;

                CreatureEvents.OnSpellCastAt.Signal(eventData.Caster, targetCreature, eventData.Spell);

                if (ResistedSpell)
                    continue;

                _ = ApplyDamage(eventData.Caster, targetCreature, damageVfx, dc, casterLevel, dice, eventData.MetaMagicFeat);
                continue;
            }
            if (!casterCreature.IsReactionTypeHostile(targetCreature))
                _ = ApplyHeal(eventData.Caster, targetCreature, healVfx, casterLevel, dice, eventData.MetaMagicFeat);
        }
    }

    private static async Task ApplyHeal(NwGameObject caster, NwCreature targetCreature, Effect healVfx,
        int casterLevel, int dice, MetaMagic metaMagic)
    {
        await NwTask.Delay(SpellUtils.GetRandomDelay());

        int healAmount = CalculateHeal(casterLevel, dice, metaMagic);

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
            SavingThrowType.Positive, caster);

        int damageAmount = CalculateHeal(casterLevel, dice, metaMagic);

        if (savingThrowResult == SavingThrowResult.Success)
        {
            damageAmount /= 2;
        }

        await caster.WaitForObjectContext();
        Effect damageEffect = Effect.Damage(damageAmount, DamageType.Positive);

        targetCreature.ApplyEffect(EffectDuration.Instant, damageEffect);
        targetCreature.ApplyEffect(EffectDuration.Instant, damageVfx);
    }

    private static int CalculateHeal(int casterLevel, int dice, MetaMagic metaMagic)
    {
        int healRoll = SpellUtils.MaximizeSpell(metaMagic,8, dice);
        int totalDamage = healRoll + casterLevel;
        totalDamage = SpellUtils.EmpowerSpell(metaMagic, totalDamage);

        return totalDamage;
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}
