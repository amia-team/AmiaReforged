using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.CraftingCategory;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

/// <summary>
/// Represents the presenter for the Mythal Forge view.
/// </summary>
public sealed class MythalForgePresenter : ScryPresenter<MythalForgeView>
{
    /// <summary>
    /// The title of the Mythal Forge window.
    /// </summary>
    private const string WindowTitle = "Mythal Forge";

    /// <summary>
    /// Gets the view associated with this presenter.
    /// </summary>
    public override MythalForgeView View { get; }

    private MythalForgeModel _model;
    private NuiWindowToken _token;
    private readonly NwPlayer _player;
    private NuiWindow? _window;

    /// <summary>
    /// Initializes a new instance of the <see cref="MythalForgePresenter"/> class.
    /// </summary>
    /// <param name="view">The view associated with this presenter.</param>
    /// <param name="propertyData">The crafting property data.</param>
    /// <param name="budget">The crafting budget service.</param>
    /// <param name="item">The item being crafted.</param>
    /// <param name="player">The player performing the crafting.</param>
    public MythalForgePresenter(MythalForgeView view, CraftingPropertyData propertyData, CraftingBudgetService budget,
        NwItem item, NwPlayer player)
    {
        _model = new MythalForgeModel(item, propertyData, budget, player);
        View = view;
        _player = player;
        
        NwModule.Instance.OnNuiEvent += HandleNuiInputs;
    }

    /// <summary>
    /// Handles NUI input events.
    /// </summary>
    /// <param name="obj">The NUI event data.</param>
    private void HandleNuiInputs(ModuleEvents.OnNuiEvent obj)
    {
        switch (obj.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(obj);
                break;
        }
    }

    /// <summary>
    /// Handles button click events.
    /// </summary>
    /// <param name="eventData">The event data for the button click.</param>
    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (View.CategoryView.ButtonIds.Contains(eventData.ElementId))
        {
            // Handle category view button click
        }
    }

    /// <summary>
    /// Initializes the presenter and sets up initial data.
    /// </summary>
    public override void Initialize()
    {
        // Sets up all of the initial data...Calls the model for this...
        UpdateCategoryBindings();

        _window = new NuiWindow(View.RootLayout(), WindowTitle)
        {
            Geometry = new NuiRect(500f, 500f, 1000f, 1000f)
        };
    }

    /// <summary>
    /// Updates the category bindings with the latest data.
    /// </summary>
    private void UpdateCategoryBindings()
    {
        _model.RecalculateCategoryAffordability();
        foreach (MythalCategoryModel.MythalCategory category in MythalCategories)
        {
            foreach (MythalCategoryModel.MythalProperty property in category.Properties)
            {
                Token().SetBindValue(View.CategoryView.EnabledPropertyBindings[property.Id], property.Selectable);
                Token().SetBindValue(View.CategoryView.PowerCostColors[property.Id], property.Color);
                Token().SetBindValue(View.CategoryView.PowerCostTooltips[property.Id], property.CostLabelTooltip);
            }
        }
    }

    /// <summary>
    /// Updates the view with the latest data.
    /// </summary>
    public override void UpdateView()
    {
        // Updates all the nui bindings for data that can change...
    }

    /// <summary>
    /// Creates the NUI window if it does not already exist.
    /// </summary>
    public override void Create()
    {
        // Create the window if it's null.
        if (_window == null)
        {
            // Try to create the window if it doesn't exist.
            Initialize();
        }

        // If the window wasn't created, then tell the user we screwed up.
        if (_window == null)
        {
            _player.SendServerMessage("The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        // This assigns out our token and renders the actual NUI window.
        _player.TryCreateNuiWindow(_window, out _token);
    }

    /// <summary>
    /// Closes the NUI window.
    /// </summary>
    public override void Close()
    {
        _token.Close();
    }

    /// <summary>
    /// Gets the NUI window token, creating the window if necessary.
    /// </summary>
    /// <returns>The NUI window token.</returns>
    public override NuiWindowToken Token()
    {
        Create();
        return _token;
    }
    
    /// <summary>
    /// Gets the list of Mythal categories from the model.
    /// </summary>
    public IReadOnlyList<MythalCategoryModel.MythalCategory> MythalCategories => _model.MythalCategoryModel.Categories;
}