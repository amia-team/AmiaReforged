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

/// <summary>
/// Represents the model for the Mythal Forge crafting system, handling the data and logic
/// required to interact with the crafting UI. The model manages current crafting properties,
/// categories, changes, and validations while adhering to the provided crafting budget.
/// </summary>
public class MythalForgeModel
{
    /// <summary>
    /// Represents the crafting budget service utilized for managing and calculating
    /// the mythal budget associated with item crafting operations.
    /// </summary>
    private readonly CraftingBudgetService _budget;

    /// <summary>
    /// Represents a private instance of the <see cref="DifficultyClassCalculator"/> used to
    /// calculate the difficulty class for crafting operations in the Mythal Forge feature.
    /// </summary>
    private readonly DifficultyClassCalculator _dcCalculator;

    /// <summary>
    /// Represents the player associated with the current Mythal Forge crafting process.
    /// This player interacts with the interface, performs crafting actions, and
    /// executes any validation or property changes on the item being crafted.
    /// </summary>
    private readonly NwPlayer _player;

    /// <summary>
    /// A private, readonly field utilized for validating property-related operations within the <see cref="MythalForgeModel"/> class.
    /// </summary>
    /// <remarks>
    /// The <c>_validator</c> is an instance of the <see cref="PropertyValidator"/> class, allowing the validation of crafting properties
    /// against the current item properties and change list. It ensures that property additions, removals, and modifications
    /// adhere to the defined rules and constraints, such as power costs, crafting tiers, and other specific validation checks.
    /// </remarks>
    private readonly PropertyValidator _validator;

    /// <summary>
    /// Represents the core model for the Mythal Forge crafting interface.
    /// This class is responsible for managing and organizing crafting data and behavior in the context of the Mythal Forge system.
    /// </summary>
    /// <remarks>
    /// The <see cref="MythalForgeModel"/> class initializes and holds references to various related crafting models including:
    /// <list type="bullet">
    /// <item><description><see cref="MythalCategoryModel"/>: The data structure for categorized crafting properties.</description></item>
    /// <item><description><see cref="ChangeListModel"/>: Tracks changes made to crafting properties during modification.</description></item>
    /// <item><description><see cref="ActivePropertiesModel"/>: Manages the active crafting properties for the given item.</description></item>
    /// </list>
    /// It utilizes services such as the <see cref="CraftingBudgetService"/> for budget calculations, <see cref="PropertyValidator"/> for property validation, and <see cref="DifficultyClassCalculator"/> for managing difficulty class logic.
    /// This model also determines whether an item qualifies as a caster weapon and adjusts its crafting logic accordingly.
    /// </remarks>
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

    /// Represents the model responsible for managing a list of changes to be applied within the Mythal Forge system.
    /// This includes tracking added and removed properties, calculating costs, and undoing changes.
    public ChangeListModel ChangeListModel { get; }

    /// <summary>
    /// Gets whether the item can be converted to a caster weapon.
    /// </summary>
    /// <remarks>
    /// An item can be converted if:
    /// - It is a melee weapon (1H or 2H)
    /// - It is NOT already a caster weapon
    /// - It is NOT a magic staff (which is always treated as a caster weapon)
    /// </remarks>
    public bool CanConvertToCasterWeapon
    {
        get
        {
            int baseType = NWScript.GetBaseItemType(Item);
            
            // Magic staffs are always caster weapons, can't convert
            if (baseType == NWScript.BASE_ITEM_MAGICSTAFF)
                return false;
            
            // Already a caster weapon
            if (NWScript.GetLocalInt(Item, ItemTypeConstants.CasterWeaponVar) == NWScript.TRUE)
                return false;
            
            // Must be a melee weapon (1H or 2H)
            bool isMeleeWeapon = ItemTypeConstants.MeleeWeapons().Contains(baseType) ||
                                 ItemTypeConstants.Melee2HWeapons().Contains(baseType);
            
            return isMeleeWeapon;
        }
    }

    /// <summary>
    /// Represents a model used in the Mythal Forge crafting system, managing categories of mythals and their associated data.
    /// </summary>
    public MythalCategoryModel MythalCategoryModel { get; }

    /// <summary>
    /// Represents the model responsible for managing the active crafting properties currently
    /// associated with an item in the Mythal Forge crafting system.
    /// </summary>
    /// <remarks>
    /// This model provides functionality to manage the visibility and manipulation of
    /// the active crafting properties for a particular item. It interacts with crafting
    /// categories and supports operations such as hiding and revealing properties, as well
    /// as retrieving a list of visible properties.
    /// </remarks>
    /// <seealso cref="MythalForgeModel"/>
    /// <seealso cref="MythalCategoryModel"/>
    public ActivePropertiesModel ActivePropertiesModel { get; }


    /// <summary>
    /// Gets the <see cref="NwItem"/> instance associated with the current crafting operation.
    /// This property provides access to the item being modified or analyzed in the Mythal Forge functionality.
    /// </summary>
    public NwItem Item { get; }

    /// <summary>
    /// Gets the maximum crafting budget for the current item within the context of the Mythal Forge.
    /// </summary>
    /// <remarks>
    /// The maximum budget is determined based on the item's type and specific conditions, such as whether
    /// it is a two-handed weapon or has a caster-based property. The calculation is performed by the
    /// CraftingBudgetService associated with the Mythal Forge.
    /// </remarks>
    /// <value>
    /// An integer representing the total allowable crafting points for the item.
    /// </value>
    public int MaxBudget => _budget.MythalBudgetForNwItem(Item);

    /// <summary>
    /// Gets the net power cost change from the current changelist.
    /// Positive values indicate power being added, negative values indicate power being freed up.
    /// </summary>
    public int NetPowerChange
    {
        get
        {
            int netChange = 0;
            foreach (ChangeListModel.ChangelistEntry entry in ChangeListModel.ChangeList())
            {
                if (entry.State == ChangeListModel.ChangeState.Added)
                    netChange += entry.Property.PowerCost;
                else if (entry.State == ChangeListModel.ChangeState.Removed)
                    netChange -= entry.Property.PowerCost;
            }
            return netChange;
        }
    }

    /// <summary>
    /// Gets the remaining powers before any changelist modifications are applied.
    /// This represents the item's current state power budget.
    /// </summary>
    public int InitialRemainingPowers
    {
        get
        {
            // Only count visible properties (which already excludes hidden/removed ones)
            // But we need the original state, so we add back removed and subtract added from current RemainingPowers
            return RemainingPowers + NetPowerChange;
        }
    }

    /// <summary>
    /// Gets the remaining number of available powers in the crafting budget after accounting for the power costs
    /// of active properties and pending additions in the change list.
    /// </summary>
    /// <remarks>
    /// The remaining powers calculation considers both currently visible properties and changes marked for addition.
    /// The result is clamped within a permissible range defined by the crafting system, typically between -16 and the
    /// maximum budget. This value is critical for determining if additional properties can be added or adjustments
    /// need to be made to stay within the allowed crafting limits.
    /// </remarks>
    /// <returns>
    /// The number of powers remaining in the crafting budget.
    /// </returns>
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

    /// <summary>
    /// Gets a collection of visible mythal properties associated with the active properties model.
    /// </summary>
    /// <remarks>
    /// The VisibleProperties property retrieves a list of properties that are currently active and visible
    /// in the crafting interface. These properties are derived from the active properties model,
    /// which determines their visibility based on specific criteria.
    /// </remarks>
    /// <returns>
    /// An enumerable collection of <see cref="MythalCategoryModel.MythalProperty"/> objects representing
    /// the visible mythal properties.
    /// </returns>
    public IEnumerable<MythalCategoryModel.MythalProperty> VisibleProperties =>
        ActivePropertiesModel.GetVisibleProperties();

    /// <summary>
    /// Calculates and returns the crafting difficulty value based on the current change list.
    /// </summary>
    /// <returns>
    /// An integer representing the crafting difficulty. Returns 0 if the change list is empty.
    /// </returns>
    public int GetCraftingDifficulty()
    {
        List<CraftingProperty> addedProperties = ChangeListModel.ChangeList()
            .Where(e => e.State != ChangeListModel.ChangeState.Removed)
            .Select(e => e.Property)
            .ToList();

        if (addedProperties.Count == 0) return 0;

        int craftingDifficulty = 0;

        foreach (CraftingProperty property in addedProperties)
        {
            int dc = _dcCalculator.ComputeDifficulty(property);
            craftingDifficulty = Math.Max(craftingDifficulty, dc);
        }

        return craftingDifficulty;
    }

    /// <summary>
    /// Adds a new property to the current crafting session and updates the mythal category accordingly.
    /// </summary>
    /// <param name="property">The property to be added, containing information like crafting tier and associated details.</param>
    public void AddNewProperty(MythalCategoryModel.MythalProperty property)
    {
        ChangeListModel.AddNewProperty(property);
        MythalCategoryModel.ConsumeMythal(property.Internal.CraftingTier);
    }

    /// <summary>
    /// Applies changes to the item properties based on the current change list.
    /// This method processes the removal and addition of item properties as specified
    /// by the associated <see cref="ChangeListModel"/>. It ensures atomicity by first
    /// validating all property changes before applying them. If any removal operation
    /// fails, no changes will be applied and an error message is sent to the player.
    /// Upon successful application, the player is charged the total gold cost from the
    /// change list, and any required cleanup (e.g., destroying mythals) is performed.
    /// </summary>
    /// <remarks>
    /// The method checks the validity of property removals before applying them.
    /// Additions are then processed separately. Any issues during the removal process
    /// will result in the process being aborted and a message being sent to the player.
    /// </remarks>
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

        Effect explosion = Effect.VisualEffect(VfxType.FnfElectricExplosion);

        _player.LoginCreature?.ApplyEffect(EffectDuration.Instant, explosion);

        MythalCategoryModel.DestroyMythals(_player);
    }

    /// <summary>
    /// Converts the current weapon to a caster weapon by removing all incompatible properties
    /// and setting the caster weapon flag.
    /// </summary>
    /// <remarks>
    /// Incompatible properties include: damage bonuses, massive criticals, attack bonus,
    /// enhancement bonus, on-hit effects, visual effects, keen, and vampiric regeneration.
    /// These are properties available on regular weapons but not on caster weapons.
    /// </remarks>
    public void ConvertToCasterWeapon()
    {
        // List of property types that are NOT allowed on caster weapons
        HashSet<ItemPropertyType> incompatibleTypes =
        [
            ItemPropertyType.DamageBonus,
            ItemPropertyType.MassiveCriticals,
            ItemPropertyType.AttackBonus,
            ItemPropertyType.EnhancementBonus,
            ItemPropertyType.OnHitProperties,
            ItemPropertyType.VisualEffect,
            ItemPropertyType.Keen,
            ItemPropertyType.RegenerationVampiric
        ];

        // Collect properties to remove (can't modify collection while iterating)
        List<ItemProperty> propertiesToRemove = Item.ItemProperties
            .Where(p => incompatibleTypes.Contains(p.Property.PropertyType))
            .ToList();

        // Remove incompatible properties
        foreach (ItemProperty property in propertiesToRemove)
        {
            Item.RemoveItemProperty(property);
        }

        // Set the caster weapon flag
        NWScript.SetLocalInt(Item, ItemTypeConstants.CasterWeaponVar, NWScript.TRUE);

        // Visual feedback
        Effect explosion = Effect.VisualEffect(VfxType.ImpMagblue);
        _player.LoginCreature?.ApplyEffect(EffectDuration.Instant, explosion);

        _player.SendServerMessage("Weapon has been converted to a caster weapon.", ColorConstants.Cyan);
    }

    /// <summary>
    /// Refreshes and updates the state of all mythal categories and their properties based on
    /// the remaining power budget, current item properties, and active changes.
    /// </summary>
    /// <remarks>
    /// This method recalculates the selectability and tooltips of all properties within each
    /// mythal category. It does so by performing validation checks to ensure the current budget,
    /// mythal availability, and validation criteria are met. Non-affordable or invalid properties
    /// are marked as non-selectable, and corresponding tooltip messages are set.
    /// </remarks>
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

    /// Generates a tooltip string that displays the crafting skill associated with the item.
    /// The method determines the appropriate skill based on the base type of the item.
    /// If the item is equippable (such as armor, shields, belts, bracers), it assigns "Craft Armor"
    /// or "Spellcraft" as the skill. If the item is a weapon, it assigns "Craft Weapon" as the skill.
    /// Additionally, the tooltip includes a warning if the required skill rank to craft the item
    /// is not met.
    /// <returns>A string representing the crafting skill tooltip for the item.</returns>
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

    /// <summary>
    /// Determines the relevant crafting skill required for the given item based on its base item type.
    /// </summary>
    /// <returns>
    /// The skill constant associated with the item type:
    /// - Returns SKILL_CRAFT_ARMOR for equippable items related to armor and shields.
    /// - Returns SKILL_CRAFT_WEAPON for ranged, thrown weapons, or ammo.
    /// - Defaults to SKILL_SPELLCRAFT if no specific match is found.
    /// </returns>
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

    /// <summary>
    /// Determines if the player has sufficient skill rank to meet or exceed the crafting difficulty.
    /// </summary>
    /// <returns>
    /// True if the player's skill rank plus a modifier is greater than or equal to the crafting difficulty;
    /// otherwise, false.
    /// </returns>
    public bool CanMakeCheck() =>
        NWScript.GetSkillRank(GetSkill(), _player.LoginCreature) + 20 >= GetCraftingDifficulty();

    /// <summary>
    /// Removes an active crafting property from the current model and updates the changelist.
    /// </summary>
    /// <param name="property">The crafting property to be removed.</param>
    public void RemoveActiveProperty(CraftingProperty property)
    {
        ActivePropertiesModel.HideProperty(property);
        ChangeListModel.AddRemovedProperty(property);
    }

    /// <summary>
    /// Reverts the addition of a crafting property to the change list and refunds the resource cost associated with its tier.
    /// </summary>
    /// <param name="property">The <see cref="CraftingProperty"/> to undo from the change list.</param>
    public void UndoAddition(CraftingProperty property)
    {
        ChangeListModel.UndoAddition(property);
        MythalCategoryModel.RefundMythal(property.CraftingTier);
    }

    /// <summary>
    /// Reverts the removal of a specified crafting property from the change list and updates the active properties model.
    /// Also validates dependent properties and undoes their addition if validation fails.
    /// </summary>
    /// <param name="property">The crafting property to be restored.</param>
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

    /// <summary>
    /// Validates a single property against the current item properties and change list to ensure it meets the criteria for addition or modification.
    /// </summary>
    /// <param name="property">The property to be validated.</param>
    /// <param name="currentItemProps">The list of current item properties to validate against.</param>
    /// <param name="changeList">The list of changes pending for the item.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> indicating the result of the validation and any associated error messages.
    /// </returns>
    public ValidationResult ValidateSingle(MythalCategoryModel.MythalProperty property,
        IReadOnlyList<ItemProperty> currentItemProps,
        List<ChangeListModel.ChangelistEntry> changeList)
    {
        return _validator.Validate(property, currentItemProps, changeList);
    }
}
