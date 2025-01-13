using AmiaReforged.PwEngine.Systems.Crafting;
using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.NwObjectHelpers;

public static class ItemPropertyHelper
{
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

    public static CraftingProperty ToCraftingProperty(ItemProperty propertyItemProperty)
    {
        return new CraftingProperty
        {
            ItemProperty = propertyItemProperty,
            GuiLabel = GameLabel(propertyItemProperty),
            PowerCost = 2,
            CraftingTier = CraftingTier.DreamCoin
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

    public static Dictionary<string, MythalEntry> GetMythals(NwPlayer player)
    {
        List<string> mythals = new()
        {
            "mythal1",
            "mythal2",
            "mythal3",
            "mythal4",
            "mythal5",
            "mythal6",
            "mythal7",
        };

        Dictionary<string, MythalEntry> mythalTierMap = new()
        {
            { "mythal1", new MythalEntry { Amount = 0, Tier = CraftingTier.Minor, ResRef = "mythal1" } },
            { "mythal2", new MythalEntry { Amount = 0, Tier = CraftingTier.Lesser, ResRef = "mythal2" } },
            { "mythal3", new MythalEntry { Amount = 0, Tier = CraftingTier.Intermediate, ResRef = "mythal3" } },
            { "mythal4", new MythalEntry { Amount = 0, Tier = CraftingTier.Greater, ResRef = "mythal4" } },
            { "mythal5", new MythalEntry { Amount = 0, Tier = CraftingTier.Flawless, ResRef = "mythal5" } },
            { "mythal6", new MythalEntry { Amount = 0, Tier = CraftingTier.Perfect, ResRef = "mythal6" } },
            { "mythal7", new MythalEntry { Amount = 0, Tier = CraftingTier.Divine, ResRef = "mythal7" } }
        };

        NwCreature? playerLoginCreature = player.LoginCreature;
        if (playerLoginCreature == null) return mythalTierMap;

        foreach (NwItem item in playerLoginCreature.Inventory.Items.Where(i => i.ResRef.StartsWith("mythal")))
        {
            string resRef = item.ResRef;
            if (mythals.Contains(resRef))
            {
                mythalTierMap[resRef].Amount += 1;
            }
        }

        return mythalTierMap;
    }
}