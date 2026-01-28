using System.Text;
using System.Text.RegularExpressions;
using AmiaReforged.PwEngine.Features.Crafting;
using AmiaReforged.PwEngine.Features.Crafting.Models;
using Anvil.API;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.NwObjectHelpers;

public static class ItemPropertyHelper
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static List<string> ItemPropertyLabelsFor(NwItem item) =>
        item.ItemProperties.Select(GameLabel).ToList();

    public static string GameLabel(ItemProperty property)
    {
        string label = string.Empty;

        if (property.Property.GameStrRef == null) return label;

        label += property.Property.GameStrRef;

        ItemPropertySubTypeTableEntry? subType = property.SubType;
        if (subType != null) label += " " + subType.Label;

        ItemPropertyParamTableEntry? param1Value = property.Param1TableValue;
        ItemPropertyCostTableEntry? costTableValue = property.CostTableValue;


        if (param1Value != null || costTableValue != null)
        {
            if (costTableValue != null) label += " " + costTableValue.Label;

            if (param1Value != null) label += " " + param1Value.Label;
        }

        return label;
    }

    public static CraftingProperty ToCraftingProperty(ItemProperty ip, IReadOnlyList<CraftingCategory>? categories = null)
    {
        LogManager.GetCurrentClassLogger().Info("=== Converting property to CraftingProperty ===");

        string fullDesc = FullPropertyDescription(ip);
        string gameLabel = fullDesc.Replace(oldValue: "_", newValue: " ");

        LogManager.GetCurrentClassLogger().Info($"Property Type: {ip.Property.PropertyType}");
        LogManager.GetCurrentClassLogger().Info($"Full Description: {fullDesc}");
        LogManager.GetCurrentClassLogger().Info($"Game Label: {gameLabel}");

        // Try to find the property in the categories to get the correct PowerCost
        int powerCost = 2; // Default fallback
        if (categories != null)
        {
            LogManager.GetCurrentClassLogger().Info($"Searching through {categories.Count} categories for matching property...");

            List<CraftingProperty> allProperties = categories.SelectMany(c => c.Properties).ToList();
            LogManager.GetCurrentClassLogger().Info($"Total properties in categories: {allProperties.Count}");

            // Try to find matching property using PropertiesAreSame (string comparison)
            CraftingProperty? matchingProperty = allProperties.FirstOrDefault(p => PropertiesAreSame(p.ItemProperty, ip));

            if (matchingProperty == null)
            {
                // Try alternative matching: same property type and same game label
                LogManager.GetCurrentClassLogger().Info($"First match attempt failed, trying game label matching...");
                string ipGameLabel = GameLabel(ip);
                matchingProperty = allProperties.FirstOrDefault(p =>
                    p.ItemProperty.Property.PropertyType == ip.Property.PropertyType &&
                    GameLabel(p.ItemProperty) == ipGameLabel);

                if (matchingProperty != null)
                {
                    LogManager.GetCurrentClassLogger().Info($"✓ MATCH FOUND via GameLabel! Label: '{ipGameLabel}', PowerCost: {matchingProperty.PowerCost}");
                }
                // Special case: If it's a Keen property and we still haven't found a match,
                // look for any Keen property in the categories (handles both Keen and KeenThrown)
                else if (ip.Property.PropertyType == ItemPropertyType.Keen)
                {
                    LogManager.GetCurrentClassLogger().Info($"Keen property detected - searching for any Keen in categories...");
                    matchingProperty = allProperties.FirstOrDefault(p => p.ItemProperty.Property.PropertyType == ItemPropertyType.Keen);
                    if (matchingProperty != null)
                    {
                        LogManager.GetCurrentClassLogger().Info($"✓ MATCH FOUND via Keen type matching! PowerCost: {matchingProperty.PowerCost}");
                    }
                }
            }
            else
            {
                LogManager.GetCurrentClassLogger().Info($"✓ MATCH FOUND via PropertiesAreSame! GuiLabel: '{matchingProperty.GuiLabel}', PowerCost: {matchingProperty.PowerCost}");
            }

            if (matchingProperty != null)
            {
                powerCost = matchingProperty.PowerCost;
            }
            else
            {
                powerCost = GetPowerCost(ip);
                LogManager.GetCurrentClassLogger().Warn($"✗ NO MATCH FOUND - Using calculated PowerCost: {powerCost}");
                LogManager.GetCurrentClassLogger().Warn($"  This property may not be in any category definitions!");
                LogManager.GetCurrentClassLogger().Warn($"  GameLabel: '{GameLabel(ip)}'");
            }
        }
        else
        {
            powerCost = GetPowerCost(ip);
            LogManager.GetCurrentClassLogger().Warn($"No categories provided - Using calculated PowerCost: {powerCost}");
        }

        LogManager.GetCurrentClassLogger().Info($"=== Conversion complete - PowerCost: {powerCost} ===");

        return new CraftingProperty
        {
            ItemProperty = ip,
            GuiLabel = gameLabel,
            PowerCost = powerCost,
            CraftingTier = CraftingTier.Wondrous,
            Removable = CanBeRemoved(ip)
        };
    }

    private static int GetPowerCost(ItemProperty ip)
    {
        // Check for "On Hit: None" which is a placeholder/empty property
        string gameLabel = GameLabel(ip);
        if (gameLabel.Contains("On Hit: None", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        List<ItemPropertyType> noCost =
        [
            ItemPropertyType.DecreasedAbilityScore,
            ItemPropertyType.DecreasedAc,
            ItemPropertyType.DecreasedSavingThrows,
            ItemPropertyType.DecreasedSkillModifier,
            ItemPropertyType.DecreasedDamage,
            ItemPropertyType.DecreasedAttackModifier,
            ItemPropertyType.WeightIncrease,
            ItemPropertyType.Material,
            ItemPropertyType.Quality,
            ItemPropertyType.Trap,
            ItemPropertyType.Additional,
            ItemPropertyType.UseLimitationClass,
            ItemPropertyType.UseLimitationAlignmentGroup,
            ItemPropertyType.UseLimitationRacialType,
            ItemPropertyType.UseLimitationSpecificAlignment,
            ItemPropertyType.NoDamage,
            ItemPropertyType.CastSpell,
            ItemPropertyType.Light
        ];

        if (noCost.Any(it => it == ip.Property.PropertyType)) return 0;

        // Special handling for Keen property - it's 1 point for most weapons, 2 for thrown weapons
        // This ensures Keen is always recognized correctly regardless of whether it matches a category
        if (ip.Property.PropertyType == ItemPropertyType.Keen)
        {
            // Default is 1 point (for melee/ranged weapons)
            // Note: The item context isn't available here, so we return 1 as the default
            // The actual item type check happens in ToCraftingProperty where categories are available
            LogManager.GetCurrentClassLogger().Info("Keen property detected - using default PowerCost of 1");
            return 1;
        }

        // Default fallback - this should rarely be used if categories are comprehensive
        // The ToCraftingProperty method should find matching properties in categories first
        return 2;
    }

    public static bool CanBeRemoved(ItemProperty itemProperty)
    {
        return itemProperty.Property.PropertyType switch
        {
            ItemPropertyType.DamageVulnerability => false,
            ItemPropertyType.NoDamage => false,
            ItemPropertyType.DecreasedAbilityScore => false,
            ItemPropertyType.DecreasedAc => false,
            ItemPropertyType.DecreasedSavingThrows => false,
            ItemPropertyType.DecreasedSkillModifier => false,
            ItemPropertyType.DecreasedDamage => false,
            ItemPropertyType.DecreasedAttackModifier => false,
            ItemPropertyType.WeightIncrease => false,
            ItemPropertyType.Material => false,
            ItemPropertyType.Quality => false,
            ItemPropertyType.Trap => false,
            ItemPropertyType.UseLimitationClass => false,
            ItemPropertyType.UseLimitationAlignmentGroup => false,
            ItemPropertyType.UseLimitationRacialType => false,
            ItemPropertyType.UseLimitationSpecificAlignment => false,
            ItemPropertyType.Additional => false,
            _ => true
        };
    }

    public static Dictionary<CraftingTier, int> GetMythals(NwPlayer player)
    {
        Log.Info($"Getting mythals for player: {player.PlayerName}.");
        Dictionary<string, CraftingTier> mythalMap = ResRefToTierMap();

        Dictionary<CraftingTier, int> mythals = new()
        {
            { CraftingTier.Minor, 0 },
            { CraftingTier.Lesser, 0 },
            { CraftingTier.Intermediate, 0 },
            { CraftingTier.Greater, 0 },
            { CraftingTier.Flawless, 0 },
            { CraftingTier.Perfect, 0 },
            { CraftingTier.Divine, 0 }
        };

        NwCreature? playerLoginCreature = player.LoginCreature;
        if (playerLoginCreature == null) return mythals;

        // Count loose mythals in inventory
        foreach (NwItem item in playerLoginCreature.Inventory.Items.Where(i => i.ResRef.StartsWith(value: "mythal")))
        {
            string resRef = item.ResRef;

            if (!mythalMap.TryGetValue(resRef, out CraftingTier tier)) continue;

            mythals[tier] += 1;
        }

        // Count mythals stored in Mythal Tubes
        Dictionary<CraftingTier, int> tubeMythals = GetMythalsFromTubes(player);
        foreach (CraftingTier tier in tubeMythals.Keys)
        {
            mythals[tier] += tubeMythals[tier];
        }

        return mythals;
    }

    /// <summary>
    /// Gets the count of loose mythals in the player's inventory (not in tubes).
    /// </summary>
    public static Dictionary<CraftingTier, int> GetLooseMythals(NwPlayer player)
    {
        Dictionary<string, CraftingTier> mythalMap = ResRefToTierMap();

        Dictionary<CraftingTier, int> mythals = new()
        {
            { CraftingTier.Minor, 0 },
            { CraftingTier.Lesser, 0 },
            { CraftingTier.Intermediate, 0 },
            { CraftingTier.Greater, 0 },
            { CraftingTier.Flawless, 0 },
            { CraftingTier.Perfect, 0 },
            { CraftingTier.Divine, 0 }
        };

        NwCreature? playerLoginCreature = player.LoginCreature;
        if (playerLoginCreature == null) return mythals;

        foreach (NwItem item in playerLoginCreature.Inventory.Items.Where(i => i.ResRef.StartsWith(value: "mythal")))
        {
            string resRef = item.ResRef;

            if (!mythalMap.TryGetValue(resRef, out CraftingTier tier)) continue;

            mythals[tier] += 1;
        }

        return mythals;
    }

    /// <summary>
    /// Gets the count of mythals stored in Mythal Tubes in the player's inventory.
    /// </summary>
    public static Dictionary<CraftingTier, int> GetMythalsFromTubes(NwPlayer player)
    {
        Dictionary<string, CraftingTier> mythalMap = ResRefToTierMap();

        Dictionary<CraftingTier, int> mythals = new()
        {
            { CraftingTier.Minor, 0 },
            { CraftingTier.Lesser, 0 },
            { CraftingTier.Intermediate, 0 },
            { CraftingTier.Greater, 0 },
            { CraftingTier.Flawless, 0 },
            { CraftingTier.Perfect, 0 },
            { CraftingTier.Divine, 0 }
        };

        NwCreature? playerLoginCreature = player.LoginCreature;
        if (playerLoginCreature == null) return mythals;

        // Find all Mythal Tubes
        IEnumerable<NwItem> tubes = playerLoginCreature.Inventory.Items
            .Where(i => i.ResRef == ItemTypeConstants.MythalTubeResRef);

        foreach (NwItem tube in tubes)
        {
            int itemCount = NWScript.GetLocalInt(tube, ItemTypeConstants.StorageItemCountVar);
            if (itemCount <= 0) continue;

            string storedItem = NWScript.GetLocalString(tube, ItemTypeConstants.StoredItemVar);
            if (string.IsNullOrEmpty(storedItem)) continue;

            if (!mythalMap.TryGetValue(storedItem, out CraftingTier tier)) continue;

            mythals[tier] += itemCount;
        }

        return mythals;
    }

    /// <summary>
    /// Gets all Mythal Tubes in the player's inventory that contain the specified mythal type.
    /// </summary>
    public static List<NwItem> GetTubesForTier(NwPlayer player, CraftingTier tier)
    {
        List<NwItem> result = [];

        NwCreature? playerLoginCreature = player.LoginCreature;
        if (playerLoginCreature == null) return result;

        string targetResRef = TierToResRef(tier);
        if (string.IsNullOrEmpty(targetResRef)) return result;

        IEnumerable<NwItem> tubes = playerLoginCreature.Inventory.Items
            .Where(i => i.ResRef == ItemTypeConstants.MythalTubeResRef);

        foreach (NwItem tube in tubes)
        {
            string storedItem = NWScript.GetLocalString(tube, ItemTypeConstants.StoredItemVar);
            if (storedItem == targetResRef)
            {
                result.Add(tube);
            }
        }

        return result;
    }

    /// <summary>
    /// Consumes the specified number of mythals from a Mythal Tube.
    /// Returns the number of mythals actually consumed.
    /// </summary>
    public static int ConsumeMythalsFromTube(NwItem tube, int amount)
    {
        int currentCount = NWScript.GetLocalInt(tube, ItemTypeConstants.StorageItemCountVar);
        if (currentCount <= 0) return 0;

        int toConsume = Math.Min(amount, currentCount);
        int newCount = currentCount - toConsume;

        if (newCount <= 0)
        {
            // Empty the tube - match NWScript behavior by resetting the tube
            // Reset name to "Mythal Storage Tube" (empty tube state)
            tube.Name = "Mythal Storage Tube";
            NWScript.DeleteLocalString(tube, ItemTypeConstants.StoredItemVar);
            NWScript.DeleteLocalInt(tube, ItemTypeConstants.StorageItemCountVar);
            // Reset description to empty
            tube.Description = "";
        }
        else
        {
            NWScript.SetLocalInt(tube, ItemTypeConstants.StorageItemCountVar, newCount);
            // Update the tube name to reflect new count
            string storedItem = NWScript.GetLocalString(tube, ItemTypeConstants.StoredItemVar);
            string mythalName = GetMythalDisplayName(storedItem);
            tube.Name = $"Mythal Storage Tube ({mythalName})";
            tube.Description = $"Number of stored items: {newCount}";
        }

        return toConsume;
    }

    /// <summary>
    /// Gets a display name for a mythal resref.
    /// </summary>
    private static string GetMythalDisplayName(string resRef)
    {
        return resRef switch
        {
            "mythal1" => "Minor Mythal",
            "mythal2" => "Lesser Mythal",
            "mythal3" => "Intermediate Mythal",
            "mythal4" => "Greater Mythal",
            "mythal5" => "Flawless Mythal",
            "mythal6" => "Perfect Mythal",
            "mythal7" => "Divine Mythal",
            _ => "Unknown Mythal"
        };
    }

    /// <summary>
    /// Maps mythal resrefs to crafting tiers.
    /// </summary>
    private static Dictionary<string, CraftingTier> ResRefToTierMap() => new()
    {
        { "mythal1", CraftingTier.Minor },
        { "mythal2", CraftingTier.Lesser },
        { "mythal3", CraftingTier.Intermediate },
        { "mythal4", CraftingTier.Greater },
        { "mythal5", CraftingTier.Flawless },
        { "mythal6", CraftingTier.Perfect },
        { "mythal7", CraftingTier.Divine }
    };

    public static string TierToResRef(CraftingTier tier)
    {
        Dictionary<CraftingTier, string> tierMap = new()
        {
            { CraftingTier.Minor, "mythal1" },
            { CraftingTier.Lesser, "mythal2" },
            { CraftingTier.Intermediate, "mythal3" },
            { CraftingTier.Greater, "mythal4" },
            { CraftingTier.Flawless, "mythal5" },
            { CraftingTier.Perfect, "mythal6" },
            { CraftingTier.Divine, "mythal7" }
        };

        tierMap.TryGetValue(tier, out string? t);

        return t ?? "";
    }

    public static bool PropertiesAreSame(ItemProperty property1, ItemProperty property2)
    {
        string label1 = FullPropertyDescription(property1);
        string label2 = FullPropertyDescription(property2);
        bool propertiesAreSame = label1 == label2;
        if (property1.Property.PropertyType == ItemPropertyType.OnHitProperties &&
            property2.Property.PropertyType == ItemPropertyType.OnHitProperties
            || property1.Property.PropertyType == ItemPropertyType.OnHitCastSpell &&
            property2.Property.PropertyType == ItemPropertyType.OnHitCastSpell)
        {
            LogManager.GetCurrentClassLogger().Info(message: "Comparing OnHit properties.");
            NwArea? systemArea = NwModule.Instance.Areas.FirstOrDefault(a => a.Name.Contains(value: "Area to Rest"));

            if (systemArea == null)
            {
                LogManager.GetCurrentClassLogger().Info(message: "System area not found.");
                return false;
            }

            Location? arbitraryWaypoint = systemArea.FindObjectsOfTypeInArea<NwWaypoint>().First().Location;

            if (arbitraryWaypoint == null)
            {
                LogManager.GetCurrentClassLogger().Info(message: "Arbitrary waypoint not found.");
                return false;
            }

            NwItem dummy = NwItem.Create(template: "nw_wswls001", arbitraryWaypoint);
            if (dummy == null)
            {
                // log this error
                LogManager.GetCurrentClassLogger().Info(message: "Dummy item not created.");
                return false;
            }

            if (!property1.Valid || !property2.Valid)
            {
                LogManager.GetCurrentClassLogger()
                    .Info(message:
                        "An item property was not valid. Check the definitions in PropertyConstants for errors.");
                return false;
            }

            dummy.AddItemProperty(property1, EffectDuration.Permanent);
            dummy.AddItemProperty(property2, EffectDuration.Permanent);
            ItemProperty[] properties = dummy.ItemProperties.ToArray();
            LogManager.GetCurrentClassLogger().Info($"Comparing {properties.Length} properties.");

            // Uses regex to remove the +X% from the labels for comparison purposes
            string l1 = Regex.Replace(GameLabel(properties[0]), pattern: @"\d+%", string.Empty).TrimEnd();
            string l2 = Regex.Replace(GameLabel(properties[1]), pattern: @"\d+%", string.Empty).TrimEnd();
            propertiesAreSame = l1 == l2;
            LogManager.GetCurrentClassLogger().Info($"Properties are the same: {propertiesAreSame}. {l1} == {l2}");
            dummy.Destroy();
        }


        return propertiesAreSame;
    }

    public static string FullPropertyDescription(ItemProperty property)
    {
        StringBuilder description = new(value: "");
        if (property.Property.GameStrRef == null) return description.ToString();

        description.Append(property.Property.GameStrRef.ToString());

        ItemPropertySubTypeTableEntry? subType = property.SubType;
        if (subType != null)
        {
            description.Append($" {subType.Name}");
        }

        ItemPropertyParamTableEntry? param1Value = property.Param1TableValue;
        ItemPropertyCostTableEntry? costTableValue = property.CostTableValue;

        if (param1Value != null || costTableValue != null)
        {
            if (costTableValue != null)
            {
                description.Append(' ');
                description.Append(costTableValue.Name);
            }

            if (param1Value != null)
            {
                description.Append(' ');
                description.Append(param1Value.Name);
            }
        }

        return description.ToString();
    }
}
