using AmiaReforged.PwEngine.Systems.Crafting;
using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;
using Anvil.API;
using Anvil.Services;
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
        if (subType != null)
        {
            label += " " + subType.Label;
        }

        ItemPropertyParamTableEntry? param1Value = property.Param1TableValue;
        ItemPropertyCostTableEntry? costTableValue = property.CostTableValue;


        if (param1Value != null || costTableValue != null)
        {
            if (costTableValue != null)
            {
                label += " " + costTableValue.Label;
            }

            if (param1Value != null)
            {
                label += " " + param1Value.Label;
            }
        }

        return label;
    }

    public static CraftingProperty ToCraftingProperty(ItemProperty ip)
    {
        return new CraftingProperty
        {
            ItemProperty = ip,
            GuiLabel = GameLabel(ip),
            PowerCost = 2,
            CraftingTier = CraftingTier.DreamCoin,
            Removable = CanBeRemoved(ip)
        };
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
            { "mythal7", CraftingTier.Divine },
        };

        Dictionary<CraftingTier, int> mythals = new()
        {
            { CraftingTier.Minor, 0 },
            { CraftingTier.Lesser, 0 },
            { CraftingTier.Intermediate, 0 },
            { CraftingTier.Greater, 0 },
            { CraftingTier.Flawless, 0 },
            { CraftingTier.Perfect, 0 },
            { CraftingTier.Divine, 0 },
        };

        NwCreature? playerLoginCreature = player.LoginCreature;
        if (playerLoginCreature == null) return mythals;

        foreach (NwItem item in playerLoginCreature.Inventory.Items.Where(i => i.ResRef.StartsWith("mythal")))
        {
            string resRef = item.ResRef;

            Log.Info("Item: " + resRef);

            if (!mythalMap.TryGetValue(resRef, out CraftingTier tier)) continue;

            Log.Info("Tier: " + tier);
            mythals[tier] += 1;
        }

        return mythals;
    }
    
    public static bool PropertiesAreSame(ItemProperty property1, ItemProperty property2)
    {
        return property1.Property == property2.Property &&
               property1.SubType == property2.SubType &&
               property1.CostTableValue == property2.CostTableValue &&
               property1.Param1TableValue == property2.Param1TableValue;
    }
}