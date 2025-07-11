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
        ValidationEnum @enum = ValidationEnum.Valid;

        if (incoming.ItemProperty.Property.PropertyType == ItemPropertyType.AcBonus)
        {
            bool anyAcBonus = itemProperties.Any(x => x.Property.PropertyType == ItemPropertyType.AcBonus);
            bool acBonusNotRemoved = !changelistProperties.Any(e =>
                e is { BasePropertyType: ItemPropertyType.AcBonus, State: ChangeListModel.ChangeState.Removed });
            bool anyInChangelist = changelistProperties.Any(e =>
                e.BasePropertyType == ItemPropertyType.AcBonus && e.State != ChangeListModel.ChangeState.Removed);

            @enum = anyAcBonus && acBonusNotRemoved || anyInChangelist
                ? ValidationEnum.PropertyNeverStacks
                : ValidationEnum.Valid;
            errorMessage = "AC Bonus already exists on this item.";
        }

        return new ValidationResult
        {
            Result = @enum,
            ErrorMessage = errorMessage
        };
    }
}