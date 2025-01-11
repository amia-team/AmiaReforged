using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public class CraftingCategorySectionView : INuiModel
{
    private readonly MythalForgeWindow _window;
    private readonly List<CraftingCategory> _categories;

    public CraftingCategorySectionView(MythalForgeWindow window, List<CraftingCategory> categories)
    {
        _window = window;
        _categories = categories;
    }

    public NuiElement GetElement()
    {
        List<NuiElement> elements = new();
        foreach (CraftingCategory category in _categories)
        {
            List<NuiElement> properties = new();
            foreach (CraftingProperty categoryProperty in category.Properties)
            {
                NuiRow row = new()
                {
                    Children =
                    {
                        new NuiButton(categoryProperty.GuiLabel)
                        {
                            Id = Guid.NewGuid().ToString(),
                            Width = 200f,
                            Enabled = _window.PropertyEnabled,
                            ForegroundColor = _window.PropertyColors
                        }.Assign(out categoryProperty.Button),
                        new NuiGroup
                        {
                            Element = new NuiLabel(categoryProperty.PowerCost.ToString())
                            {
                                HorizontalAlign = NuiHAlign.Center,
                                VerticalAlign = NuiVAlign.Middle
                            },
                            Width = 50f,
                            Height = 50f,
                            Tooltip = "Power Cost On Item"
                        }
                    }
                };

                properties.Add(row);
            }

            NuiColumn column = new()
            {
                Children =
                {
                    new NuiGroup
                    {
                        Element = new NuiLabel(category.Label),
                        Width = 300f,
                        Height = 30f,
                        Border = true
                    },
                    new NuiGroup
                    {
                        Element = new NuiColumn
                        {
                            Children = properties
                        },
                        Border = true,
                        Height = 200f,
                        Width = 300f
                    }
                }
            };
            elements.Add(column);
        }
        
        NuiGroup categorySection = new()
        {
            Element = new NuiColumn
            {
                Children = elements,
                Width = 400f,
                Height = 400f
            },
            Border = true,
        };
        
        return categorySection;
    }
}