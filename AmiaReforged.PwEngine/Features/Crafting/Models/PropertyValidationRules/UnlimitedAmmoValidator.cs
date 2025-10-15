using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.UnlimitedAmmunition)]
public class UnlimitedAmmoValidator : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        ValidationEnum result = ValidationEnum.Valid;
        string error = string.Empty;

        // We just don't want it to already exist on the item or in the changelist
        bool alreadyExists = itemProperties.Any(x => x.Property.PropertyType == ItemPropertyType.UnlimitedAmmunition);
        bool anyRemoved = changelistProperties.Any(e =>
            e is { BasePropertyType: ItemPropertyType.UnlimitedAmmunition, State: ChangeListModel.ChangeState.Removed });
        bool inChangelist = changelistProperties.Any(x =>
            x.BasePropertyType == ItemPropertyType.UnlimitedAmmunition && x.State != ChangeListModel.ChangeState.Removed);

        result = alreadyExists && !anyRemoved || inChangelist
            ? ValidationEnum.PropertyNeverStacks
            : ValidationEnum.Valid;
        error = result == ValidationEnum.PropertyNeverStacks
            ? "Unlimited Ammo already exists on this item."
            : string.Empty;

        return new ValidationResult
        {
            Result = result,
            ErrorMessage = error
        };
    }
}
