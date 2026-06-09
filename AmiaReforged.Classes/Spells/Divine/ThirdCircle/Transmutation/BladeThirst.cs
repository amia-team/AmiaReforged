using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.EffectUtils.ItemBuff;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.ThirdCircle.Transmutation;

[ServiceBinding(typeof(ISpell))]
public class BladeThirst(ItemBuffService itemBuffService) : ISpell
{
    public string ImpactScript => "X2_S0_BldeThst";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not { } caster) return;

        TimeSpan duration = NwTimeSpan.FromTurns(caster.CasterLevel);
        if (eventData.MetaMagicFeat == MetaMagic.Extend) duration *= 2;

        List<NwItem> weaponCandidates = WeaponBuffUtils.GetWeaponsToBuff(eventData, allowRanged: true);

        if (weaponCandidates.Count == 0) return;
        NwItem weapon = DetermineWeaponToBuff(weaponCandidates, eventData.Spell);

        ItemProperty[] properties = GetBladeThirstProperties(weapon, caster.CasterLevel);
        itemBuffService.ApplyItemBuff(weapon, eventData.Spell, properties, EffectSubType.Magical, duration);


        if (eventData.TargetObject is NwItem item && item.Possessor != null)
        {
            item.Possessor.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpPulseWater));
        }
        else
        {
            eventData.TargetObject?.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpPulseWater));
        }
    }

    private static NwItem DetermineWeaponToBuff(List<NwItem> weaponCandidates, NwSpell spell)
    {
        if (weaponCandidates.Count == 1)
            return weaponCandidates[0];

        // Check for weapons that don't already have Blade Thirst
        NwItem? unbuffedWeapon = weaponCandidates.FirstOrDefault(w
            => w.ItemProperties.All(ip => ip.Spell != spell));
        return unbuffedWeapon ?? weaponCandidates[0];
    }

    private static ItemProperty[] GetBladeThirstProperties(NwItem weapon, int casterLevel)
    {
        int bonusAmount =
            casterLevel <= 12 ? 0 :
            casterLevel <= 14 ? 1 :
            casterLevel <= 16 ? 2 :
            casterLevel <= 18 ? 3 :
            casterLevel <= 20 ? 4 :
            5;

        if (weapon.IsRangedWeapon)
        {
            if (bonusAmount == 0) bonusAmount = 1;
            return [ItemProperty.AttackBonus(bonusAmount), ItemProperty.MaxRangeStrengthMod(bonusAmount)];
        }

        if (bonusAmount == 0) return [ItemProperty.Keen(), ItemProperty.VisualEffect(AmiaItemVisuals.BlueFire)];
        return [ItemProperty.EnhancementBonus(bonusAmount), ItemProperty.Keen(), ItemProperty.VisualEffect(AmiaItemVisuals.BlueFire)];
    }

    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
}
