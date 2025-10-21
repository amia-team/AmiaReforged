using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.Crafting.Models.PropertyValidationRules;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ActiveProperties;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ChangeList;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.MythalCategory;
using AmiaReforged.PwEngine.Features.NwObjectHelpers;
using Anvil.API;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;

public class MythalForgeModel
{
    private readonly CraftingBudgetService _budget;
    private readonly DifficultyClassCalculator _dcCalculator;
    private readonly NwPlayer _player;
    private readonly PropertyValidator _validator;

    public MythalForgeModel(NwItem item, CraftingPropertyData data, CraftingBudgetService budget, NwPlayer player,
        PropertyValidator validator, DifficultyClassCalculator dcCalculator)
    {
        Item = item;
        _budget = budget;
        _player = player;
        _validator = validator;
        _dcCalculator = dcCalculator;

        int baseType = NWScript.GetBaseItemType(item);

        // Is it a caster weapon?
        bool casterWeapon = NWScript.GetLocalInt(item, ItemTypeConstants.CasterWeaponVar) == NWScript.TRUE;
        if (casterWeapon)
            baseType = ItemTypeConstants.Melee2HWeapons().Contains(baseType)
                ? CraftingPropertyData.CasterWeapon2H
                : CraftingPropertyData.CasterWeapon1H;

        LogManager.GetCurrentClassLogger().Info("Base type: " + baseType);

        IReadOnlyList<CraftingCategory> categories = data.Properties[baseType];

        MythalCategoryModel = new MythalCategoryModel(item, player, categories);
        ChangeListModel = new ChangeListModel();
        ActivePropertiesModel = new ActivePropertiesModel(item, categories);
    }

    public ChangeListModel ChangeListModel { get; }
    public MythalCategoryModel MythalCategoryModel { get; }
    public ActivePropertiesModel ActivePropertiesModel { get; }


    public NwItem Item { get; }

    public int MaxBudget => _budget.MythalBudgetForNwItem(Item);

    public int RemainingPowers
    {
        get
        {
            int remaining = MaxBudget;

            foreach (MythalCategoryModel.MythalProperty visibleProperty in ActivePropertiesModel.GetVisibleProperties())
            {
                remaining -= visibleProperty.Internal.PowerCost;
            }

            foreach (ChangeListModel.ChangelistEntry entry in ChangeListModel.ChangeList())
            {
                if (entry.State == ChangeListModel.ChangeState.Added) remaining -= entry.Property.PowerCost;
            }

            return Math.Clamp(remaining, -16, MaxBudget);
        }
    }

    public IEnumerable<MythalCategoryModel.MythalProperty> VisibleProperties =>
        ActivePropertiesModel.GetVisibleProperties();

    public int GetCraftingDifficulty()
    {
        if (ChangeListModel.ChangeList().Count == 0) return 0;

        int craftingDifficulty = 0;

        List<CraftingProperty> changelistProperties =
            ChangeListModel.ChangeList()
                .Where(e => e.State != ChangeListModel.ChangeState.Removed)
                .Select(p => p.Property)
                .ToList();


        foreach (CraftingProperty craftingProperty in changelistProperties)
        {
            int newDifficulty = _dcCalculator.ComputeDifficulty(craftingProperty);
        }

        return craftingDifficulty;
    }

    public void AddNewProperty(MythalCategoryModel.MythalProperty property)
    {
        ChangeListModel.AddNewProperty(property);
        MythalCategoryModel.ConsumeMythal(property.Internal.CraftingTier);
    }

    public void ApplyChanges()
    {
        bool failed = false;
        List<ItemProperty> propertiesToRemove = new();
        foreach (ChangeListModel.ChangelistEntry change in ChangeListModel.ChangeList())
        {
            if (change.State != ChangeListModel.ChangeState.Removed) continue;
            ItemProperty? identical =
                Item.ItemProperties.FirstOrDefault(p => ItemPropertyHelper.PropertiesAreSame(p, change.Property));
            if (identical == null)
            {
                failed = true;
                break;
            }

            propertiesToRemove.Add(identical);
        }

        if (failed)
        {
            _player.SendServerMessage(
                "For some reason, we couldn't find the properties to remove. Please try again."
                + " If the problem persists, contact the team on Discord.",
                ColorConstants.Red);
            return;
        }

        // Only remove properties after we've checked that we can remove them all.
        // Invalid or null properties means that the "transaction" failed.
        foreach (ItemProperty property in propertiesToRemove)
        {
            Item.RemoveItemProperty(property);
        }

        // Handle additions.
        foreach (ChangeListModel.ChangelistEntry change in ChangeListModel.ChangeList())
        {
            if (change.State == ChangeListModel.ChangeState.Added)
                Item.AddItemProperty(change.Property, EffectDuration.Permanent);
        }

        int goldCost = ChangeListModel.TotalGpCost();
        _player.LoginCreature?.TakeGold(goldCost);

        MythalCategoryModel.DestroyMythals(_player);
    }

    public void RefreshCategories()
    {
        // Compute once and reuse within this refresh.
        int remainingPowers = RemainingPowers;

        // Cache current state to avoid repeated allocations/enumerations in loops.
        IReadOnlyList<ItemProperty> currentItemProps = Item.ItemProperties.ToList();
        List<ChangeListModel.ChangelistEntry> currentChangeList = ChangeListModel.ChangeList();

        MythalCategoryModel.UpdateFromRemainingBudget(remainingPowers);

        foreach (MythalCategoryModel.MythalCategory category in MythalCategoryModel.Categories)
        {
            foreach (MythalCategoryModel.MythalProperty property in category.Properties)
            {
                // Cheap pre-checks to skip expensive validation
                bool hasTheMythals = MythalCategoryModel.HasMythals(property.Internal.CraftingTier);
                bool canAfford = property.Internal.PowerCost <= remainingPowers || property.Internal.PowerCost == 0;

                if (!hasTheMythals || !canAfford)
                {
                    property.Selectable = false;
                    property.CostLabelTooltip = !hasTheMythals
                        ? "Not enough mythals."
                        : "Not enough points left.";
                    continue;
                }

                // Validate only when affordable and mythals available
                ValidationResult operation =
                    _validator.Validate(property, currentItemProps, currentChangeList);

                bool passesValidation = operation.Result == ValidationEnum.Valid;

                property.Selectable = passesValidation;
                property.CostLabelTooltip = passesValidation
                    ? string.Empty
                    : operation.ErrorMessage ?? "Validation failed.";
            }
        }
    }

    public string SkillToolTip()
    {
        int baseType = NWScript.GetBaseItemType(Item);
        string tooltip = "Skill: ";
        if (ItemTypeConstants.EquippableItems().Contains(baseType))
            tooltip += baseType is NWScript.BASE_ITEM_ARMOR or NWScript.BASE_ITEM_SMALLSHIELD
                or NWScript.BASE_ITEM_LARGESHIELD or NWScript.BASE_ITEM_TOWERSHIELD or NWScript.BASE_ITEM_GLOVES
                or NWScript.BASE_ITEM_BRACER or NWScript.BASE_ITEM_BELT
                ? "Craft Armor"
                : "Spellcraft";

        if (ItemTypeConstants.RangedWeapons().Contains(baseType) ||
            ItemTypeConstants.ThrownWeapons().Contains(baseType) ||
            ItemTypeConstants.Ammo().Contains(baseType) ||
            ItemTypeConstants.Melee2HWeapons().Contains(baseType) ||
            ItemTypeConstants.MeleeWeapons().Contains(baseType))
            tooltip += "Craft Weapon";

        bool canCraft = CanMakeCheck();
        if (!canCraft) tooltip += " (You don't have the required skill rank)";

        return tooltip;
    }

    private int GetSkill()
    {
        int baseType = NWScript.GetBaseItemType(Item);
        if (ItemTypeConstants.EquippableItems().Contains(baseType))
            return baseType is NWScript.BASE_ITEM_ARMOR or NWScript.BASE_ITEM_SMALLSHIELD
                or NWScript.BASE_ITEM_LARGESHIELD or NWScript.BASE_ITEM_TOWERSHIELD or NWScript.BASE_ITEM_GLOVES
                or NWScript.BASE_ITEM_BRACER or NWScript.BASE_ITEM_BELT
                ? NWScript.SKILL_CRAFT_ARMOR
                : NWScript.SKILL_SPELLCRAFT;

        if (ItemTypeConstants.RangedWeapons().Contains(baseType)) return NWScript.SKILL_CRAFT_WEAPON;

        if (ItemTypeConstants.ThrownWeapons().Contains(baseType)) return NWScript.SKILL_CRAFT_WEAPON;

        if (ItemTypeConstants.Ammo().Contains(baseType)) return NWScript.SKILL_CRAFT_WEAPON;

        // Default fall back value
        return NWScript.SKILL_SPELLCRAFT;
    }

        public bool CanMakeCheck() => NWScript.GetSkillRank(GetSkill(), _player.LoginCreature) + 20 >= GetCraftingDifficulty();

    public void RemoveActiveProperty(CraftingProperty property)
    {
        ActivePropertiesModel.HideProperty(property);
        ChangeListModel.AddRemovedProperty(property);
    }

    public void UndoAddition(CraftingProperty property)
    {
        ChangeListModel.UndoAddition(property);
        MythalCategoryModel.RefundMythal(property.CraftingTier);
    }

    public void UndoRemoval(CraftingProperty property)
    {
        List<ChangeListModel.ChangelistEntry> additions;
        ItemPropertyType baseType = property.ItemProperty.Property.PropertyType;

        // Quick workaround for dealing with saving throws....
        // We should eventually switch this to a better model that allows for specifying maximum bonuses....
        if (baseType is ItemPropertyType.SavingThrowBonus or ItemPropertyType.SavingThrowBonusSpecific)
        {
            additions = ChangeListModel.ChangeList().Where(e =>
                    e.State != ChangeListModel.ChangeState.Removed && e.Property.ItemProperty.Property.PropertyType ==
                    baseType || e.Property.ItemProperty.Property.PropertyType ==
                    ItemPropertyType.SavingThrowBonusSpecific)
                .ToList();
        }
        else
        {
            additions = ChangeListModel.ChangeList().Where(e =>
                e.State != ChangeListModel.ChangeState.Removed && e.Property.ItemProperty.Property.PropertyType ==
                baseType).ToList();
        }

        ChangeListModel.UndoRemoval(property);
        ActivePropertiesModel.RevealProperty(property);

        foreach (ChangeListModel.ChangelistEntry entry in additions)
        {
            ValidationResult operation =
                _validator.Validate(entry.Property, Item.ItemProperties, ChangeListModel.ChangeList());

            bool passesValidation = operation.Result == ValidationEnum.Valid;

            if (!passesValidation) UndoAddition(entry.Property);
        }
    }

    public ValidationResult ValidateSingle(MythalCategoryModel.MythalProperty property,
        IReadOnlyList<ItemProperty> currentItemProps,
        List<ChangeListModel.ChangelistEntry> changeList)
    {
        return _validator.Validate(property, currentItemProps, changeList);
    }
}
