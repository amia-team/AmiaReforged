using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

/// <summary>
///     Duplication here is intentional. This is a validator for specific saving throw bonuses.
/// </summary>
[ValidationRuleFor(Property = ItemPropertyType.SavingThrowBonusSpecific)]
public class SpecificSavingThrowValidator : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        SavingThrow savingThrow = new(incoming);

        // Get all of the saving throw bonuses on the item
        List<SavingThrow> savingThrows = itemProperties
            .Where(x => x.Property.PropertyType == ItemPropertyType.SavingThrowBonusSpecific)
            .Select(x => new SavingThrow(x))
            .ToList();

        // And in the changelist (if it's not being removed)
        savingThrows.AddRange(changelistProperties
            .Where(x => x.BasePropertyType == ItemPropertyType.SavingThrowBonusSpecific &&
                        x.State != ChangeListModel.ChangeState.Removed)
            .Select(x => new SavingThrow(x.Property)));

        int cumulative = savingThrows.Sum(x => x.Bonus);
        bool capped = cumulative > 6;
        // Check if the saving throw already exists on the item
        bool onItem = savingThrows.Any(x => x.ThrowType == savingThrow.ThrowType);

        // The bonus is irrelevant, we just don't want it to already exist on the item or in the changelist
        ValidationEnum result = onItem || capped ? ValidationEnum.CannotStackSameSubtype : ValidationEnum.Valid;
        string error = string.Empty;
        if (capped)
        {
            error = $"You have reached the maximum number of specific saves on an item.";
        }
        else if (onItem)
        {
            error = $"{savingThrow.ThrowType} saving throw already exists on this item.";
        }

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