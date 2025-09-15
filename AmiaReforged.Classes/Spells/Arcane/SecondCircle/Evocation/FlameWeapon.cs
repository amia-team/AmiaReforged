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

    private const string UnknownItemResRef = "itm_coldweapon_dmg";
    private const string WyvernBileResRef = "itm_sc_wyvernbil";
    private const string AuricularEssenceResRef = "itm_sc_auricular";
    private const string SourtoothVenomResRef = "itm_sc_sourtooth";
    private const string VenomGlandResRef = "js_venomgland";
    private const string HighQualityVenomGlandResRef = "js_hqvenomgland";
    private const string LegendaryVenomGlandResRef = "js_lvenomgland";
    private const string WyvernVenomGlandResRef = "js_wyverngland";
    private const string FrostspearsTreasureResRef = "wdragonbossrewar";

    private static readonly Dictionary<string, (FlameWeaponType, int CasterLevel)> ItemResRefMap = new()
    {
        { UnknownItemResRef, (FlameWeaponType.Cold, 15) },
        { WyvernBileResRef, (FlameWeaponType.Acid, 15) },
        { AuricularEssenceResRef, (FlameWeaponType.Sonic, 15)},
        { SourtoothVenomResRef, (FlameWeaponType.Negative, 15) },
        { VenomGlandResRef, (FlameWeaponType.Acid, 9) },
        { HighQualityVenomGlandResRef, (FlameWeaponType.Acid, 10) },
        { LegendaryVenomGlandResRef, (FlameWeaponType.Acid, 15) },
        { WyvernVenomGlandResRef, (FlameWeaponType.Acid, 20) },
        { FrostspearsTreasureResRef, (FlameWeaponType.Sonic, 17) }
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

        NwItem? weapon = FindTargetWeapon(eventData.TargetObject, eventData.Caster, damageBonus, damageType);

        if (weapon == null) return;

        ItemProperty damageProperty = ItemProperty.DamageBonus(damageType, damageBonus);
        ItemProperty weaponVisual = ItemProperty.VisualEffect(FlameWeaponMap[flameWeaponType].itemVisual);

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

        VfxType pulseVfx = FlameWeaponMap[flameWeaponType].vfxType;
        eventData.TargetObject?.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(pulseVfx));
    }

    private NwItem? FindTargetWeapon(NwGameObject? targetObject, NwGameObject caster, IPDamageBonus damageBonus, IPDamageType damageType)
    {
        if (targetObject is NwItem targetItem)
            return targetItem;

        if (targetObject is not NwCreature targetCreature)
            return null;

        caster.IsPlayerControlled(out NwPlayer? player);

        NwItem?[] itemsToCheck =
        [
            targetCreature.GetItemInSlot(InventorySlot.RightHand),
            targetCreature.GetItemInSlot(InventorySlot.LeftHand),
            targetCreature.GetItemInSlot(InventorySlot.Arms)
        ];

        itemsToCheck = itemsToCheck
            .Where(item => item != null && (item.BaseItem.Category == BaseItemCategory.Melee || item.BaseItem.ItemType == BaseItemType.Gloves))
            .ToArray();

        if (itemsToCheck.Length == 0)
        {
            player?.FloatingTextString
                ($"No weapon or gloves found on {targetCreature.Name} to cast Flame Weapon.", false);

            return null;
        }

        NwItem? weapon = itemsToCheck
            // Filter out items that have a more powerful effect
            .Where(item => item != null && !item.ItemProperties.Any(ip =>
                ip is { DurationType: EffectDuration.Temporary, Property.PropertyType: ItemPropertyType.DamageBonus }
                && ip.IntParams[1] == (int)damageType
                && ip.IntParams[3] > (int)damageBonus))
            // First check for items with no damage bonus
            .OrderBy(item => item!.ItemProperties.Any(ip =>
                ip is { DurationType: EffectDuration.Temporary, Property.PropertyType: ItemPropertyType.DamageBonus }))
            // then by items without matching damage type
            .ThenBy(item => item!.ItemProperties.Any(ip =>
                ip is { DurationType: EffectDuration.Temporary, Property.PropertyType: ItemPropertyType.DamageBonus }
                && ip.IntParams[1] == (int)damageType))
            // lastly by items with less damage bonus
            .ThenByDescending(item => item!.ItemProperties.Any(ip =>
                ip is { DurationType: EffectDuration.Temporary, Property.PropertyType: ItemPropertyType.DamageBonus }
                && ip.IntParams[3] < (int)damageBonus))
            // Pick the first item in the sorted list.
            .FirstOrDefault();

        if (weapon != null) return weapon;

        player?.FloatingTextString
            ($"{targetCreature.Name} already has a more powerful weapon effect.", false);

        return null;
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

    public void SetSpellResisted(bool result) { }
}
