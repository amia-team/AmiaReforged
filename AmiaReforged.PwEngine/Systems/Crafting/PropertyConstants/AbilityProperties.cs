using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

public static class AbilityProperties
{
    private const int MythalCostAbility1 = 2000;
    private const int MythalCostAbility2 = 10000;
    private const int MythalCostAbility3 = 30000;

    public static readonly CraftingCategory Abilities = new("ability_selection")
    {
        Label = "Ability Bonus",
        Properties = new[]
        {
            // Intermediate
            new CraftingProperty()
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_STR, 1)!,
                GuiLabel = "+1 Strength",
                GoldCost = MythalCostAbility1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty()
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_DEX, 1)!,
                GuiLabel = "+1 Dexterity",
                GoldCost = MythalCostAbility1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty()
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CON, 1)!,
                GuiLabel = "+1 Constitution",
                GoldCost = MythalCostAbility1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty()
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_INT, 1)!,
                GuiLabel = "+1 Intelligence",
                GoldCost = MythalCostAbility1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty()
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_WIS, 1)!,
                GuiLabel = "+1 Wisdom",
                GoldCost = MythalCostAbility1,
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty()
            {
                PowerCost = 2,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CHA, 1)!,
                GuiLabel = "+1 Charisma",
                GoldCost = MythalCostAbility1,
                CraftingTier = CraftingTier.Intermediate
            },
            // Greater
            new CraftingProperty()
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_STR, 2)!,
                GuiLabel = "+2 Strength",
                GoldCost = MythalCostAbility2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty()
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_DEX, 2)!,
                GuiLabel = "+2 Dexterity",
                GoldCost = MythalCostAbility2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty()
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CON, 2)!,
                GuiLabel = "+2 Constitution",
                GoldCost = MythalCostAbility2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty()
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_INT, 2)!,
                GuiLabel = "+2 Intelligence",
                GoldCost = MythalCostAbility2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty()
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_WIS, 2)!,
                GuiLabel = "+2 Wisdom",
                GoldCost = MythalCostAbility2,
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty()
            {
                PowerCost = 4,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CHA, 2)!,
                GuiLabel = "+2 Charisma",
                GoldCost = MythalCostAbility2,
                CraftingTier = CraftingTier.Greater
            },
            // Flawless
            new CraftingProperty()
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_STR, 3)!,
                GuiLabel = "+3 Strength",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty()
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_DEX, 3)!,
                GuiLabel = "+3 Dexterity",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty()
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CON, 3)!,
                GuiLabel = "+3 Constitution",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty()
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_INT, 3)!,
                GuiLabel = "+3 Intelligence",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty()
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_WIS, 3)!,
                GuiLabel = "+3 Wisdom",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Flawless
            },
            new CraftingProperty()
            {
                PowerCost = 6,
                ItemProperty = NWScript.ItemPropertyAbilityBonus(NWScript.IP_CONST_ABILITY_CHA, 3)!,
                GuiLabel = "+3 Charisma",
                GoldCost = MythalCostAbility3,
                CraftingTier = CraftingTier.Flawless
            }
        },
        BaseDifficulty = 5,
        PerformValidation = (c, i, l) =>
        {
            PropertyValidationResult result = PropertyValidationResult.Valid;
            if (c.ItemProperty.Property.PropertyType != ItemPropertyType.AbilityBonus) return result;

            ItemPropertyModel incomingProperty = c;

            foreach (ChangeListModel.ChangelistEntry entry in l)
            {
                ItemPropertyModel entryProperty = entry.Property;

                if (HasSameAbilityType(incomingProperty, entryProperty)) continue;

                result = PropertyValidationResult.CannotStackSameSubtype;
                break;
            }

            foreach (ItemProperty p in i.ItemProperties)
            {
                ItemPropertyModel itemPropertyEntry = new()
                {
                    Property = p,
                    GoldCost = 0, // We don't care about it here.
                };

                if (!HasSameAbilityType(incomingProperty, itemPropertyEntry)) continue;
                result = PropertyValidationResult.CannotStackSameSubtype;
                break;
            }

            return result;
            
            bool HasSameAbilityType(ItemPropertyModel model, ItemPropertyModel model2)
            {
                string modelLabel = model.Label;
                string trimmedModel = modelLabel.Replace("Enhancement Bonus: ", "");
                string modelAbilityType = trimmedModel.Split(" ")[0];
                
                string model2Label = model2.Label;
                string trimmedModel2 = model2Label.Replace("Enhancement Bonus: ", "");
                string model2AbilityType = trimmedModel2.Split(" ")[0];
                
                return modelAbilityType == model2AbilityType;
            }
        }
    };
}