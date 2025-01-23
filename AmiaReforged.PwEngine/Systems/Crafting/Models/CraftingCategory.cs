using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models;

public class CraftingCategory
{
    public string CategoryId { get; set; }

    public CraftingCategory(string categoryId)
    {
        CategoryId = categoryId;
        ShowGroup = new NuiBind<bool>(categoryId + "_show");
    }

    public NuiBind<bool> ShowGroup { get; set; }

    public int BaseDifficulty { get; init; }
    public required string Label { get; init; }
    public required IReadOnlyList<CraftingProperty> Properties { get; init; }

    /// <summary>
    /// Provides a functional interface to validate a property before it is applied to an item.
    /// </summary>
    public Func<CraftingProperty, NwItem, List<ChangeListModel.ChangelistEntry>, PropertyValidationResult>
        PerformValidation { get; init; } = (property, item, changeList) =>
    {
        bool propertyRemoved = changeList.Any(ce =>
            ce.State == ChangeListModel.ChangeState.Removed && ce.Property.ItemProperty.Property.PropertyType ==
            property.ItemProperty.Property.PropertyType);
        if (propertyRemoved)
        {
            return PropertyValidationResult.Valid;
        }

        // Check if the property is the same as what is on the item or in the changelist
        return item.ItemProperties.Any(ip => ItemPropertyHelper.PropertiesAreSame(ip, property)) ? PropertyValidationResult.CannotBeTheSame : PropertyValidationResult.Valid;
    };
}