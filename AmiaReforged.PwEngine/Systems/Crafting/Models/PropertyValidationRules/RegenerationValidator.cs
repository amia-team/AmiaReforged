using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.Regeneration)]
public class RegenerationValidator : IValidationRule
{
    
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties, List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        // We just don't want it to already exist on the item or in the changelist
        bool alreadyExists = itemProperties.Any(x => x.Property.PropertyType == ItemPropertyType.Regeneration);
        bool wasNotRemoved = !changelistProperties.Any(e =>
            e is { BasePropertyType: ItemPropertyType.Regeneration, State: ChangeListModel.ChangeState.Removed });
        bool inChangelist = changelistProperties.Any(x => x.BasePropertyType == ItemPropertyType.Regeneration && x.State != ChangeListModel.ChangeState.Removed);
        
        bool onItem = alreadyExists && wasNotRemoved || inChangelist;
        
        ValidationEnum result = onItem ? ValidationEnum.PropertyNeverStacks : ValidationEnum.Valid;
        string error = onItem ? "Regeneration already exists on this item." : string.Empty;
        
        return new ValidationResult
        {
            Result = result,
            ErrorMessage = error
        };
    }
}