using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.ThirdCircle.Evocation;

[ServiceBinding(typeof(ISpell))]

public class Darkfire : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "X2_S0_Darkfire";

    private enum DarkfireType
    {
        Fire = 1,
        Cold = 2,
        Electrical = 3
    }

    private static readonly Dictionary<DarkfireType, (IPDamageType ipDamageType, VfxType vfxType, ItemVisual itemVisual)>
        DarkfireMap = new()
    {
        { DarkfireType.Fire, (IPDamageType.Fire, VfxType.ImpPulseFire, ItemVisual.Fire) },
        { DarkfireType.Cold, (IPDamageType.Cold, VfxType.ImpPulseCold, ItemVisual.Cold) },
        { DarkfireType.Electrical, (IPDamageType.Electrical, VfxType.ImpPulseWind, ItemVisual.Electrical) }
    };

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster == null) return;

        DarkfireType darkFireType = GetDarkfireType(eventData, eventData.Caster);

        int casterLevel = eventData.Caster.CasterLevel;

        TimeSpan duration = NwTimeSpan.FromHours(casterLevel);
        if (eventData.MetaMagicFeat == MetaMagic.Extend)
            duration *= 2;

        IPDamageBonus damageBonus = GetDamageBonus(casterLevel);
        IPDamageType damageType = DarkfireMap[darkFireType].ipDamageType;

        NwItem? weapon = FindTargetWeapon(eventData.TargetObject, eventData.Caster, damageBonus, damageType);

        if (weapon == null) return;

        ItemProperty damageProperty = ItemProperty.DamageBonus(damageType, damageBonus);
        ItemProperty weaponVisual = ItemProperty.VisualEffect(DarkfireMap[darkFireType].itemVisual);

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

        VfxType pulseVfx = DarkfireMap[darkFireType].vfxType;
        eventData.TargetObject?.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(pulseVfx));
    }

    private NwItem? FindTargetWeapon(NwGameObject? targetObject, NwGameObject caster, IPDamageBonus damageBonus, IPDamageType damageType)
    {
        if (targetObject is NwItem targetItem)
            return targetItem;

        if (targetObject is not NwCreature targetCreature)
            return null;

        caster.IsPlayerControlled(out NwPlayer? player);

        NwItem?[] itemsToEnhance = GetItemsToEnhance(targetCreature);

        if (itemsToEnhance.Length == 0)
        {
            player?.FloatingTextString
                ($"No weapon or gloves found on {targetCreature.Name} to cast Darkfire.", false);

            return null;
        }

        NwItem? weapon = itemsToEnhance
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

    private static NwItem?[] GetItemsToEnhance(NwCreature targetCreature)
    {
        // Prio 1: weapons
        NwItem?[] weapons = new[]
        {
            targetCreature.GetItemInSlot(InventorySlot.RightHand),
            targetCreature.GetItemInSlot(InventorySlot.LeftHand)
        }
        .Where(item => item is { BaseItem.Category: BaseItemCategory.Melee }).ToArray();

        if (weapons.Length > 0)
            return weapons;

        // Prio 2: gloves
        NwItem? armItem = targetCreature.GetItemInSlot(InventorySlot.Arms);
        if (armItem is { BaseItem.ItemType: BaseItemType.Gloves })
            return [armItem];

        // Prio 3: creature weapons
        return new[]
        {
            targetCreature.GetItemInSlot(InventorySlot.CreatureRightWeapon),
            targetCreature.GetItemInSlot(InventorySlot.CreatureLeftWeapon),
            targetCreature.GetItemInSlot(InventorySlot.CreatureBiteWeapon)
        }
        .Where(item => item != null).ToArray();
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

    private DarkfireType GetDarkfireType(SpellEvents.OnSpellCast eventData, NwGameObject caster)
    {
        if (eventData.Item is { BaseItem.ItemType:
                BaseItemType.Scroll or BaseItemType.EnchantedScroll or BaseItemType.SpellScroll
                or BaseItemType.Potions or BaseItemType.EnchantedPotion
                or BaseItemType.EnchantedWand or BaseItemType.MagicWand })
            return DarkfireType.Fire;

        LocalVariableInt flameWeaponTypeKey =
            caster.GetObjectVariable<LocalVariableInt>("ds_spell_" + eventData.Spell.Id);

        // default value fire for unexpected values
        if (flameWeaponTypeKey.Value is < 1 or > 6)
            flameWeaponTypeKey.Value = (int)DarkfireType.Fire;

        return (DarkfireType)flameWeaponTypeKey.Value;
    }

    public void SetSpellResisted(bool result) { }
}
