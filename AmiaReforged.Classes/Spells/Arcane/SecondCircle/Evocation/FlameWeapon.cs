using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.SecondCircle.Evocation;

[ServiceBinding(typeof(ISpell))]

public class FlameWeapon : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "X2_S0_FlmeWeap";

    private enum FlameWeaponType
    {
        Fire = 1,
        Cold = 2,
        Electrical = 3,
        Acid = 4,
        Sonic = 5,
        Negative = 6
    }

    private static readonly Dictionary<FlameWeaponType, (IPDamageType ipDamageType, VfxType vfxType, ItemVisual itemVisual)>
        FlameWeaponMap = new()
    {
        { FlameWeaponType.Fire, (IPDamageType.Fire, VfxType.ImpPulseFire, ItemVisual.Fire) },
        { FlameWeaponType.Cold, (IPDamageType.Cold, VfxType.ImpPulseCold, ItemVisual.Cold) },
        { FlameWeaponType.Electrical, (IPDamageType.Electrical, VfxType.ImpPulseWind, ItemVisual.Electrical) },
        { FlameWeaponType.Acid, (IPDamageType.Acid, VfxType.ImpPulseNature, ItemVisual.Acid) },
        { FlameWeaponType.Sonic, (IPDamageType.Sonic, VfxType.ImpSuperHeroism, ItemVisual.Sonic) },
        { FlameWeaponType.Negative, (IPDamageType.Negative, VfxType.ImpPulseNegative, ItemVisual.Evil) }
    };

    private static readonly Dictionary<string, (FlameWeaponType, int CasterLevel)> ItemResRefMap = new()
    {
        { "itm_coldweapon_dmg", (FlameWeaponType.Cold, 15) }, // Unknown item?
        { "itm_sc_wyvernbil", (FlameWeaponType.Acid, 15) }, // Wyvern Bile
        { "itm_sc_auricular", (FlameWeaponType.Sonic, 15)}, // Auricular Essence
        { "itm_sc_sourtoothvenom", (FlameWeaponType.Negative, 15) }, // Sourtooth Venom
        { "js_venomgland", (FlameWeaponType.Acid, 9) }, // Venom Gland
        { "js_hqvenomgland", (FlameWeaponType.Acid, 10) }, // HQ Venom Gland
        { "js_lvenomgland", (FlameWeaponType.Acid, 15) }, // Legendary Venom Gland
        { "js_wyverngland", (FlameWeaponType.Acid, 20) } // Wyvern Venom Gland
    };

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster == null) return;

        (FlameWeaponType flameWeaponType, int casterLevel) = GetFlameWeaponData(eventData, eventData.Caster);

        TimeSpan duration = NwTimeSpan.FromHours(casterLevel);
        if (eventData.MetaMagicFeat == MetaMagic.Extend)
            duration *= 2;

        IPDamageBonus damageBonus = GetDamageBonus(casterLevel);
        IPDamageType damageType = FlameWeaponMap[flameWeaponType].ipDamageType;

        NwItem? weapon = FindTargetWeapon(eventData.TargetObject, damageBonus, damageType);

        if (weapon == null)
        {
            if (!eventData.Caster.IsPlayerControlled(out NwPlayer? player)) return;

            player.FloatingTextString
                ($"No weapon or gloves found on {eventData.TargetObject?.Name} to cast Flame Weapon.");
            player.SendServerMessage
                ("Either no weapon or gloves are equipped or they already have a more powerful Flame Weapon.");

            return;
        }

        ItemProperty damageProperty = ItemProperty.DamageBonus(damageType, damageBonus);
        ItemProperty weaponVisual = ItemProperty.VisualEffect(FlameWeaponMap[flameWeaponType].itemVisual);

        weapon.AddItemProperty
        (
            damageProperty,
            EffectDuration.Temporary,
            duration,
            AddPropPolicy.ReplaceExisting,
            ignoreSubType: false
        );

        weapon.AddItemProperty
        (
            weaponVisual,
            EffectDuration.Temporary,
            duration,
            AddPropPolicy.ReplaceExisting,
            ignoreSubType: false
        );
    }

    private NwItem? FindTargetWeapon(NwGameObject? targetObject, IPDamageBonus damageBonus, IPDamageType damageType)
    {
        if (targetObject is NwItem targetItem)
            return targetItem;

        if (targetObject is not NwCreature targetCreature) return null;

        NwItem?[] itemsToCheck =
        [
            targetCreature.GetItemInSlot(InventorySlot.RightHand),
            targetCreature.GetItemInSlot(InventorySlot.LeftHand),
            targetCreature.GetItemInSlot(InventorySlot.Arms)
        ];

        NwItem? itemToEnhance = itemsToCheck
            .FirstOrDefault(item => item != null &&
                                    (item.BaseItem.Category == BaseItemCategory.Melee
                                     || item.BaseItem.ItemType == BaseItemType.Gloves) &&
                                    !item.ItemProperties.Any(ip =>
                                        ip.DurationType == EffectDuration.Temporary &&
                                        ip.Property.PropertyType == ItemPropertyType.DamageBonus &&
                                        ip.IntParams[0] > (int)damageBonus &&
                                        ip.IntParams[1] == (int)damageType));

        return itemToEnhance == null ? null : itemToEnhance;
    }


    private (FlameWeaponType, int) GetFlameWeaponData(SpellEvents.OnSpellCast eventData, NwGameObject caster)
    {
        LocalVariableInt flameWeaponTypeKey =
            caster.GetObjectVariable<LocalVariableInt>("ds_spell_" + eventData.Spell.Id);

        if (eventData.Item is { } item)
        {
            if (item.BaseItem.ItemType is
                BaseItemType.Scroll or BaseItemType.EnchantedScroll or BaseItemType.SpellScroll
                or BaseItemType.Potions or BaseItemType.EnchantedPotion
                or BaseItemType.EnchantedWand or BaseItemType.MagicWand)
                flameWeaponTypeKey.Value = 1;

            else if (ItemResRefMap.TryGetValue(item.ResRef, out (FlameWeaponType FlameWeaponType, int CasterLevel) itemData))
                return (itemData.FlameWeaponType, itemData.CasterLevel);
        }

        int casterLevel = eventData.Caster?.CasterLevel ?? 0;

        // default value to 1 for unexpected values
        if (flameWeaponTypeKey.Value is < 1 or > 6)
            flameWeaponTypeKey.Value = 1;

        return ((FlameWeaponType)flameWeaponTypeKey.Value, casterLevel);
    }

    private IPDamageBonus GetDamageBonus(int casterLevel)
        => casterLevel switch
        {
            >= 25 => IPDamageBonus.Plus1d12,
            >= 20 => IPDamageBonus.Plus1d10,
            >= 15 => IPDamageBonus.Plus1d8,
            >= 10 => IPDamageBonus.Plus1d6,
            _ => IPDamageBonus.Plus1d4,
        };

    public void SetSpellResisted(bool result)
    {
        throw new NotImplementedException();
    }
}
