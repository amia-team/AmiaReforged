using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.MythalCategory;

public sealed class MythalCategoryView : ScryView<MythalForgePresenter>
{
    /// <summary>
    ///     A read-only list of Mythal categories. Provided by the presenter.
    /// </summary>
    private readonly IReadOnlyList<MythalCategoryModel.MythalCategory> _categories;

    public readonly Dictionary<string, NuiBind<bool>> EmphasizedProperties = new();

    /// <summary>
    ///     A dictionary of enabled property bindings. More performant than looking up the property by ID.
    /// </summary>
    public readonly Dictionary<string, NuiBind<bool>> EnabledPropertyBindings = new();

    /// <summary>
    ///     A dictionary of power cost color bindings. More performant than looking up the color by ID.
    /// </summary>
    public readonly Dictionary<string, NuiBind<Color>> PowerCostColors = new();

    /// <summary>
    ///     A dictionary of power cost tooltip bindings. More performant than looking up the tooltip by ID.
    /// </summary>
    public readonly Dictionary<string, NuiBind<string>> PowerCostTooltips = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="MythalCategoryView" /> class.
    /// </summary>
    /// <param name="toolPresenter">The presenter for the Mythal Forge.</param>
    public MythalCategoryView(MythalForgePresenter toolPresenter)
    {
        Presenter = toolPresenter;
        _categories = toolPresenter.MythalCategories;
    }

    /// <summary>
    ///     Gets or sets the presenter for the Mythal Forge.
    /// </summary>
    public override MythalForgePresenter Presenter { get; protected set; }

    /// <summary>
    ///     Gets a list of button IDs. Used for event handling.
    /// </summary>
    public List<string> ButtonIds { get; } = new();

    public override NuiLayout RootLayout()
    {
        List<NuiElement> elements = new();

        foreach (MythalCategoryModel.MythalCategory category in _categories)
        {
            List<NuiElement> properties = new();
            foreach (MythalCategoryModel.MythalProperty property in category.Properties)
            {
                NuiBind<bool> enableProperty = new(property.Id + "_enable");
                NuiBind<Color> costColor = new(property.Id + "_color");
                NuiBind<string> powerCostTooltip = new(property.Id + "_tooltip");
                NuiBind<bool> emphasized = new(property.Id + "_emphasized");

                NuiRow propertyRow = new()
                {
                    Children =
                    {
                        new NuiButton(property.Label)
                        {
                            Id = property.Id,
                            Width = 200f,
                            Enabled = enableProperty
                        },
                        new NuiGroup
                        {
                            Element = new NuiLabel(property.Internal.PowerCost.ToString())
                            {
                                ForegroundColor = costColor,
                                HorizontalAlign = NuiHAlign.Center,
                                VerticalAlign = NuiVAlign.Middle
                            },
                            Width = 50f,
                            Height = 50f,
                            Encouraged = emphasized,
                            Tooltip = powerCostTooltip
                        }
                    }
                };

                properties.Add(propertyRow);
                EnabledPropertyBindings.Add(property.Id, enableProperty);
                PowerCostColors.Add(property.Id, costColor);
                PowerCostTooltips.Add(property.Id, powerCostTooltip);
                ButtonIds.Add(property.Id);
                EmphasizedProperties.Add(property.Id, emphasized);
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
                            Children = properties
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
            Border = true
        };

        return categoryLayout;
    }
}