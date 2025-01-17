using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.CraftingCategory;

public sealed class MythalCategoryView : ScryView<MythalForgePresenter>
{
    private readonly IReadOnlyList<MythalCategoryModel.MythalCategory> _categories;
    public override MythalForgePresenter Presenter { get; protected set; }

    public List<string> ButtonIds { get; } = new();
    
    public readonly Dictionary<string, NuiBind<bool>> EnabledPropertyBindings = new();
    public readonly Dictionary<string, NuiBind<Color>> PowerCostColors = new();
    public readonly Dictionary<string, NuiBind<string>> PowerCostTooltips = new();

    public MythalCategoryView(MythalForgePresenter presenter)
    {
        Presenter = presenter;
        _categories = presenter.MythalCategories;
    }

    public override NuiLayout RootLayout()
    {
        List<NuiElement> elements = new();

        foreach (MythalCategoryModel.MythalCategory category in _categories)
        {
            List<NuiElement> properties = new();
            foreach (MythalCategoryModel.MythalProperty property in category.Properties)
            {
                string id = Guid.NewGuid().ToString();
                property.Id = id;
                NuiBind<bool> enableProperty = new(id + "_enable");
                NuiBind<Color> costColor = new(id + "_color");
                NuiBind<string> powerCostTooltip = new(id + "_tooltip");

                NuiRow propertyRow = new()
                {
                    Children =
                    {
                        new NuiButton(property.Label)
                        {
                            Id = id,
                            Width = 200f,
                            Enabled = enableProperty
                        },
                        new NuiGroup
                        {
                            Element = new NuiLabel(property.InternalProperty.PowerCost.ToString())
                            {
                                ForegroundColor = costColor,
                                HorizontalAlign = NuiHAlign.Center,
                                VerticalAlign = NuiVAlign.Middle
                            },
                            Width = 50f,
                            Height = 50f,
                            Tooltip = powerCostTooltip
                        }
                    }
                };

                properties.Add(propertyRow);
                EnabledPropertyBindings.Add(id, enableProperty);
                PowerCostColors.Add(id, costColor);
                PowerCostTooltips.Add(id, powerCostTooltip);
                ButtonIds.Add(id);
            }

            NuiColumn categoryColumn = new()
            {
                Children =
                {
                    new NuiGroup
                    {
                        Element = new NuiLabel(category.Label),
                        Width = 300f,
                        Height = 30f
                    },
                    new NuiGroup
                    {
                        Element = new NuiColumn
                        {
                            Children = properties,
                        },
                        Border = true,
                        Height = 200f,
                        Width = 300f
                    }
                }
            };

            elements.Add(categoryColumn);
        }

        NuiGroup categoryLayout = new()
        {
            Element = new NuiColumn
            {
                Children = elements,
                Width = 400f,
                Height = 400f
            },
            Border = true,
        };

        return categoryLayout;
    }
}