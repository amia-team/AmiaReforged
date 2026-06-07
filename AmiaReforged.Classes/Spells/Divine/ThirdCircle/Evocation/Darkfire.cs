using AmiaReforged.Classes.EffectUtils.ItemBuff;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.ThirdCircle.Evocation;

[ServiceBinding(typeof(ISpell))]
public class Darkfire(ItemBuffService itemBuffService) : ISpell
{
    public string ImpactScript => "X2_S0_Darkfire";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not { } caster) return;

        (IPDamageType damageType, int casterLevel) = WeaponBuffUtils.GetFlameWeaponData(caster, eventData);

        IPDamageBonus damageBonus = WeaponBuffUtils.GetFlameWeaponDamageBonus(casterLevel);

        NwItem? weapon = WeaponBuffUtils.SelectWeaponToBuff(eventData, damageType, damageBonus);

        if (weapon == null) return;

        ItemProperty damageProperty = ItemProperty.DamageBonus(damageType, damageBonus);
        ItemProperty weaponVisual = ItemProperty.VisualEffect(WeaponBuffUtils.DamageTypeMap[damageType].itemVisual);

        TimeSpan duration = NwTimeSpan.FromHours(casterLevel);
        if (eventData.MetaMagicFeat == MetaMagic.Extend)
            duration *= 2;

        itemBuffService.ApplyItemBuff(
            weapon,
            eventData.Spell,
            [damageProperty, weaponVisual],
            EffectSubType.Magical,
            duration);

        VfxType pulseVfx = WeaponBuffUtils.DamageTypeMap[damageType].vfxType;
        eventData.TargetObject?.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(pulseVfx));
    }

    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
}
