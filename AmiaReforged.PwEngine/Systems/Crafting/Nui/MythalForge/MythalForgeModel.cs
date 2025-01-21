using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ActiveProperties;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.MythalCategory;
using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using Anvil.API;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public class MythalForgeModel
{
    public ChangeListModel ChangeListModel { get; }
    public MythalCategoryModel MythalCategoryModel { get; }
    public ActivePropertiesModel ActivePropertiesModel { get; }

    public NwItem Item { get; }
    private readonly CraftingBudgetService _budget;

    public MythalForgeModel(NwItem item, CraftingPropertyData data, CraftingBudgetService budget, NwPlayer player)
    {
        Item = item;
        _budget = budget;

        int baseType = NWScript.GetBaseItemType(item);
        IReadOnlyList<CraftingCategory> categories = data.Properties[baseType];

        MythalCategoryModel = new MythalCategoryModel(item, player, categories);
        ChangeListModel = new ChangeListModel();
        ActivePropertiesModel = new ActivePropertiesModel(item, player, categories);
    }

    public int MaxBudget => _budget.MythalBudgetForNwItem(Item);

    public int RemainingPowers
    {
        get
        {
            int remaining = MaxBudget;

            foreach (MythalCategoryModel.MythalProperty visibleProperty in ActivePropertiesModel.GetVisibleProperties())
            {
                remaining -= visibleProperty.InternalProperty.PowerCost;
            }

            foreach (ChangeListModel.ChangelistEntry entry in ChangeListModel.ChangeList())
            {
                if (entry.State == ChangeListModel.ChangeState.Added)
                {
                    remaining -= entry.Property.PowerCost;
                }
            }

            return Math.Clamp(remaining, -16, MaxBudget);
        }
    }

    public IEnumerable<MythalCategoryModel.MythalProperty> VisibleProperties =>
        ActivePropertiesModel.GetVisibleProperties();

    public int GetCraftingDifficulty()
    {
        return MythalCategoryModel.Categories.Select(c => c.BaseDifficulty).Max() *
               ChangeListModel.ChangeList().Select(c => c.Property.PowerCost).Max();
    }

    public void TryAddProperty(CraftingProperty property)
    {
        ChangeListModel.AddNewProperty(property);
    }

    private bool PropertyIsInvalid(CraftingProperty property)
    {
        return property.PowerCost > RemainingPowers ||
               Item.ItemProperties.Any(c => ItemPropertyHelper.GameLabel(c) == property.GameLabel);
    }

    public void ApplyChanges()
    {
        foreach (ChangeListModel.ChangelistEntry change in ChangeListModel.ChangeList())
        {
            if (change.State == ChangeListModel.ChangeState.Added)
            {
                Item.AddItemProperty(change.Property, EffectDuration.Permanent);
            }
            else if (change.State == ChangeListModel.ChangeState.Removed)
            {
                Item.RemoveItemProperty(change.Property.ItemProperty);
            }
        }
    }

    public void RefreshCategories()
    {
        MythalCategoryModel.UpdateFromRemainingBudget(RemainingPowers);

        foreach (MythalCategoryModel.MythalCategory category in MythalCategoryModel.Categories)
        {
            foreach (MythalCategoryModel.MythalProperty property in category.Properties)
            {
                PropertyValidationResult validationResult = PropertyValidationResult.Valid;
                if (category.PerformValidation != null)
                {
                    validationResult = category.PerformValidation(property, Item);
                }

                property.Selectable = !ActivePropertiesModel.PropertyExistsOnItem(property) &&
                                      validationResult == PropertyValidationResult.Valid &&
                                      property.InternalProperty.PowerCost <= RemainingPowers;
            }
        }
    }

    public string GetSkillName()
    {
        int baseType = NWScript.GetBaseItemType(Item);
        if (ItemTypeConstants.EquippableItems().Contains(baseType))
        {
            return baseType is NWScript.BASE_ITEM_ARMOR or NWScript.BASE_ITEM_SMALLSHIELD
                or NWScript.BASE_ITEM_LARGESHIELD or NWScript.BASE_ITEM_TOWERSHIELD or NWScript.BASE_ITEM_GLOVES
                or NWScript.BASE_ITEM_BRACER or NWScript.BASE_ITEM_BELT
                ? "Craft Armor"
                : "Spellcraft";
        }

        if (ItemTypeConstants.RangedWeapons().Contains(baseType))
        {
            return "Craft Weapon";
        }

        if (ItemTypeConstants.ThrownWeapons().Contains(baseType))
        {
            return "Craft Weapon";
        }

        if (ItemTypeConstants.Ammo().Contains(baseType))
        {
            return "Craft Weapon";
        }

        return "Spellcraft";
    }

    public int GetSkill()
    {
        int baseType = NWScript.GetBaseItemType(Item);
        if (ItemTypeConstants.EquippableItems().Contains(baseType))
        {
            return baseType is NWScript.BASE_ITEM_ARMOR or NWScript.BASE_ITEM_SMALLSHIELD
                or NWScript.BASE_ITEM_LARGESHIELD or NWScript.BASE_ITEM_TOWERSHIELD or NWScript.BASE_ITEM_GLOVES
                or NWScript.BASE_ITEM_BRACER or NWScript.BASE_ITEM_BELT
                ? NWScript.SKILL_CRAFT_ARMOR
                : NWScript.SKILL_SPELLCRAFT;
        }

        if (ItemTypeConstants.RangedWeapons().Contains(baseType))
        {
            return NWScript.SKILL_CRAFT_WEAPON;
        }

        if (ItemTypeConstants.ThrownWeapons().Contains(baseType))
        {
            return NWScript.SKILL_CRAFT_WEAPON;
        }

        if (ItemTypeConstants.Ammo().Contains(baseType))
        {
            return NWScript.SKILL_CRAFT_WEAPON;
        }

        // Default fall back value
        return NWScript.SKILL_SPELLCRAFT;
    }
    
    public bool CanMakeCheck()
    {
        return NWScript.GetSkillRank(GetSkill(), NWScript.OBJECT_SELF) >= GetCraftingDifficulty();
    }
}

public class MythalMap
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public Dictionary<CraftingTier, int> Map { get; }

    public MythalMap(NwPlayer player)
    {
        Log.Info("Getting mythals for player.");
        Map = ItemPropertyHelper.GetMythals(player);
    }
}