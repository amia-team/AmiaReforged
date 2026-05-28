using AmiaReforged.Classes.EffectUtils.ItemBuff;
using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.FourthCircle.Evocation;

/// <summary>
/// The caster energizes themselves with a large flow of electricity, dangerously protecting themselves and
/// enhancing themselves with electrical energy. For the duration, the caster gains 50% movement speed,
/// gains a 25% immunity to electricity, gains +1d8 electrical damage per hit, and has a 2d6 electrical damage shield.
/// This spell does not stack with death armor, elemental shield, wounding whispers, or mestil's acid sheath.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class MorgensElectrifier(DamageShieldService damageShieldService, ItemBuffService itemBuffService) : ISpell
{
    public string ImpactScript => "morgens_elec";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        damageShieldService.RemoveDamageShieldSpells(caster);

        TimeSpan duration = NwTimeSpan.FromRounds(caster.CasterLevel);
        if (eventData.MetaMagicFeat == MetaMagic.Extend) duration *= 2;

        caster.ApplyEffect(EffectDuration.Temporary, MorgensElectrifierEffect, duration);
        caster.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHaste));

        const IPDamageType damageType = IPDamageType.Electrical;
        const IPDamageBonus damageBonus = IPDamageBonus.Plus1d8;

        NwItem? weapon = WeaponBuffUtils.SelectWeaponToBuff(eventData, damageType, damageBonus);
        if (weapon == null) return;

        ItemProperty damageProperty = ItemProperty.DamageBonus(damageType, damageBonus);
        ItemProperty weaponVisual = ItemProperty.VisualEffect(ItemVisual.Electrical);

        itemBuffService.ApplyItemBuff(
            weapon,
            eventData.Spell,
            [damageProperty, weaponVisual],
            EffectSubType.Magical,
            duration);

        caster.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpPulseWind));
    }

    private static Effect MorgensElectrifierEffect =>
        Effect.LinkEffects
        (
            Effect.VisualEffect(AmiaVfxTypes.DurElectricShield),
            Effect.DamageShield(0, DamageBonus.Plus2d6, DamageType.Electrical),
            Effect.MovementSpeedIncrease(50),
            Effect.DamageImmunityIncrease(DamageType.Electrical, 25)
        );

    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public void SetSpellResisted(bool result) { }
}
