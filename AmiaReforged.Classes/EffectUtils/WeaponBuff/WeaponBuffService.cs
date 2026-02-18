using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.EffectUtils.WeaponBuff;

/// <summary>
/// This service holds functionality and rules for the stacking of weapon buffs, and it should be used for
/// spells that apply weapon buffs
/// </summary>
[ServiceBinding(typeof(WeaponBuffService))]
public class WeaponBuffService
{
    public readonly Dictionary<IPDamageType, (VfxType vfxType, ItemVisual itemVisual)>
        ElementalDamageMap = new()
        {
            { IPDamageType.Fire, (VfxType.ImpPulseFire, ItemVisual.Fire) },
            { IPDamageType.Cold, (VfxType.ImpPulseCold, ItemVisual.Cold) },
            { IPDamageType.Electrical, (VfxType.ImpPulseWind, ItemVisual.Electrical) },
            { IPDamageType.Acid, (VfxType.ImpPulseNature, ItemVisual.Acid) },
            { IPDamageType.Sonic, (VfxType.ImpSuperHeroism, ItemVisual.Sonic) },
            { IPDamageType.Negative, (VfxType.ImpPulseNegative, ItemVisual.Evil) },
            { IPDamageType.Positive, (VfxType.ImpPulseHoly, ItemVisual.Holy) },
        };

    public readonly Dictionary<int, IPDamageType> BookOfTransmutationMap = new()
    {
        { 1, IPDamageType.Fire },
        { 2, IPDamageType.Cold },
        { 3, IPDamageType.Electrical },
        { 4, IPDamageType.Acid },
        { 5, IPDamageType.Sonic },
        { 6, IPDamageType.Negative },
    };

    private readonly NwSpell?[] _conflictingWeaponBuffs =
    [
        NwSpell.FromSpellType(Spell.FlameWeapon),
        NwSpell.FromSpellType(Spell.Darkfire),
    ];

    /// <summary>
    /// This checks if the weapon buff spell should not be stacking with other similar effects and removes them.
    /// Use this before applying the weapon buffs.
    /// </summary>
    /// <param name="sourceSpell">The weapon enhancing spell being cast</param>
    /// <param name="weapon">The target weapon being buffed</param>
    public void RemoveConflictingWeaponBuffs(NwSpell sourceSpell, NwItem weapon)
    {
        if (!_conflictingWeaponBuffs.Contains(sourceSpell.SpellType)) return;

        foreach (ItemProperty property in weapon.ItemProperties)
        {
            if (_conflictingWeaponBuffs.Contains(property.Spell))
                weapon.RemoveItemProperty(property);
        }
    }

    /// <summary>
    /// Selects the best candidate for the weapon buff so that no weapon is downgraded
    /// </summary>
    public NwItem? SelectWeaponToBuff(SpellEvents.OnSpellCast castData, IPDamageType damageType, IPDamageBonus damageBonus)
    {
        List<NwItem>? weaponList = GetWeaponsToBuff(castData);
        if (weaponList == null || weaponList.Count == 0) return null;

        return weaponList
            // Filter out items that have a more powerful effect
            .Where(item => !item.ItemProperties.Any(ip =>
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
    }

    /// <summary>
    /// Gets which target creature's weapon should be enhanced
    /// </summary>
    /// <returns>The list of weapons to consider for enhancements</returns>
    private static List<NwItem>? GetWeaponsToBuff(SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetObject is NwItem { BaseItem.Category: BaseItemCategory.Melee } meleeWeapon)
            return [meleeWeapon];

        if (castData.TargetObject is not NwCreature creature)
            return null;

        // Prio 1: weapons
        List<NwItem> meleeWeapons = [];
        if (creature.GetItemInSlot(InventorySlot.RightHand) is { BaseItem.Category: BaseItemCategory.Melee } mainWeapon)
            meleeWeapons.Add(mainWeapon);
        if (creature.GetItemInSlot(InventorySlot.LeftHand) is { BaseItem.Category: BaseItemCategory.Melee } offWeapon)
            meleeWeapons.Add(offWeapon);

        if (meleeWeapons.Count > 0)
            return meleeWeapons;

        // Prio 2: gloves
        NwItem? armItem = creature.GetItemInSlot(InventorySlot.Arms);
        if (armItem is { BaseItem.ItemType: BaseItemType.Gloves })
            return [armItem];

        // Prio 3: creature weapons
        List<NwItem> creatureWeapons = [];
        if (creature.GetItemInSlot(InventorySlot.CreatureRightWeapon) is { } rightClaw)
            creatureWeapons.Add(rightClaw);
        if (creature.GetItemInSlot(InventorySlot.CreatureLeftWeapon) is { } leftClaw)
            creatureWeapons.Add(leftClaw);
        if (creature.GetItemInSlot(InventorySlot.CreatureBiteWeapon) is { } bite)
            creatureWeapons.Add(bite);

        return creatureWeapons;
    }
}
