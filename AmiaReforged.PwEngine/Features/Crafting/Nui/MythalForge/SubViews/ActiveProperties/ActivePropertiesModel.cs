﻿using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.MythalCategory;
using AmiaReforged.PwEngine.Features.NwObjectHelpers;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ActiveProperties;

public class ActivePropertiesModel
{
    private readonly List<CraftingProperty> _hidden = new();
    private readonly List<CraftingProperty> _visible = new();


    public ActivePropertiesModel(NwItem item, IReadOnlyList<CraftingCategory> categories)
    {
        List<CraftingProperty> properties = categories.SelectMany(c => c.Properties).ToList();

        NLog.LogManager.GetCurrentClassLogger().Info($"ActivePropertiesModel: Processing item with {item.ItemProperties.Count()} properties");
        NLog.LogManager.GetCurrentClassLogger().Info($"ActivePropertiesModel: Loaded {properties.Count} properties from {categories.Count} categories");

        foreach (ItemProperty property in item.ItemProperties)
        {
            if (!ItemPropertyHelper.CanBeRemoved(property) &&
                property.DurationType != EffectDuration.Permanent) continue;

            // Check the existing properties in the categories
            CraftingProperty? matchedProperty = properties.FirstOrDefault(p => ItemPropertyHelper.PropertiesAreSame(p, property));

            if (matchedProperty != null)
            {
                NLog.LogManager.GetCurrentClassLogger().Info($"✓ Direct match found for property in categories");
                _visible.Add(matchedProperty);
            }
            else
            {
                NLog.LogManager.GetCurrentClassLogger().Info($"✗ No direct match - calling ToCraftingProperty with {categories.Count} categories");
                CraftingProperty craftingProperty = ItemPropertyHelper.ToCraftingProperty(property, categories);
                _visible.Add(craftingProperty);
            }
        }

        NLog.LogManager.GetCurrentClassLogger().Info($"ActivePropertiesModel: Loaded {_visible.Count} visible properties");
    }

    public void HideProperty(CraftingProperty property)
    {
        _hidden.Add(property);
        _visible.Remove(property);

        // sort alphabetically
        _hidden.Sort((a, b) => string.Compare(a.GuiLabel, b.GuiLabel, StringComparison.Ordinal));
    }

    public void RevealProperty(CraftingProperty property)
    {
        _hidden.Remove(property);
        _visible.Add(property);

        // sort alphabetically
        _visible.Sort((a, b) => string.Compare(a.GuiLabel, b.GuiLabel, StringComparison.Ordinal));
    }

    public List<MythalCategoryModel.MythalProperty> GetVisibleProperties()
    {
        List<MythalCategoryModel.MythalProperty> visibleProperties = _visible.Select(property =>
            new MythalCategoryModel.MythalProperty
            {
                Id = Guid.NewGuid().ToString(), Label = property.GuiLabel, Internal = property, Selectable = true
            }).ToList();

        // sort alphabetically
        visibleProperties.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.Ordinal));

        return visibleProperties;
    }
}
