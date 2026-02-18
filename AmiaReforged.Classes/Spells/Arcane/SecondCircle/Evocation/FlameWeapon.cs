using AmiaReforged.Classes.EffectUtils.WeaponBuff;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.SecondCircle.Evocation;

[ServiceBinding(typeof(ISpell))]

public class FlameWeapon(WeaponBuffService weaponBuffService) : ISpell
{
    public string ImpactScript => "X2_S0_FlmeWeap";

    private const string UnknownItemResRef = "itm_coldweapon_dmg";
    private const string WyvernBileResRef = "itm_sc_wyvernbil";
    private const string AuricularEssenceResRef = "itm_sc_auricular";
    private const string SourtoothVenomResRef = "itm_sc_sourtooth";
    private const string VenomGlandResRef = "js_venomgland";
    private const string HighQualityVenomGlandResRef = "js_hqvenomgland";
    private const string LegendaryVenomGlandResRef = "js_lvenomgland";
    private const string WyvernVenomGlandResRef = "js_wyverngland";
    private const string FrostspearsTreasureResRef = "wdragonbossrewar";

    private static readonly Dictionary<string, (IPDamageType DamageType, int CasterLevel)> ItemResRefMap = new()
    {
        { UnknownItemResRef, (IPDamageType.Cold, 15) },
        { WyvernBileResRef, (IPDamageType.Acid, 15) },
        { AuricularEssenceResRef, (IPDamageType.Sonic, 15) },
        { SourtoothVenomResRef, (IPDamageType.Negative, 15) },
        { VenomGlandResRef, (IPDamageType.Acid, 9) },
        { HighQualityVenomGlandResRef, (IPDamageType.Acid, 10) },
        { LegendaryVenomGlandResRef, (IPDamageType.Acid, 15) },
        { WyvernVenomGlandResRef, (IPDamageType.Acid, 20) },
        { FrostspearsTreasureResRef, (IPDamageType.Sonic, 17) }
    };

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster == null) return;

        (IPDamageType damageType, int casterLevel) = GetFlameWeaponData(eventData, eventData.Caster);

        IPDamageBonus damageBonus = GetDamageBonus(casterLevel);

        NwItem? weapon = weaponBuffService.SelectWeaponToBuff(eventData, damageType, damageBonus);

        if (weapon == null) return;

        ItemProperty damageProperty = ItemProperty.DamageBonus(damageType, damageBonus);
        ItemProperty weaponVisual = ItemProperty.VisualEffect(weaponBuffService.ElementalDamageMap[damageType].itemVisual);

        TimeSpan duration = NwTimeSpan.FromHours(casterLevel);
        if (eventData.MetaMagicFeat == MetaMagic.Extend)
            duration *= 2;

        weaponBuffService.RemoveConflictingWeaponBuffs(eventData.Spell, weapon);

        weapon.AddItemProperty
        (
            damageProperty,
            EffectDuration.Temporary,
            duration,
            AddPropPolicy.ReplaceExisting,
            ignoreSubType: true
        );

        weapon.AddItemProperty
        (
            weaponVisual,
            EffectDuration.Temporary,
            duration,
            AddPropPolicy.ReplaceExisting,
            ignoreSubType: true
        );

        VfxType pulseVfx = weaponBuffService.ElementalDamageMap[damageType].vfxType;
        eventData.TargetObject?.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(pulseVfx));
    }

    private static IPDamageBonus GetDamageBonus(int casterLevel)
        => casterLevel switch
        {
            >= 25 => IPDamageBonus.Plus1d12,
            >= 20 => IPDamageBonus.Plus1d10,
            >= 15 => IPDamageBonus.Plus1d8,
            >= 10 => IPDamageBonus.Plus1d6,
            _ => IPDamageBonus.Plus1d4,
        };

    private (IPDamageType, int) GetFlameWeaponData(SpellEvents.OnSpellCast eventData, NwGameObject caster)
    {
        int casterLevel = eventData.Caster?.CasterLevel ?? 0;

        if (eventData.Item is { } item)
        {
            if (item.BaseItem.ItemType is
                BaseItemType.Scroll or BaseItemType.EnchantedScroll or BaseItemType.SpellScroll
                or BaseItemType.Potions or BaseItemType.EnchantedPotion
                or BaseItemType.EnchantedWand or BaseItemType.MagicWand)
                return (IPDamageType.Fire, casterLevel);

            if (ItemResRefMap.TryGetValue(item.ResRef, out (IPDamageType DamageType, int CasterLevel) itemData))
                return (itemData.DamageType, itemData.CasterLevel);
        }

        LocalVariableInt damageTypeKey =
            caster.GetObjectVariable<LocalVariableInt>("ds_spell_" + eventData.Spell.Id);

        // default value fire for unexpected values
        IPDamageType damageType = weaponBuffService.BookOfTransmutationMap.GetValueOrDefault(damageTypeKey.Value, IPDamageType.Fire);

        return (damageType, casterLevel);
    }

    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
}
