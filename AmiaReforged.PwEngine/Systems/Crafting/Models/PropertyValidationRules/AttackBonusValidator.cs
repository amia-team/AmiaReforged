using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.AttackBonus)]
public class AttackBonusValidator : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties, List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        ValidationEnum result = ValidationEnum.Valid;
        string error = string.Empty;
        
        // We just don't want it to already exist on the item or in the changelist
        bool alreadyExists = itemProperties.Any(x => x.Property.PropertyType == ItemPropertyType.AttackBonus);
        bool inChangelist = changelistProperties.Any(x => x.BasePropertyType == ItemPropertyType.AttackBonus && x.State != ChangeListModel.ChangeState.Removed);
        
        result = alreadyExists || inChangelist ? ValidationEnum.PropertyNeverStacks : ValidationEnum.Valid;
        error = result == ValidationEnum.PropertyNeverStacks ? "Attack Bonus already exists on this item." : string.Empty;
        
        return new ValidationResult
        {
            Result = result,
            ErrorMessage = error
        };
    }
}