using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.SavingThrowBonus)]
public class SavingThrowValidator : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        ValidationEnum result = ValidationEnum.Valid;
        string error = string.Empty;

        SavingThrow savingThrow = new(incoming);
        
        // Get all of the saving throw bonuses on the item
        List<SavingThrow> savingThrows = itemProperties
            .Where(x => x.Property.PropertyType == ItemPropertyType.SavingThrowBonus)
            .Select(x => new SavingThrow(x))
            .ToList();

        // And in the changelist (if it's not being removed)
        savingThrows.AddRange(changelistProperties
            .Where(x => x.BasePropertyType == ItemPropertyType.SavingThrowBonus &&
                        x.State != ChangeListModel.ChangeState.Removed)
            .Select(x => new SavingThrow(x.Property)));
        
        // Check if the saving throw already exists on the item
        bool onItem = savingThrows.Any(x => x.ThrowType == savingThrow.ThrowType);
        
        // The bonus is irrelevant, we just don't want it to already exist on the item or in the changelist
        result = onItem ? ValidationEnum.CannotStackSameSubtype : ValidationEnum.Valid;
        error = onItem ? $"{savingThrow.ThrowType} saving throw already exists on this item." : string.Empty;

        return new ValidationResult
        {
            Result = result,
            ErrorMessage = error
        };
    }

    private class SavingThrow
    {
        public SavingThrow(ItemProperty property)
        {
            ItemPropertyModel model = new()
            {
                Property = property
            };

            ThrowType = model.SubTypeName;
            Bonus = int.Parse(model.PropertyBonus);
        }

        public string ThrowType { get; set; }
        public int Bonus { get; set; }
    }
}