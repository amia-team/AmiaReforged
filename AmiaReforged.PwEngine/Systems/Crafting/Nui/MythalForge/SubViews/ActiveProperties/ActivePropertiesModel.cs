using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.MythalCategory;
using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ActiveProperties;

public class ActivePropertiesModel
{
    private readonly NwItem _item;
    private readonly NwPlayer _player;

    public readonly List<CraftingProperty> Hidden = new();
    public readonly List<CraftingProperty> Visible = new();


    public ActivePropertiesModel(NwItem item, NwPlayer player, IReadOnlyList<CraftingCategory> categories)
    {
        _item = item;
        _player = player;

        List<CraftingProperty> properties = categories.SelectMany(c => c.Properties).ToList();

        foreach (ItemProperty property in item.ItemProperties)
        {
            if (!ItemPropertyHelper.CanBeRemoved(property) && property.DurationType != EffectDuration.Permanent) continue;

            // Check the existing properties in the categories
            CraftingProperty? craftingProperty = properties.FirstOrDefault(p => ItemPropertyHelper.PropertiesAreSame(p, property)) ??
                                                 ItemPropertyHelper.ToCraftingProperty(property);

            // If the property is in the categories, add it to the list of all properties
            Visible.Add(craftingProperty);
        }
    }

    public void HideProperty(CraftingProperty property)
    {
        Hidden.Add(property);
        Visible.Remove(property);
    }

    public void RevealProperty(CraftingProperty property)
    {
        Hidden.Remove(property);
        Visible.Add(property);
    }

    public List<MythalCategoryModel.MythalProperty> GetVisibleProperties()
    {
        List<MythalCategoryModel.MythalProperty> visibleProperties = Visible.Select(property => new MythalCategoryModel.MythalProperty
        {
            Id = Guid.NewGuid().ToString(), Label = property.GuiLabel, Internal = property, Selectable = true
        }).ToList();
        
        // sort alphabetically
        visibleProperties.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.Ordinal));
        
        return visibleProperties;
    }


    public bool PropertyExistsOnItem(CraftingProperty c) =>
        Visible.Any(property => ItemPropertyHelper.PropertiesAreSame(c, property));

    public void UndoAllChanges()
    {
        foreach (CraftingProperty property in Hidden)
        {
            Visible.Add(property);
        }
        
        Hidden.Clear();
    }
}