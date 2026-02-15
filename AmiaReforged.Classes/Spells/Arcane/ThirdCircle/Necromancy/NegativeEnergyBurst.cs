using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.ThirdCircle.Necromancy;

/// <summary>
/// Innate level: 3
/// School: necromancy
/// Components: verbal, somatic
/// Range: medium
/// Area of effect: huge (6.67m radius)
/// Duration: instant / permanent
/// Save: Will half (living only)
/// Spell resistance: yes
///
/// The caster releases a burst of negative energy at a specified point, dealing 1d8 + 1 per
/// caster level (max +20) negative energy damage to living creatures. A successful Will save
/// halves the damage. Living creatures also suffer a permanent Strength decrease equal to
/// caster level / 4 (minimum 1). Undead in the area are healed for the same amount and
/// receive a permanent Strength increase instead. Pale Master levels add to caster level
/// and reduce target spell resistance.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class NegativeEnergyBurst : ISpell
{
    private const int PaleMasterClassId = 24;

    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_NegBurst";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        int paleMasterLevel = GetPaleMasterLevel(caster);
        int casterLevel = caster.CasterLevel + paleMasterLevel;
        int additionalDamage = Math.Min(casterLevel, 20);
        int strMod = Math.Max(casterLevel / 4, 1);
        int spellDc = SpellUtils.GetSpellDc(eventData);
        MetaMagic metaMagic = eventData.MetaMagicFeat;

        Location? targetLocation = eventData.TargetLocation;
        if (targetLocation == null) return;

        // Explosion VFX at target location
        Effect explosion = Effect.VisualEffect(VfxType.FnfLosEvil20);
        targetLocation.ApplyEffect(EffectDuration.Instant, explosion);

        // Iterate all creatures in a huge sphere
        foreach (NwCreature target in targetLocation
                     .GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Huge, true))
        {
            bool isUndead = NWScript.GetRacialType(target) == NWScript.RACIAL_TYPE_UNDEAD;

            // Roll damage
            int damage = RollDamage(additionalDamage, metaMagic);

            if (isUndead)
            {
                ApplyUndeadEffects(caster, target, damage, strMod, eventData.Spell);
            }
            else if (SpellUtils.IsValidHostileTarget(target, caster))
            {
                ApplyLivingEffects(caster, target, damage, strMod, spellDc, paleMasterLevel,
                    eventData.Spell);
            }
        }
    }

    private static void ApplyUndeadEffects(NwCreature caster, NwCreature target, int damage, int strMod,
        NwSpell spell)
    {
        // Signal as non-hostile for undead
        CreatureEvents.OnSpellCastAt.Signal(caster, target, spell, harmful: false);

        Effect heal = Effect.Heal(damage);
        Effect healVfx = Effect.VisualEffect(VfxType.ImpHealingM);

        // Permanent Strength buff for undead
        Effect strBuff = Effect.LinkEffects(
            Effect.AbilityIncrease(Ability.Strength, strMod),
            Effect.VisualEffect(VfxType.ImpHeadEvil));

        target.ApplyEffect(EffectDuration.Instant, heal);
        target.ApplyEffect(EffectDuration.Instant, healVfx);
        target.ApplyEffect(EffectDuration.Permanent, strBuff);
    }

    private static void ApplyLivingEffects(NwCreature caster, NwCreature target, int damage, int strMod,
        int spellDc, int paleMasterLevel, NwSpell spell)
    {
        // Signal hostile spell
        CreatureEvents.OnSpellCastAt.Signal(caster, target, spell);

        // Pale Master SR reduction
        if (paleMasterLevel > 0)
        {
            Effect srDecrease = Effect.SpellResistanceDecrease(paleMasterLevel);
            target.ApplyEffect(EffectDuration.Temporary, srDecrease, TimeSpan.FromSeconds(0.5));
        }

        // Check spell resistance
        if (SpellUtils.MyResistSpell(caster, target))
            return;

        // Will save for half damage
        SavingThrowResult saveResult = target.RollSavingThrow(
            SavingThrow.Will, spellDc, SavingThrowType.Negative, caster);

        if (saveResult == SavingThrowResult.Success)
        {
            target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpWillSavingThrowUse));
            damage /= 2;
        }

        // Apply negative energy damage
        Effect damageEffect = Effect.Damage(damage, DamageType.Negative);
        Effect negVfx = Effect.VisualEffect(VfxType.ImpNegativeEnergy);

        target.ApplyEffect(EffectDuration.Instant, damageEffect);
        target.ApplyEffect(EffectDuration.Instant, negVfx);

        // Permanent Strength debuff for living creatures
        Effect strDebuff = Effect.LinkEffects(
            Effect.AbilityDecrease(Ability.Strength, strMod),
            Effect.VisualEffect(VfxType.DurCessateNegative));

        target.ApplyEffect(EffectDuration.Permanent, strDebuff);
    }

    private static int RollDamage(int additionalDamage, MetaMagic metaMagic)
    {
        int damage;

        if (metaMagic == MetaMagic.Maximize)
        {
            damage = 8 + additionalDamage;
        }
        else
        {
            damage = Random.Shared.Roll(8) + additionalDamage;
        }

        if (metaMagic == MetaMagic.Empower)
        {
            damage = damage + damage / 2;
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
