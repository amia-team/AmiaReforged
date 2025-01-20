using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.MythalCategory;
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

    private readonly MythalForgeModel _model;
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
        if (!_model.MythalCategoryModel.PropertyMap.TryGetValue(eventData.ElementId, out MythalCategoryModel.MythalProperty? property))
        {
            if(property == null) return;

            _model.TryAddProperty(property.InternalProperty);
        }

        if (eventData.ElementId == MythalForgeView.ApplyNameButtonId)
        {
            string? newName = _token.GetBindValue(View.ItemName);
            if (string.IsNullOrEmpty(newName))
            {
                _player.SendServerMessage("The item name cannot be empty.", ColorConstants.Orange);
                return;
            }
            
            _model.Item.Name = newName;
        }
        
        UpdateView();
    }

    /// <summary>
    /// Initializes the presenter and sets up initial data.
    /// </summary>
    public override void Initialize()
    {
        // Sets up all of the initial data...Calls the model for this...
        _window = new NuiWindow(View.RootLayout(), WindowTitle)
        {
            Geometry = new NuiRect(500f, 500f, 1000f, 1000f)
        };
    }

    /// <summary>
    /// Updates the view with the latest data.
    /// </summary>
    public override void UpdateView()
    {
        UpdateNameField();
        UpdateItemPowerBindings();
        UpdateCategoryBindings();
        UpdateItemPropertyBindings();
    }

    private void UpdateNameField()
    {
        Token().SetBindValue(View.ItemName, _model.Item.Name);
    }

    private void UpdateItemPowerBindings()
    {
        Token().SetBindValue(View.MaxPowers, _model.MaxBudget.ToString());
        Token().SetBindValue(View.RemainingPowers, _model.RemainingPowers.ToString());
    }

    private void UpdateCategoryBindings()
    {
        _model.RefreshCategories();
            
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

    private void UpdateItemPropertyBindings()
    {
        List<MythalCategoryModel.MythalProperty> visibleProperties = _model.VisibleProperties.ToList();

        int count = visibleProperties.Count;
        Token().SetBindValue(View.ActivePropertiesView.PropertyCount, count);
        
        List<string> labels = visibleProperties.Select(m => m.Label).ToList();
        Token().SetBindValues(View.ActivePropertiesView.PropertyNames, labels);

        List<string> powerCosts = visibleProperties.Select(m => m.InternalProperty.PowerCost.ToString()).ToList();
        Token().SetBindValues(View.ActivePropertiesView.PropertyPowerCosts, powerCosts);

        List<bool> removable = visibleProperties.Select(m => m.InternalProperty.Removable).ToList();
        Token().SetBindValues(View.ActivePropertiesView.Removable, removable);
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
        
        UpdateView();
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
        return _token;
    }
    
    /// <summary>
    /// Gets the list of Mythal categories from the model.
    /// </summary>
    public IReadOnlyList<MythalCategoryModel.MythalCategory> MythalCategories => _model.MythalCategoryModel.Categories;
}