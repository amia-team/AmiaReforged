using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.MythalCategory;
using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ActiveProperties;

public class ActivePropertiesModel
{
    public readonly List<CraftingProperty> Hidden = new();
    public readonly List<CraftingProperty> Visible = new();


    public ActivePropertiesModel(NwItem item, IReadOnlyList<CraftingCategory> categories)
    {
        List<CraftingProperty> properties = categories.SelectMany(c => c.Properties).ToList();

        foreach (ItemProperty property in item.ItemProperties)
        {
            if (!ItemPropertyHelper.CanBeRemoved(property) &&
                property.DurationType != EffectDuration.Permanent) continue;

            // Check the existing properties in the categories
            CraftingProperty craftingProperty =
                properties.FirstOrDefault(p => ItemPropertyHelper.PropertiesAreSame(p, property)) ??
                ItemPropertyHelper.ToCraftingProperty(property);

            // If the property is in the categories, add it to the list of all properties
            Visible.Add(craftingProperty);
        }
    }

    public void HideProperty(CraftingProperty property)
    {
        Hidden.Add(property);
        Visible.Remove(property);

        // sort alphabetically
        Hidden.Sort((a, b) => string.Compare(a.GuiLabel, b.GuiLabel, StringComparison.Ordinal));
    }

    public void RevealProperty(CraftingProperty property)
    {
        Hidden.Remove(property);
        Visible.Add(property);

        // sort alphabetically
        Visible.Sort((a, b) => string.Compare(a.GuiLabel, b.GuiLabel, StringComparison.Ordinal));
    }

    public List<MythalCategoryModel.MythalProperty> GetVisibleProperties()
    {
        List<MythalCategoryModel.MythalProperty> visibleProperties = Visible.Select(property =>
            new MythalCategoryModel.MythalProperty
            {
                Id = Guid.NewGuid().ToString(), Label = property.GuiLabel, Internal = property, Selectable = true
            }).ToList();

        // sort alphabetically
        visibleProperties.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.Ordinal));

        return visibleProperties;
    }
}