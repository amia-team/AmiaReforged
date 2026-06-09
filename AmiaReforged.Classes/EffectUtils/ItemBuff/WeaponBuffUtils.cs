using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.EffectUtils.ItemBuff;

/// <summary>
/// This service holds functionality and rules for the stacking of weapon buffs, and it should be used for
/// spells that apply weapon buffs
/// </summary>
public static class WeaponBuffUtils
{
    public static readonly Dictionary<IPDamageType, (VfxType vfxType, ItemVisual itemVisual)>
        DamageTypeMap = new()
        {
            { IPDamageType.Fire, (VfxType.ImpPulseFire, ItemVisual.Fire) },
            { IPDamageType.Cold, (VfxType.ImpPulseCold, ItemVisual.Cold) },
            { IPDamageType.Electrical, (VfxType.ImpPulseWind, ItemVisual.Electrical) },
            { IPDamageType.Acid, (VfxType.ImpPulseNature, ItemVisual.Acid) },
            { IPDamageType.Sonic, (VfxType.ImpSuperHeroism, ItemVisual.Sonic) },
            { IPDamageType.Negative, (VfxType.ImpPulseNegative, ItemVisual.Evil) },
            { IPDamageType.Positive, (VfxType.ImpPulseHoly, ItemVisual.Holy) },
        };

    private static readonly Dictionary<int, IPDamageType> BookOfTransmutationMap = new()
    {
        { 1, IPDamageType.Fire },
        { 2, IPDamageType.Cold },
        { 3, IPDamageType.Electrical },
        { 4, IPDamageType.Acid },
        { 5, IPDamageType.Sonic },
        { 6, IPDamageType.Negative },
    };

    private static readonly NwSpell?[] ConflictingWeaponBuffs =
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
    public static void RemoveConflictingWeaponBuffs(NwSpell sourceSpell, NwItem weapon)
    {
        if (!ConflictingWeaponBuffs.Contains(sourceSpell.SpellType)) return;

        foreach (Effect effect in weapon.ActiveEffects)
        {
            if (ConflictingWeaponBuffs.Contains(effect.Spell))
                weapon.RemoveEffect(effect);
        }
    }

    /// <summary>
    /// Selects the best candidate for the weapon buff so that no weapon is downgraded
    /// </summary>
    public static NwItem? SelectWeaponToBuff(SpellEvents.OnSpellCast castData, IPDamageType damageType, IPDamageBonus damageBonus)
    {
        List<NwItem> weaponList = GetWeaponsToBuff(castData);
        if (weaponList.Count == 0) return null;

        NwItem? weaponToBuff = weaponList
            // Filter out items that have a more powerful effect
            .Where(item => !item.ItemProperties.Any(ip =>
                ip is { DurationType: EffectDuration.Temporary, Property.PropertyType: ItemPropertyType.DamageBonus }
                && ip.IntParams[1] == (int)damageType
                && ip.IntParams[3] > (int)damageBonus))
            // First check for items with no damage bonus
            .OrderBy(item => item.ItemProperties.Any(ip =>
                ip is { DurationType: EffectDuration.Temporary, Property.PropertyType: ItemPropertyType.DamageBonus }))
            // then by items without matching damage type
            .ThenBy(item => item.ItemProperties.Any(ip =>
                ip is { DurationType: EffectDuration.Temporary, Property.PropertyType: ItemPropertyType.DamageBonus }
                && ip.IntParams[1] == (int)damageType))
            // lastly by items with less damage bonus
            .ThenByDescending(item => item.ItemProperties.Any(ip =>
                ip is { DurationType: EffectDuration.Temporary, Property.PropertyType: ItemPropertyType.DamageBonus }
                && ip.IntParams[3] < (int)damageBonus))
            // Pick the first item in the sorted list.
            .FirstOrDefault();

        if (weaponToBuff == null)
            NotifySpellFail(castData);

        return weaponToBuff;
    }

    private static void NotifySpellFail(SpellEvents.OnSpellCast castData)
    {
        NwCreature? caster = castData.Caster as NwCreature;
        NwCreature? target = castData.TargetObject as NwCreature
                             ?? (castData.TargetObject as NwItem)?.Possessor as NwCreature;

        string floatString = $"{castData.Spell.Name} failed!".ColorString(ColorConstants.Red);
        string message = "No valid weapon found, or a more powerful effect is already active.".ColorString(ColorConstants.Pink);

        if (caster?.ControllingPlayer != null)
        {
            caster.ControllingPlayer.FloatingTextString(floatString, broadcastToParty: false);
            caster.ControllingPlayer.SendServerMessage(message);
        }

        if (target == caster || target?.ControllingPlayer == null) return;

        target.ControllingPlayer.FloatingTextString(floatString, broadcastToParty: false);
        target.ControllingPlayer.SendServerMessage(message);
    }

    /// <summary>
    /// Gets which target creature's weapon should be enhanced
    /// </summary>
    /// <returns>The list of weapons to consider for enhancements</returns>
    public static List<NwItem> GetWeaponsToBuff(SpellEvents.OnSpellCast castData, bool allowRanged = false)
    {
        if (castData.TargetObject is NwItem targetWeapon && IsAllowedWeapon(targetWeapon, allowRanged))
            return [targetWeapon];

        if (castData.TargetObject is not NwCreature creature)
            return [];

        // Prio 1: weapons
        List<NwItem> weapons = [];
        if (creature.GetItemInSlot(InventorySlot.RightHand) is { } mainWeapon&& IsAllowedWeapon(mainWeapon, allowRanged))
            weapons.Add(mainWeapon);
        if (creature.GetItemInSlot(InventorySlot.LeftHand) is { } offWeapon && IsAllowedWeapon(offWeapon, allowRanged))
            weapons.Add(offWeapon);

        if (weapons.Count > 0)
            return weapons;

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

    private static bool IsAllowedWeapon(NwItem item, bool allowRanged) =>
        IsMeleeWeapon(item) || allowRanged && item.BaseItem.Category == BaseItemCategory.Ranged;

    public static bool IsMeleeWeapon(NwItem item) =>
        item.BaseItem.Category == BaseItemCategory.Melee
        || item.BaseItem.ItemType is BaseItemType.Gloves or BaseItemType.CreatureBludgeoningWeapon
        or BaseItemType.CreaturePiercingWeapon or BaseItemType.CreatureSlashingAndPiercingWeapon
        or BaseItemType.CreatureSlashingWeapon;

    /// <summary>
    /// Centralised scaling for weapon damage buffs based on Caster Level.
    /// </summary>
    public static IPDamageBonus GetFlameWeaponDamageBonus(int casterLevel)
        => casterLevel switch
        {
            >= 25 => IPDamageBonus.Plus1d12,
            >= 20 => IPDamageBonus.Plus1d10,
            >= 15 => IPDamageBonus.Plus1d8,
            >= 10 => IPDamageBonus.Plus1d6,
            _ => IPDamageBonus.Plus1d4,
        };

    private const string BotPrefix = "ds_spell_";

    public static (IPDamageType, int CasterLevel) GetFlameWeaponData(NwGameObject caster, SpellEvents.OnSpellCast castData)
    {
        if (castData.Item != null)
        {
            if (IsWandPotionOrScroll(castData.Item))
                return (IPDamageType.Fire, caster.CasterLevel);

            // Check for items with unique damage and CLs
            if (ItemResRefMap.TryGetValue(castData.Item.ResRef,
                    out (IPDamageType DamageType, int CasterLevel) itemData))
                return (itemData.DamageType, itemData.CasterLevel);
        }

        LocalVariableInt damageTypeKey = caster.GetObjectVariable<LocalVariableInt>(BotPrefix + castData.Spell.Id);
        IPDamageType damageType = BookOfTransmutationMap.GetValueOrDefault(damageTypeKey.Value, IPDamageType.Fire);

        return (damageType, caster.CasterLevel);
    }

    private const string UnknownItemResRef = "itm_coldweapon_dmg";
    private const string WyvernBileResRef = "itm_sc_wyvernbil";
    private const string AuricularEssenceResRef = "itm_sc_auricular";
    private const string SourtoothVenomResRef = "itm_sc_sourtooth";
    private const string VenomGlandResRef = "js_venomgland";
    private const string HighQualityVenomGlandResRef = "js_hqvenomgland";
    private const string LegendaryVenomGlandResRef = "js_lvenomgland";
    private const string WyvernVenomGlandResRef = "js_wyverngland";
    private const string FrostspearsTreasureResRef = "wdragonbossrewar";
    private const string ColdWeaponRune = "itm_coldweapon";

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
        { FrostspearsTreasureResRef, (IPDamageType.Sonic, 17) },
        { ColdWeaponRune, (IPDamageType.Cold, 17) }
    };

    private static bool IsWandPotionOrScroll(NwItem item) => item is { BaseItem.ItemType:
        BaseItemType.Scroll or BaseItemType.EnchantedScroll or BaseItemType.SpellScroll
            or BaseItemType.Potions or BaseItemType.EnchantedPotion
            or BaseItemType.EnchantedWand or BaseItemType.MagicWand };
}
