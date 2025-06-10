using System.Text;
using System.Text.RegularExpressions;
using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Systems.NwObjectHelpers;

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

    public static CraftingProperty ToCraftingProperty(ItemProperty ip)
    {
        string gameLabel = GameLabel(ip);

        gameLabel = gameLabel.Replace(oldValue: "_", newValue: " ");

        return new()
        {
            ItemProperty = ip,
            GuiLabel = gameLabel,
            PowerCost = GetPowerCost(ip),
            CraftingTier = CraftingTier.Wondrous,
            Removable = CanBeRemoved(ip)
        };
    }

    private static int GetPowerCost(ItemProperty ip)
    {
        List<ItemPropertyType> noCost = new()
        {
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
            ItemPropertyType.NoDamage
        };

        return noCost.Any(it => it == ip.Property.PropertyType) ? 0 : 2;
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
        Dictionary<string, CraftingTier> mythalMap = new()
        {
            { "mythal1", CraftingTier.Minor },
            { "mythal2", CraftingTier.Lesser },
            { "mythal3", CraftingTier.Intermediate },
            { "mythal4", CraftingTier.Greater },
            { "mythal5", CraftingTier.Flawless },
            { "mythal6", CraftingTier.Perfect },
            { "mythal7", CraftingTier.Divine }
        };

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
        string label1 = GameLabel(property1);
        string label2 = GameLabel(property2);
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