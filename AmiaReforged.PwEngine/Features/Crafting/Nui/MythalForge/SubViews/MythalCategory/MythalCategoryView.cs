using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.MythalCategory;

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

    public const string SearchPropertiesButton = "search_properties";

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

    public NuiBind<int> PropertyCount { get; } = new("property_count");
    public NuiBind<string> PropertyLabels { get; } = new("property_labels");
    public NuiBind<string> PropertyCosts { get; } = new("property_costs");
    public NuiBind<string> PropertyFilterText { get; } = new("property_filter_text");

    public override NuiLayout RootLayout()
    {
        // Collect all properties from all categories
        List<MythalCategoryModel.MythalProperty> allProperties = new();
        foreach (MythalCategoryModel.MythalCategory category in _categories)
        {
            allProperties.AddRange(category.Properties);
        }

        // Sort alphabetically by label
        allProperties.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.Ordinal));

        // Create list template cells for the NuiList - property name and cost columns
        List<NuiListTemplateCell> cells = new();

        foreach (MythalCategoryModel.MythalProperty property in allProperties)
        {
            NuiBind<bool> enableProperty = new(property.Id + "_enable");
            NuiBind<Color> costColor = new(property.Id + "_color");
            NuiBind<string> powerCostTooltip = new(property.Id + "_tooltip");
            NuiBind<bool> emphasized = new(property.Id + "_emphasized");

            string tierTooltip = property.Internal.CraftingTier.ToString();

            // Store bindings for presenter to use
            EnabledPropertyBindings.Add(property.Id, enableProperty);
            PowerCostColors.Add(property.Id, costColor);
            PowerCostTooltips.Add(property.Id, powerCostTooltip);
            ButtonIds.Add(property.Id);
            EmphasizedProperties.Add(property.Id, emphasized);
        }

        // Create two list template cells: one for property name (wider), one for power cost (narrow)
        List<NuiListTemplateCell> templateCells =
        [
            new NuiListTemplateCell(new NuiLabel(PropertyLabels)
            {
                VerticalAlign = NuiVAlign.Middle,
                HorizontalAlign = NuiHAlign.Left,
                Tooltip = PropertyLabels
            }) { Width = 180f },
            new NuiListTemplateCell(new NuiLabel(PropertyCosts)
            {
                VerticalAlign = NuiVAlign.Middle,
                HorizontalAlign = NuiHAlign.Left
            }) { Width = 50f }
        ];

        NuiList propertyList = new(templateCells, PropertyCount)
        {
            RowHeight = 38f,
            Width = 230f,
            Height = 250f
        };

        return new NuiColumn
        {
            Children = { propertyList }
        };
    }
}
