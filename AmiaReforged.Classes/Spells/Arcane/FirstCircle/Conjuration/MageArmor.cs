using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.FirstCircle.Conjuration;

/// <summary>
/// Mage Armor gives the target +1 AC Bonus to Deflection, Armor Enchantment, Natural Armor and Dodge.
/// Shadow Conjuration Mage Armor instead applies 5/- cold and negative energy resistance
/// (Epic Spell Focus Illusion adds 5/- to each resistance).
/// Duration: 1 hour per caster level.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class MageArmor : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }

    public string ImpactScript => "NW_S0_MageArm";

    private const string MageArmorTag = "MageArmor";
    private const string ShadowMageArmorTag = "ShadowMageArmor";

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        NwGameObject? targetObject = eventData.TargetObject;
        if (targetObject is not NwCreature target) return;

        SpellUtils.SignalSpell(caster, target, eventData.Spell);

        TimeSpan duration = SpellUtils.ExtendSpell(eventData.MetaMagicFeat, NwTimeSpan.FromHours(caster.CasterLevel));

        switch (eventData.Spell.SpellType)
        {
            case Spell.MageArmor:
                ApplyMageArmor(target, duration);
                break;
            case Spell.ShadowConjurationMageArmor:
                ApplyShadowMageArmor(caster, target, duration);
                break;
        }
    }

    private static void ApplyMageArmor(NwCreature target, TimeSpan duration)
    {
        // Remove existing Mage Armor effect to prevent stacking
        Effect? existingEffect = target.ActiveEffects.FirstOrDefault(e => e.Tag == MageArmorTag);
        if (existingEffect != null)
        {
            target.RemoveEffect(existingEffect);
        }

        // Create AC bonuses: +1 to Armor Enchantment, Deflection, Dodge, and Natural
        Effect acArmor = Effect.ACIncrease(1, ACBonus.ArmourEnchantment);
        Effect acDeflection = Effect.ACIncrease(1, ACBonus.Deflection);
        Effect acDodge = Effect.ACIncrease(1, ACBonus.Dodge);
        Effect acNatural = Effect.ACIncrease(1, ACBonus.Natural);
        Effect durationVfx = Effect.VisualEffect(VfxType.DurCessatePositive);

        Effect mageArmorEffect = Effect.LinkEffects(acArmor, acDeflection, acDodge, acNatural, durationVfx);
        mageArmorEffect.Tag = MageArmorTag;

        // Apply impact VFX and the armor effect
        Effect impactVfx = Effect.VisualEffect(VfxType.ImpAcBonus);
        target.ApplyEffect(EffectDuration.Instant, impactVfx);
        target.ApplyEffect(EffectDuration.Temporary, mageArmorEffect, duration);
    }

    private static void ApplyShadowMageArmor(NwCreature caster, NwCreature target, TimeSpan duration)
    {
        // Remove existing Shadow Mage Armor effect to prevent stacking
        Effect? existingEffect = target.ActiveEffects.FirstOrDefault(e => e.Tag == ShadowMageArmorTag);
        if (existingEffect != null)
        {
            target.RemoveEffect(existingEffect);
        }

        // Base resistance is 5, Epic Spell Focus Illusion adds 5 more
        int resistance = 5;
        if (caster.KnowsFeat(Feat.EpicSpellFocusIllusion!))
        {
            resistance += 5;
        }

        // Create cold and negative energy resistance
        Effect coldResist = Effect.DamageResistance(DamageType.Cold, resistance);
        Effect negativeResist = Effect.DamageResistance(DamageType.Negative, resistance);
        Effect durationVfx = Effect.VisualEffect(VfxType.DurCessatePositive);

        Effect shadowMageArmorEffect = Effect.LinkEffects(coldResist, negativeResist, durationVfx);
        shadowMageArmorEffect.Tag = ShadowMageArmorTag;

        // Apply impact VFX and the armor effect
        Effect impactVfx = Effect.VisualEffect(VfxType.ImpAcBonus);
        target.ApplyEffect(EffectDuration.Instant, impactVfx);
        target.ApplyEffect(EffectDuration.Temporary, shadowMageArmorEffect, duration);
    }
}

