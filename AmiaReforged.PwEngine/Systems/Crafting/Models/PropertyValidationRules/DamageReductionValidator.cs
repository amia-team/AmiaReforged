using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.DamageReduction)]
public class DamageReductionValidator : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        // Only care that it doesn't already exist on the item
        bool alreadyRemoved = changelistProperties.Any(e =>
            e is { BasePropertyType: ItemPropertyType.DamageReduction, State: ChangeListModel.ChangeState.Removed });
        bool onItem = itemProperties.Any(x => x.Property.PropertyType == ItemPropertyType.DamageReduction);
        bool hasBeenAdded = changelistProperties.Any(x =>
            x.BasePropertyType == ItemPropertyType.DamageReduction &&
            x.State != ChangeListModel.ChangeState.Removed);

        bool alreadyExists = onItem && !alreadyRemoved ||
                             hasBeenAdded;

        return new()
        {
            Result = alreadyExists ? ValidationEnum.PropertyNeverStacks : ValidationEnum.Valid,
            ErrorMessage = alreadyExists ? "Damage Reduction already exists on this item." : string.Empty
        };
    }
}