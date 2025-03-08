using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.OnHitCastSpell)]
public class OnHitCastSpellValidator : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        ValidationEnum result = ValidationEnum.Valid;
        string message = string.Empty;

        List<ItemProperty> existing =
            itemProperties
                .Where(i => i.Property.PropertyType == ItemPropertyType.OnHitCastSpell)
                .ToList();
        List<ItemProperty> changelist =
            changelistProperties
                .Where(i => i.Property.ItemProperty.Property.PropertyType == ItemPropertyType.OnHitCastSpell &&
                            i.State != ChangeListModel.ChangeState.Removed)
                .Select(i => i.Property.ItemProperty)
                .ToList();

        if (existing.Any() || changelist.Any())
        {
            result = ValidationEnum.PropertyNeverStacks;
            message = "OnHit properties cannot be stacked.";
        }

        return new()
        {
            Result = result,
            ErrorMessage = message
        };
    }
}