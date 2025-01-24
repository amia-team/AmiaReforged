﻿using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.EnhancementBonus)]
public class EnhancementBonusValidator : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties, List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        string error = string.Empty;
        
        // We just don't want it to already exist on the item or in the changelist
        bool alreadyExists = itemProperties.Any(x => x.Property.PropertyType == ItemPropertyType.EnhancementBonus);
        bool inChangelist = changelistProperties.Any(x => x.BasePropertyType == ItemPropertyType.EnhancementBonus && x.State != ChangeListModel.ChangeState.Removed);
        
        bool onItem = alreadyExists || inChangelist;
        
        ValidationEnum result = onItem ? ValidationEnum.PropertyNeverStacks : ValidationEnum.Valid;
        error = onItem ? "Enhancement Bonus already exists on this item." : string.Empty;
        
        return new ValidationResult
        {
            Result = result,
            ErrorMessage = error
        };
    }
}