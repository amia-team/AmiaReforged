using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.FourthCircle.Necromancy;

/// <summary>
/// Level: Cleric 4
/// Area of effect: Single
/// Duration: 1 Turn/Level
/// Valid Metamagic: Still, Silent, Empower, Maximize, Extend
/// Save: Fortitude 1/2
/// Spell Resistance: Yes
///  The caster painfully stiffens the joints and attempts to forcibly onset rigor mortis on their target.
///  The victim suffers 1d6 bludgeoning damage per caster level, to a maximum of 15d6. In addition,
///  the victim suffers 4 points of dexterity damage and 10% movement speed decrease for turns per caster level.
///  A fortitude saving throw halves the damage and negates the effect.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class RigorMortis : ISpell
{
    private const VfxType DurImmobilize = (VfxType)2526;
    public string ImpactScript => "rigor_mortis";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.TargetObject is not NwCreature creature
            || eventData.Caster is not NwCreature caster
            || caster.IsReactionTypeFriendly(creature))
            return;

        CreatureEvents.OnSpellCastAt.Signal(caster, creature, eventData.Spell);
        if (caster.SpellResistanceCheck(creature, eventData.Spell, creature.CasterLevel)) return;

        MetaMagic metaMagic = eventData.MetaMagicFeat;
        int damageDice = caster.CasterLevel / 2;
        int damageAmount = SpellUtils.MaximizeSpell(metaMagic, 6, damageDice);
        damageAmount = SpellUtils.EmpowerSpell(metaMagic, damageAmount);

        if (caster.RollSavingThrow(SavingThrow.Fortitude, eventData.SaveDC, SavingThrowType.Spell, creature)
            == SavingThrowResult.Success)
        {
            damageAmount /= 2;
            creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
            ApplyDamageEffect(creature, damageAmount);
            return;
        }

        TimeSpan duration = NwTimeSpan.FromRounds(caster.CasterLevel);
        if (metaMagic == MetaMagic.Extend) duration *= 2;

        Effect rigorEffect = Effect.LinkEffects
        (
            Effect.VisualEffect(DurImmobilize),
            Effect.AbilityDecrease(Ability.Dexterity, 4),
            Effect.MovementSpeedDecrease(10)
        );
        rigorEffect.SubType = EffectSubType.Magical;

        ApplyDamageEffect(creature, damageAmount);
        creature.ApplyEffect(EffectDuration.Temporary, rigorEffect, duration);
    }

    private static void ApplyDamageEffect(NwCreature creature, int damageAmount)
    {
        creature.ApplyEffect(EffectDuration.Instant, Effect.Damage(damageAmount, DamageType.Bludgeoning));
        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfDemonHand));
        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDivineStrikeHoly, fScale: 0.1f));
    }

    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
}
