using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.OnHitProperties)]
public class OnHitValidator : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        ValidationEnum result = ValidationEnum.Valid;
        string message = string.Empty;

        // Is this type of property already on the item?
        bool anyOnHit = itemProperties.Any(x => x.Property.PropertyType == ItemPropertyType.OnHitProperties);
        // If it's on the changelist already (removed), it can be added again
        bool previouslyRemoved = changelistProperties.Any(e =>
            e is { BasePropertyType: ItemPropertyType.OnHitProperties, State: ChangeListModel.ChangeState.Removed });

        bool notRemoved = anyOnHit && !previouslyRemoved;

        // If it's on the changelist as added, then it can't be added again
        bool separateAddition = changelistProperties.Any(e =>
            e is { BasePropertyType: ItemPropertyType.OnHitProperties, State: ChangeListModel.ChangeState.Added });

        bool invalid = separateAddition || notRemoved;

        if (invalid)
        {
            result = ValidationEnum.PropertyNeverStacks;
            message = "On Hit Properties already exist on this item.";
        }


        return new()
        {
            Result = result,
            ErrorMessage = message
        };
    }
}