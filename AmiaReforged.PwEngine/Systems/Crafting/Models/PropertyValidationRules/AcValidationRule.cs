﻿using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.AcBonus)]
public class AcValidationRule : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        string errorMessage = "";
        PropertyValidationResult result = PropertyValidationResult.Valid;
        
        if(incoming.ItemProperty.Property.PropertyType == ItemPropertyType.AcBonus)
        {
            bool anyAcBonus = itemProperties.Any(x => x.Property.PropertyType == ItemPropertyType.AcBonus);
            bool anyInChangelist = changelistProperties.Any(e => e.BasePropertyType == ItemPropertyType.AcBonus && e.State != ChangeListModel.ChangeState.Removed);
            
            result = anyAcBonus || anyInChangelist ? PropertyValidationResult.PropertyNeverStacks : PropertyValidationResult.Valid;
            errorMessage = "AC Bonus already exists on this item.";
        }
        
        return new ValidationResult
        {
            Result = result,
            ErrorMessage = errorMessage
        };
    }
}