using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.SavingThrowBonus)]
public class SavingThrowValidator : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        SavingThrow savingThrow = new(incoming);

        // Get all of the saving throw bonuses on the item
        List<SavingThrow> savingThrows = itemProperties
            .Where(x => x.Property.PropertyType == ItemPropertyType.SavingThrowBonus)
            .Select(x => new SavingThrow(x))
            .ToList();

        // Get all of the removed saving throw bonuses in the changelist
        IEnumerable<SavingThrow> removed = changelistProperties
            .Where(x => x is
                { BasePropertyType: ItemPropertyType.SavingThrowBonus, State: ChangeListModel.ChangeState.Removed })
            .Select(x => new SavingThrow(x.Property));

        // And in the changelist (if it's not being removed)
        IEnumerable<SavingThrow> changelist = changelistProperties
            .Where(x => x.BasePropertyType == ItemPropertyType.SavingThrowBonus &&
                        x.State != ChangeListModel.ChangeState.Removed)
            .Select(x => new SavingThrow(x.Property));

        bool inItemProperties = savingThrows.Any(x => x.ThrowType == savingThrow.ThrowType);
        bool wasNotRemoved = removed.All(x => x.ThrowType != savingThrow.ThrowType);
        bool inChangeList = changelist.Any(x => x.ThrowType == savingThrow.ThrowType);

        bool onItem = inItemProperties && wasNotRemoved || inChangeList;

        // The bonus is irrelevant, we just don't want it to already exist on the item or in the changelist
        ValidationEnum result = onItem ? ValidationEnum.CannotStackSameSubtype : ValidationEnum.Valid;
        string error = onItem ? $"{savingThrow.ThrowType} saving throw already exists on this item." : string.Empty;

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

        public string ThrowType { get; }
        public int Bonus { get; set; }
    }
}