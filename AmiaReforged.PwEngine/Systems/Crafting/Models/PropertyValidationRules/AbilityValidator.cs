using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.AbilityBonus)]
public class AbilityValidator : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        ValidationEnum @enum = ValidationEnum.Valid;
        string error = string.Empty;

        // Extract any one of Cha, Wis, Str, Dex, Int, Con bonuses substrings in the incoming item property label
        string incomingLabel = ItemPropertyHelper.GameLabel(incoming.ItemProperty);
        string noEnhancementBonus = incomingLabel.Replace(oldValue: "Enhancement Bonus: ", newValue: "");

        string[] split = noEnhancementBonus.Split(separator: " ");
        string incomingAbility = split[0];

        // Check if the incoming ability has been removed from the ChangeList
        bool removed = changelistProperties.Any(e =>
            e is { BasePropertyType: ItemPropertyType.AbilityBonus, State: ChangeListModel.ChangeState.Removed } &&
            AbilitiesAreSame(incomingAbility, e.Property));

        if (!removed)
        {
            // Check if the incoming ability already exists on the item
            bool anyAbilityBonus = itemProperties.Any(x =>
                x.Property.PropertyType == ItemPropertyType.AbilityBonus && AbilitiesAreSame(incomingAbility, x));
            // Check if the incoming ability is in the ChangeList
            bool anyInChangelist = changelistProperties.Any(e =>
                e.BasePropertyType == ItemPropertyType.AbilityBonus && AbilitiesAreSame(incomingAbility, e.Property) &&
                e.State != ChangeListModel.ChangeState.Removed);

            @enum = anyAbilityBonus || anyInChangelist ? ValidationEnum.CannotStackSameSubtype : ValidationEnum.Valid;
            error = "Ability already exists on this item.";
        }


        return new ValidationResult
        {
            Result = @enum,
            ErrorMessage = error
        };
    }

    private bool AbilitiesAreSame(string incomingAbility, ItemProperty itemProperty)
    {
        string itemPropertyLabel = ItemPropertyHelper.GameLabel(itemProperty);
        string noEnhancementBonus = itemPropertyLabel.Replace(oldValue: "Enhancement Bonus: ", newValue: "");

        string[] split = noEnhancementBonus.Split(separator: " ");

        string itemPropertyAbility = split[0];

        return incomingAbility == itemPropertyAbility;
    }
}