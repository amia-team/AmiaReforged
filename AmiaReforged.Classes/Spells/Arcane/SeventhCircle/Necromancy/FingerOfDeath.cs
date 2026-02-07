using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.SeventhCircle.Necromancy;

/**
 * Innate level: 7
 * School: necromancy
 * Descriptor: death
 * Components: verbal, somatic
 * Range: short
 * Target: single
 * Duration: instant
 * Save: fortitude partial
 * Spell resistance: yes
 *
 * Description: You can slay any one living creature within range. The victim is entitled to a
 * Fortitude saving throw to survive the attack. If they succeed, they instead sustain 3d6 points
 * of negative energy damage +1 point per caster level. Pale Master levels add to caster level
 * and reduce target spell resistance.
 *
 * Amia: Targets take 8% of their max health as negative energy damage per necromancy spell focus tier,
 * even if they pass their save. Undead are healed instead. Death immunity negates the effect.
 */
[ServiceBinding(typeof(ISpell))]
public class FingerOfDeath(DeathSpellService deathSpellService) : ISpell
{
    private const int PaleMasterClassId = 24;

    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_FingDeath";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;
        if (eventData.TargetObject is not NwCreature target) return;

        // Only affects hostile targets
        if (!SpellUtils.IsValidHostileTarget(target, caster))
            return;

        int paleMasterLevel = GetPaleMasterLevel(caster);
        int casterLevel = caster.CasterLevel + paleMasterLevel;
        int dc = eventData.SaveDC;

        Effect deathVfx = Effect.VisualEffect(VfxType.ImpDeathL);
        Effect negativeVfx = Effect.VisualEffect(VfxType.ImpNegativeEnergy);

        // Signal spell cast at event
        SpellUtils.SignalSpell(caster, target, eventData.Spell);

        // Apply temporary spell resistance decrease based on Pale Master level
        if (paleMasterLevel > 0)
        {
            Effect srDecrease = Effect.SpellResistanceDecrease(paleMasterLevel);
            target.ApplyEffect(EffectDuration.Temporary, srDecrease, TimeSpan.FromSeconds(0.5));
        }

        // Check spell resistance
        if (SpellUtils.MyResistSpell(caster, target))
            return;

        // Make fortitude save
        SavingThrowResult saveResult = target.RollSavingThrow(
            SavingThrow.Fortitude,
            dc,
            SavingThrowType.Death,
            caster);

        if (saveResult != SavingThrowResult.Success)
        {
            // Failed save - apply death effect
            target.ApplyEffect(EffectDuration.Instant, Effect.Death());
            target.ApplyEffect(EffectDuration.Instant, deathVfx);
        }
        else
        {
            // Passed save - apply damage if not death immune
            if (!deathSpellService.IsDeathImmune(target))
            {
                int damage = CalculateDamage(casterLevel, eventData.MetaMagicFeat);
                Effect damageEffect = Effect.Damage(damage, DamageType.Negative);
                target.ApplyEffect(EffectDuration.Instant, damageEffect);
                target.ApplyEffect(EffectDuration.Instant, negativeVfx);
            }
        }

        // Apply percentage-based death spell damage (regardless of save result, skip VFX since we already applied one)
        deathSpellService.ApplyDeathSpellDamage(caster, target, applyVfx: false,
            percentPerFocus: DeathSpellService.FingerOfDeathPercentPerFocus);
    }

    private static int CalculateDamage(int casterLevel, MetaMagic metaMagic)
    {
        int damage;

        if (metaMagic == MetaMagic.Maximize)
        {
            damage = 18 + casterLevel; // 3d6 maximized = 18
        }
        else
        {
            damage = Random.Shared.Roll(6, 3) + casterLevel;
        }

        if (metaMagic == MetaMagic.Empower)
        {
            damage = damage + (damage / 2);
        }

        return damage;
    }

    private static int GetPaleMasterLevel(NwCreature creature)
    {
        foreach (CreatureClassInfo classInfo in creature.Classes)
        {
            if (classInfo.Class.Id == PaleMasterClassId)
                return classInfo.Level;
        }
        return 0;
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}
