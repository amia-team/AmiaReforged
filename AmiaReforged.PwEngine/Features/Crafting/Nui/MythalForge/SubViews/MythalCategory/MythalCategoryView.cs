using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.MythalCategory;
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
    public const string AddPropertyButton = "add_property_button";

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
    public NuiBind<int> CategoryFilterIndex { get; } = new("category_filter_index");
    public NuiBind<List<NuiComboEntry>> CategoryFilterOptions { get; } = new("category_filter_options");

    public const string CategoryFilterChanged = "category_filter_changed";


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

        foreach (MythalCategoryModel.MythalProperty property in allProperties)
        {
            NuiBind<bool> enableProperty = new(property.Id + "_enable");
            NuiBind<Color> costColor = new(property.Id + "_color");
            NuiBind<string> powerCostTooltip = new(property.Id + "_tooltip");
            NuiBind<bool> emphasized = new(property.Id + "_emphasized");


            // Store bindings for presenter to use
            EnabledPropertyBindings.Add(property.Id, enableProperty);
            PowerCostColors.Add(property.Id, costColor);
            PowerCostTooltips.Add(property.Id, powerCostTooltip);
            ButtonIds.Add(property.Id);
            EmphasizedProperties.Add(property.Id, emphasized);
        }

        // Create list template cells: one for property name (wider), one for power cost (narrow), and one for add button
        List<NuiListTemplateCell> templateCells =
        [
            new NuiListTemplateCell(new NuiLabel(PropertyLabels)
            {
                VerticalAlign = NuiVAlign.Middle,
                HorizontalAlign = NuiHAlign.Left,
                Tooltip = PropertyLabels
            }) { Width = 170f },
            new NuiListTemplateCell(new NuiLabel(PropertyCosts)
            {
                VerticalAlign = NuiVAlign.Middle,
                HorizontalAlign = NuiHAlign.Left
            }) { Width = 15f },
            new NuiListTemplateCell(new NuiButtonImage("ui_btn_forgeadd")
            {
                Id = AddPropertyButton,
                Width = 25f,
                Height = 25f,
                Tooltip = "Add this property."
            }) { Width = 25f }
        ];

        NuiList propertyList = new(templateCells, PropertyCount)
        {
            RowHeight = 25f,
            Width = 250f,
            Height = 250f
        };

        // Create a header row with column labels
        NuiRow headerRow = new()
        {
            Children =
            {
                new NuiLabel("Property")
                {
                    Width = 140f,
                    Height = 25f,
                    Tooltip = "The name of the property.",
                    VerticalAlign = NuiVAlign.Middle,
                    HorizontalAlign = NuiHAlign.Left
                },
                new NuiLabel("Cost")
                {
                    Width = 50f,
                    Height = 25f,
                    Tooltip = "The Power Cost of the property.",
                    VerticalAlign = NuiVAlign.Middle,
                    HorizontalAlign = NuiHAlign.Right
                },
                new NuiLabel("")
                {
                    Width = 30f,
                    Height = 25f
                }
            }
        };

        // Create a column with the header and list
        NuiColumn listColumn = new()
        {
            Children = { headerRow, propertyList }
        };

        return listColumn;
    }
}
