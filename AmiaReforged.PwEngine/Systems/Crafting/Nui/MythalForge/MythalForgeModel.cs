﻿using AmiaReforged.PwEngine.Systems.Crafting.Models;
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
    private readonly NwPlayer _player;

    public MythalForgeModel(NwItem item, CraftingPropertyData data, CraftingBudgetService budget, NwPlayer player)
    {
        Item = item;
        _budget = budget;
        _player = player;

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
        if (ChangeListModel.ChangeList().Count == 0)
        {
            return 0;
        }

        int craftingDifficulty = MythalCategoryModel.Categories.Select(c => c.BaseDifficulty).Max() *
                                 ChangeListModel.ChangeList().Select(c => c.Property.PowerCost).Max();
        return craftingDifficulty;
    }

    public void AddNewProperty(CraftingProperty property)
    {
        ChangeListModel.AddNewProperty(property);
        MythalCategoryModel.ConsumeMythal(property.CraftingTier);
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

        MythalCategoryModel.DestroyMythals(_player);
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
                                      property.InternalProperty.PowerCost <= RemainingPowers &&
                                      MythalCategoryModel.HasMythals(property.InternalProperty.CraftingTier);
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

    private int GetSkill()
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
        return NWScript.GetSkillRank(GetSkill(), _player.LoginCreature) >= GetCraftingDifficulty();
    }

    public void RemoveActiveProperty(CraftingProperty property)
    {
        ActivePropertiesModel.HideProperty(property);
        ChangeListModel.AddRemovedProperty(property);
    }

    public void RevealProperty(CraftingProperty property)
    {
        ActivePropertiesModel.RevealProperty(property);
    }

    public void UndoAllChanges()
    {
        ChangeListModel.UndoAllChanges();
        ActivePropertiesModel.UndoAllChanges();
    }

    public void UndoRemoval(CraftingProperty property)
    {
        ChangeListModel.UndoRemoval(property);
    }

    public void UndoAddition(CraftingProperty property)
    {
        ChangeListModel.UndoAddition(property);
        MythalCategoryModel.RefundMythal(property.CraftingTier);
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