using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.RegenerationVampiric)]
public class VampiricRegenerationValidator : IValidationRule
{
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        string error = string.Empty;

        // We just don't want it to already exist on the item or in the changelist
        bool alreadyExists = itemProperties.Any(x => x.Property.PropertyType == ItemPropertyType.RegenerationVampiric);
        bool inChangelist = changelistProperties.Any(x =>
            x.BasePropertyType == ItemPropertyType.RegenerationVampiric &&
            x.State != ChangeListModel.ChangeState.Removed);

        bool onItem = alreadyExists || inChangelist;

        ValidationEnum result = onItem ? ValidationEnum.PropertyNeverStacks : ValidationEnum.Valid;
        error = onItem ? "Vampiric Regeneration already exists on this item." : string.Empty;

        return new()
        {
            Result = result,
            ErrorMessage = error
        };
    }
}