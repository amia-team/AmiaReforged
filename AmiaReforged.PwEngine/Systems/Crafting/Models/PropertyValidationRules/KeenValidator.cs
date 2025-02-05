using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.Keen)]
public class KeenValidator : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties, List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        // We really only care that it doesn't already exist on the item or the chagngelist
        
        bool alreadyExists = itemProperties.Any(x => x.Property.PropertyType == ItemPropertyType.Keen);
        bool wasNotRemoved = !changelistProperties.Any(e =>
            e is { BasePropertyType: ItemPropertyType.Keen, State: ChangeListModel.ChangeState.Removed });
        bool inChangelist = changelistProperties.Any(x => x.BasePropertyType == ItemPropertyType.Keen && x.State != ChangeListModel.ChangeState.Removed);
        
        ValidationEnum result = alreadyExists && wasNotRemoved || inChangelist ? ValidationEnum.PropertyNeverStacks : ValidationEnum.Valid;
        string error = result == ValidationEnum.PropertyNeverStacks ? "Keen already exists on this item.": string.Empty;
        
        return new ValidationResult
        {
            Result = result,
            ErrorMessage = error
        };
    }
}