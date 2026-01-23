using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.EffectUtils.Polymorph;

/// <summary>
/// Service for centralised handling of polymorph equipment merging.
/// </summary>
[ServiceBinding(typeof(PolymorphMergeService))]
public class PolymorphMergeService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string WaterBreathingTag = "ds_underwater";

    private readonly Dictionary<NwCreature, ItemProperty[]> _pendingWeaponMerges = [];
    private readonly Dictionary<NwCreature, ItemProperty[]> _pendingArmorMerges = [];
    private readonly Dictionary<NwCreature, ItemProperty[]> _pendingItemMerges = [];
    private readonly Dictionary<NwCreature, bool> _pendingWaterBreathing = [];

    private readonly InventorySlot[] _polymorphedWeaponSlots =
        [InventorySlot.CreatureLeftWeapon, InventorySlot.CreatureRightWeapon, InventorySlot.CreatureBiteWeapon];

    private readonly InventorySlot[] _armorSlots =
        [InventorySlot.Chest, InventorySlot.LeftHand, InventorySlot.Head];

    private readonly InventorySlot[] _itemSlots =
        [InventorySlot.Arms, InventorySlot.Belt, InventorySlot.Boots, InventorySlot.Cloak, InventorySlot.Neck,
            InventorySlot.LeftRing, InventorySlot.RightRing];

    public PolymorphMergeService(EventService eventService)
    {
        NwModule.Instance.OnPolymorphApply += OnApplyPolymorphBefore;
        eventService.SubscribeAll<OnPolymorphApply, OnPolymorphApply.Factory>(OnApplyPolymorphAfter, EventCallbackType.After);

        Log.Info("Polymorph Merge Service initialized.");
    }

    private void OnApplyPolymorphBefore(OnPolymorphApply eventData)
    {
        NwCreature creature = eventData.Creature;

        if (eventData.PolymorphType.MergeW == true)
        {
            _pendingWeaponMerges[creature] = GetItemPropertiesFromSlot(creature, InventorySlot.RightHand);
        }

        bool hasWaterBreathing = false;

        if (eventData.PolymorphType.MergeA == true)
        {
            _pendingArmorMerges[creature] = GetItemPropertiesFromSlots(creature, _armorSlots,
                out bool? waterBreathingFound);
            if (waterBreathingFound == true)
                hasWaterBreathing = true;
        }

        if (eventData.PolymorphType.MergeI == true)
        {
            _pendingItemMerges[creature] = GetItemPropertiesFromSlots(creature, _itemSlots,
                out bool? waterBreathingFound);
            if (waterBreathingFound == true)
                hasWaterBreathing = true;
        }

        if (hasWaterBreathing)
        {
            _pendingWaterBreathing[creature] = true;
        }
    }


    private void OnApplyPolymorphAfter(OnPolymorphApply eventData)
    {
        NwCreature creature = eventData.Creature;
        NwPlayer? player = creature.ControllingPlayer;
        List<string>? mergeMessages = player != null ? [] : null;

        _pendingWaterBreathing.Remove(creature, out bool hasWaterBreathing);

        HandleWeaponMerges(creature, mergeMessages);
        HandleSkinMerges(creature, mergeMessages, hasWaterBreathing);

        if (player == null || mergeMessages == null) return;

        foreach (string log in mergeMessages)
        {
            player.SendServerMessage(log);
        }
    }

    private void HandleWeaponMerges(NwCreature creature, List<string>? mergeMessages)
    {
        if (!_pendingWeaponMerges.Remove(creature, out ItemProperty[]? weaponProps) || weaponProps.Length == 0)
        {
            return;
        }

        ItemProperty[] filteredWeaponProps = FilterProperties(weaponProps);
        if (filteredWeaponProps.Length == 0) return;

        // If creature has an actual weapon, apply properties to that
        if (creature.GetItemInSlot(InventorySlot.RightHand) is { } polymorphedWeapon)
        {
            ApplyPropertiesToItem(polymorphedWeapon, filteredWeaponProps);
            mergeMessages?.Add(FormatMergeMessage(polymorphedWeapon, filteredWeaponProps));
        }
        else // apply properties to natural weapons
        {
            foreach (InventorySlot slot in _polymorphedWeaponSlots)
            {
                if (creature.GetItemInSlot(slot) is not { } naturalWeapon) continue;
                ApplyPropertiesToItem(naturalWeapon, filteredWeaponProps);
                mergeMessages?.Add(FormatMergeMessage(naturalWeapon, filteredWeaponProps));
            }
        }
    }

    private void HandleSkinMerges(NwCreature creature, List<string>? mergeMessages, bool hasWaterBreathing)
    {
        _pendingArmorMerges.Remove(creature, out ItemProperty[]? armorProps);
        _pendingItemMerges.Remove(creature, out ItemProperty[]? itemProps);

        ItemProperty[] combinedProps = (armorProps ?? []).Concat(itemProps ?? []).ToArray();
        if (combinedProps.Length == 0) return;

        NwItem? creatureSkin = creature.GetItemInSlot(InventorySlot.CreatureSkin);
        if (creatureSkin == null) return;

        ItemProperty[] filteredSkinProps = FilterProperties(combinedProps);
        if (filteredSkinProps.Length == 0) return;

        ApplyPropertiesToItem(creatureSkin, filteredSkinProps);
        mergeMessages?.Add(FormatMergeMessage(creatureSkin, filteredSkinProps));

        if (!hasWaterBreathing) return;
        creatureSkin.Tag = WaterBreathingTag;
        mergeMessages?.Add("Water Breathing".ColorString(ColorConstants.Cyan));
    }

    private static ItemProperty[] GetItemPropertiesFromSlot(NwCreature creature, InventorySlot slot)
        => creature.GetItemInSlot(slot)?.ItemProperties.ToArray() ?? [];

    private static ItemProperty[] GetItemPropertiesFromSlots(NwCreature creature, InventorySlot[] slots, out bool? hasWaterBreathing)
    {
        NwItem[] foundItems = slots
            .Select(creature.GetItemInSlot)
            .OfType<NwItem>()
            .ToArray();

        hasWaterBreathing = foundItems.Any(i => i.Tag == WaterBreathingTag);

        return foundItems.SelectMany(i => i.ItemProperties).ToArray();
    }

    private static ItemProperty[] FilterProperties(IEnumerable<ItemProperty> properties)
    {
        return properties
            .GroupBy(ip => new { ip.Property, ip.SubType })
            .SelectMany(group =>
            {
                // Regeneration and Vampiric Regeneration stack in NWN, so return all of them
                return group.Key.Property.PropertyType
                    is ItemPropertyType.Regeneration or ItemPropertyType.RegenerationVampiric ? group :
                    // For all other types, only return the one with the highest value
                    group.OrderByDescending(ip => ip.CostTableValue?.RowIndex).Take(1);
            })
            .ToArray();
    }

    private static void ApplyPropertiesToItem(NwItem targetItem, ItemProperty[] properties)
    {
        foreach (ItemProperty ip in properties)
        {
            // Respect temporary durations (eg Flame Weapon cast before polymorph)
            if (ip.RemainingDuration > TimeSpan.Zero)
            {
                targetItem.AddItemProperty(ip, EffectDuration.Temporary, ip.RemainingDuration);
            }
            else
            {
                targetItem.AddItemProperty(ip, EffectDuration.Permanent);
            }
        }
    }

    private static string FormatMergeMessage(NwItem targetItem, ItemProperty[] properties)
    {
        List<string> categoryStrings = [];

        var groups = properties.GroupBy(ip => ip.Property).ToArray();

        for (int i = 0; i < groups.Length; i++)
        {
            var group = groups[i];
            string categoryName = group.Key.GameStrRef.ToString() ?? string.Empty;
            string details;

            // Regen and Vamp Regen stack
            if (group.Any(ip => ip.Property.PropertyType is ItemPropertyType.Regeneration or ItemPropertyType.RegenerationVampiric))
            {
                int total = group.Sum(ip => ip.CostTableValue?.RowIndex ?? 0);
                details = $"+{total}";
            }
            else
            {
                // Group multiple values under one category eg "Ability Bonus: Strength +2, Dexterity +2",
                details = string.Join(", ", group.Select(GetPropertyDescription));
            }

            string groupString;

            // if there's no subtype or value for the property, just show the category name, eg "Freedom"
            if (string.IsNullOrWhiteSpace(details))
            {
                groupString = categoryName;
            }
            else
            {
                groupString = details.Contains(categoryName, StringComparison.OrdinalIgnoreCase)
                    ? details
                    : $"{categoryName} {details}";
            }

            // Alternating colors
            Color groupColor = i % 2 == 0 ? ColorConstants.White : ColorConstants.Gray;
            categoryStrings.Add(groupString.ColorString(groupColor));
        }

        string targetItemName = targetItem.BaseItem.Name.ToString();
        if (targetItem.BaseItem.ItemType == BaseItemType.CreatureItem)
            targetItemName = "Creature Hide";

        string header = $"[Polymorph] {targetItemName} merged with:\n".ColorString(ColorConstants.Lime);
        string propertyList = string.Join("\n", categoryStrings);
        return $"{header}{propertyList}";
    }

    private static string GetPropertyDescription(ItemProperty ip)
    {
        string subTypeName = ip.SubType?.Name.ToString() ?? string.Empty;
        string valueName = ip.CostTableValue?.Name.ToString() ?? string.Empty;

        // If we have no subtype, return the value or empty
        if (string.IsNullOrEmpty(subTypeName)) return valueName;

        // If we have no value, return the subtype or empty
        if (string.IsNullOrEmpty(valueName)) return subTypeName;

        // Combine them, checking if value already contains the subtype
        // eg "Electrical" + "50% Damage Vulnerability" -> "Electrical 50% Damage Vulnerability"
        return valueName.Contains(subTypeName, StringComparison.OrdinalIgnoreCase)
            ? valueName
            : $"{subTypeName} {valueName}";
    }
}
