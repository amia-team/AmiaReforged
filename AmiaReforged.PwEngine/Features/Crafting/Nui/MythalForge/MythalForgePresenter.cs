using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.Crafting.Models.PropertyValidationRules;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ChangeList;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.MythalCategory;
using AmiaReforged.PwEngine.Features.NwObjectHelpers;
using AmiaReforged.PwEngine.Features.Player.PlayerTools.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;

/// <summary>
/// Represents the presenter for the Mythal Forge view, responsible for managing interactions
/// between the associated view, model, and crafting mechanics specific to the Mythal Forge system.
/// </summary>
public sealed class MythalForgePresenter : ScryPresenter<MythalForgeView>
{
    /// <summary>
    /// Represents a delegate that is invoked when the forge is closing.
    /// </summary>
    /// <param name="sender">The instance of the MythalForgePresenter that is sending the event.</param>
    /// <param name="e">An EventArgs object containing details about the event.</param>
    public delegate void ForgeClosingEventHandler(MythalForgePresenter sender, EventArgs e);

    /// <summary>
    /// Represents a delegate for handling the event triggered when the view is updated in the Mythal Forge presenter.
    /// </summary>
    public delegate void ViewUpdatedEventHandler(MythalForgePresenter sender, EventArgs e);

    /// <summary>
    /// The title of the Mythal Forge window.
    /// </summary>
    private const string WindowTitle = "Mythal Forge";

    /// <summary>
    /// Represents the ledger view instance used for managing and interacting with the ledger
    /// specific to the Mythal Forge functionality.
    /// </summary>
    private readonly MythalLedgerView _ledgerView;

    /// <summary>
    /// Represents the player interacting with the Mythal Forge.
    /// </summary>
    private readonly NwPlayer _player;

    /// <summary>
    /// Represents the data model for the Mythal Forge crafting system in the application.
    /// </summary>
    public readonly MythalForgeModel Model;

    /// <summary>
    /// Indicates whether the Mythal Forge creation process is currently active.
    /// </summary>
    private bool _creating;

    [Inject] private Lazy<IRenameItemService> RenameService { get; init; } = null!;

    /// <summary>
    /// Represents the token used to manage the Nui window associated with the Mythal Forge view.
    /// </summary>
    private NuiWindowToken _token;

    /// <summary>
    /// Represents the private NUI window associated with the Mythal Forge.
    /// </summary>
    private NuiWindow? _window;

    // Caches to minimize UI messages
    /// <summary>
    /// A dictionary used to cache scalar values to minimize unnecessary UI messages.
    /// </summary>
    private readonly Dictionary<string, object?> _scalarCache = new();

    /// <summary>
    /// A cache for string lists, used to track and store previously assigned values
    /// for efficient updates in the Mythal Forge view bindings.
    /// </summary>
    private readonly Dictionary<string, List<string>> _stringListCache = new();

    /// <summary>
    /// A private cache that stores lists of boolean values associated with string keys.
    /// </summary>
    private readonly Dictionary<string, List<bool>> _boolListCache = new();

    /// <summary>
    /// A cache storing lists of colors categorized by a unique string key.
    /// </summary>
    private readonly Dictionary<string, List<Color>> _colorListCache = new();

    /// <summary>
    /// Represents the presenter for the Mythal Forge crafting feature, responsible for managing
    /// the interaction between the view and model components for crafting operations.
    /// </summary>
    /// <param name="toolView">The view that this presenter is associated with.</param>
    /// <param name="propertyData">The data structure containing crafting properties.</param>
    /// <param name="budget">The service managing crafting budgets.</param>
    /// <param name="item">The item currently being crafted.</param>
    /// <param name="player">The player performing the crafting operation.</param>
    /// <param name="validator">The property validator to validate crafting operations.</param>
    /// <param name="dcCalculator">The calculator for determining crafting difficulty class.</param>
    public MythalForgePresenter(MythalForgeView toolView, CraftingPropertyData propertyData,
        CraftingBudgetService budget,
        NwItem item, NwPlayer player, PropertyValidator validator, DifficultyClassCalculator dcCalculator)
    {
        Model = new MythalForgeModel(item, propertyData, budget, player, validator, dcCalculator);
        View = toolView;
        _player = player;
        _creating = false;

        _ledgerView = new MythalLedgerView(this, player);

        if (player.LoginCreature != null)
        {
            player.LoginCreature.OnInventoryGoldRemove += PreventGoldCheese;
            player.LoginCreature.OnUnacquireItem += PreventMunchkins;
        }
    }

    /// <summary>
    /// Prevents gold removal from the player's inventory and displays a warning popup to the player.
    /// </summary>
    /// <param name="obj">The event arguments provided when an attempt to remove gold from inventory occurs.</param>
    private void PreventGoldCheese(OnInventoryGoldRemove obj)
    {
        obj.PreventGoldRemove = true;

        GenericWindow
            .Builder()
            .For()
            .SimplePopup()
            .WithPlayer(Token().Player)
            .WithTitle(title: "Don't Try That")
            .WithMessage(message: "Don't try to game the system by dropping items, gold, etc.")
            .OpenWithParent(Token());
    }

    /// <summary>
    /// The visual interface for the Mythal Forge system, handling UI components and interactions in conjunction with the presenter.
    /// </summary>
    public override MythalForgeView View { get; }

    /// <summary>
    /// Provides a read-only list of categories used within the Mythal Forge system.
    /// </summary>
    public IReadOnlyList<MythalCategoryModel.MythalCategory> MythalCategories => Model.MythalCategoryModel.Categories;

    // Event using the delegate.
    /// <summary>
    /// Occurs when the Mythal Forge view is updated.
    /// </summary>
    public event ViewUpdatedEventHandler? ViewUpdated;

    /// <summary>
    /// Triggered automatically when the Mythal Forge window is about to close.
    /// </summary>
    public event ForgeClosingEventHandler? ForgeClosing;

    /// <summary>
    /// Prevents players from attempting to exploit the system by dropping items during crafting.
    /// </summary>
    /// <param name="obj">The event data for an item being unacquired (dropped).</param>
    private void PreventMunchkins(ModuleEvents.OnUnacquireItem obj)
    {
        NwItem? item = obj.Item;

        if (item == null) return;

        GenericWindow
            .Builder()
            .For()
            .SimplePopup()
            .WithPlayer(Token().Player)
            .WithTitle(title: "Don't Try That")
            .WithMessage(message: "Don't try to game the system by dropping items, gold, etc.")
            .OpenWithParent(Token());

        NwModule.Instance.SendMessageToAllDMs("Player " + Token().Player.PlayerName +
                                              " tried to drop items while crafting.");
        _player.LoginCreature?.AcquireItem(item);
    }

    /// <summary>
    /// Processes the specified NUI event and handles it based on the event type.
    /// </summary>
    /// <param name="obj">The NUI event data to process.</param>
    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        switch (obj.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(obj);
                break;
        }
    }

    /// <summary>
    /// Handles button click events. Gets passed in from the Window Director.
    /// </summary>
    /// <param name="eventData">The event data for the button click.</param>
    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (Model.MythalCategoryModel.PropertyMap.TryGetValue(eventData.ElementId,
                out MythalCategoryModel.MythalProperty? property))
        {
            // Guarded addition: check budget, mythals, and validation synchronously
            int remaining = Model.RemainingPowers;
            bool hasMythals = Model.MythalCategoryModel.HasMythals(property.Internal.CraftingTier);
            bool canAfford = property.Internal.PowerCost <= remaining || property.Internal.PowerCost == 0;

            if (!hasMythals || !canAfford)
            {
                property.Selectable = false;
                property.CostLabelTooltip = !hasMythals ? "Not enough mythals." : "Not enough points left.";
            }
            else
            {
                List<ItemProperty> currentProps = Model.Item.ItemProperties.ToList();
                List<ChangeListModel.ChangelistEntry> changeList = Model.ChangeListModel.ChangeList();

                // Prevent duplicate adds in the same session
                bool alreadyQueued = changeList.Any(e =>
                    e.State == ChangeListModel.ChangeState.Added &&
                    e.Property.ItemProperty.Property.PropertyType ==
                    property.Internal.ItemProperty.Property.PropertyType &&
                    ItemPropertyHelper.PropertiesAreSame(e.Property, property.Internal.ItemProperty));

                if (alreadyQueued)
                {
                    property.Selectable = false;
                    property.CostLabelTooltip = "Already queued for addition.";
                }
                else
                {
                    ValidationResult result = Model.ValidateSingle(property, currentProps, changeList);
                    if (result.Result == ValidationEnum.Valid)
                    {
                        Model.AddNewProperty(property);
                    }
                    else
                    {
                        property.Selectable = false;
                        property.CostLabelTooltip = result.ErrorMessage ?? "Validation failed.";
                    }
                }
            }

            Model.RefreshCategories();
        }

        if (eventData.ElementId == MythalForgeView.ApplyNameButtonId)
            if (ApplyName())
                return;

        if (eventData.ElementId == View.ActivePropertiesView.RemoveProperty)
        {
            int index = eventData.ArrayIndex;
            MythalCategoryModel.MythalProperty p = Model.ActivePropertiesModel.GetVisibleProperties()[index];

            Model.RemoveActiveProperty(p);
            Model.RefreshCategories();
        }

        if (eventData.ElementId == ChangelistView.RemoveFromChangeList)
        {
            int index = eventData.ArrayIndex;

            ChangeListModel.ChangelistEntry e = Model.ChangeListModel.ChangeList()[index];

            switch (e.State)
            {
                case ChangeListModel.ChangeState.Added:
                    Model.UndoAddition(e.Property);
                    break;
                case ChangeListModel.ChangeState.Removed:
                    Model.UndoRemoval(e.Property);
                    break;
            }
        }

        if (eventData.ElementId == MythalForgeView.ApplyChanges)
        {
            Model.ApplyChanges();

            RaiseCloseEvent();

            return;
        }

        if (eventData.ElementId == MythalForgeView.Cancel)
        {
            RaiseCloseEvent();

            return;
        }

        UpdateView();
    }

    /// <summary>
    /// Applies the new name to the item in the model, if the name is valid.
    /// Sends a notification to the player if the name field is empty.
    /// </summary>
    /// <returns>
    /// A boolean indicating whether the operation should prematurely exit
    /// the current process flow (true) or proceed (false).
    /// </returns>
    private bool ApplyName()
    {
        string? newName = _token.GetBindValue(View.ItemName);
        if (string.IsNullOrEmpty(newName))
        {
            _player.SendServerMessage(message: "The item name cannot be empty.", ColorConstants.Orange);
            return true;
        }

        // Use rename service with business rules
        RenameItemResult result = RenameService.Value.RenameItem(Model.Item, newName, _player);
        if (!result.IsSuccess)
        {
            _player.SendServerMessage(message: result.ErrorMessage ?? "Failed to rename item.", ColorConstants.Orange);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Executes initialization tasks before the main setup of the presenter.
    /// </summary>
    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), WindowTitle)
        {
            Geometry = new NuiRect(400f, 400f, 1200f, 640f)
        };
    }

    /// <summary>
    /// Updates the view with the latest data, ensuring synchronization between the model
    /// and the UI while minimizing unnecessary changes to the interface.
    /// </summary>
    public override void UpdateView()
    {
        UpdateNameField();
        UpdateItemPowerBindings();
        UpdateItemPropertyBindings();
        UpdateChangeListBindings();
        UpdateGoldCost();
        UpdateDifficultyClass();
        UpdateCategoryBindings();

        OnViewUpdated();
    }

    // Cached bind setters

    /// <summary>
    /// Updates the value of a specific bind only if the value has changed, and caches the updated value.
    /// </summary>
    /// <param name="bind">The <see cref="NuiBind{T}" /> representing the binding.</param>
    /// <param name="value">The new value to be set for the bind.</param>
    private void SetIfChanged(NuiBind<string> bind, string value)
    {
        string key = bind.Key;
        if (_scalarCache.TryGetValue(key, out object? old) && old is string s && s == value) return;
        _scalarCache[key] = value;
        Token().SetBindValue(bind, value);
    }

    /// <summary>
    /// Updates the value of the specified bind only if it has changed.
    /// </summary>
    /// <param name="bind">The bind to be updated.</param>
    /// <param name="value">The new value to set.</param>
    private void SetIfChanged(NuiBind<bool> bind, bool value)
    {
        string key = bind.Key;
        if (_scalarCache.TryGetValue(key, out object? old) && old is bool b && b == value) return;
        _scalarCache[key] = value;
        Token().SetBindValue(bind, value);
    }

    /// <summary>
    /// Updates the value of the given bind if the value has changed.
    /// </summary>
    /// <param name="bind">The NuiBind associated with the property to be updated.</param>
    /// <param name="value">The new value to set for the bind.</param>
    private void SetIfChanged(NuiBind<Color> bind, Color value)
    {
        string key = bind.Key;
        if (_scalarCache.TryGetValue(key, out object? old) && old is Color c && c.Equals(value)) return;
        _scalarCache[key] = value;
        Token().SetBindValue(bind, value);
    }

    // Add overload for int binds (e.g., counts)
    /// <summary>
    /// Sets the value of the specified bind if the value has changed since the last update.
    /// </summary>
    /// <param name="bind">The bind to update.</param>
    /// <param name="value">The new value to set.</param>
    private void SetIfChanged(NuiBind<int> bind, int value)
    {
        string key = bind.Key;
        if (_scalarCache.TryGetValue(key, out object? old) && old is int i && i == value) return;
        _scalarCache[key] = value;
        Token().SetBindValue(bind, value);
    }

    /// <summary>
    /// Updates the list bound to a given key if the new values differ from the current cached values.
    /// </summary>
    /// <param name="bind">The binding key used to identify the list values.</param>
    /// <param name="values">The new list of string values to bind.</param>
    private void SetListIfChanged(NuiBind<string> bind, List<string> values)
    {
        string key = bind.Key;
        if (_stringListCache.TryGetValue(key, out List<string>? old) && old.Count == values.Count)
        {
            bool same = true;
            for (int i = 0; i < values.Count; i++)
            {
                if (!string.Equals(old[i], values[i], StringComparison.Ordinal))
                {
                    same = false;
                    break;
                }
            }

            if (same) return;
        }

        _stringListCache[key] = new List<string>(values);
        Token().SetBindValues(bind, values);
    }

    /// <summary>
    /// Updates the bind with a new list of boolean values if the values have changed
    /// compared to the cached list.
    /// </summary>
    /// <param name="bind">The NuiBind associated with the list of boolean values.</param>
    /// <param name="values">The new list of boolean values to be set.</param>
    private void SetListIfChanged(NuiBind<bool> bind, List<bool> values)
    {
        string key = bind.Key;
        if (_boolListCache.TryGetValue(key, out List<bool>? old) && old.Count == values.Count)
        {
            bool same = true;
            for (int i = 0; i < values.Count; i++)
            {
                if (old[i] != values[i])
                {
                    same = false;
                    break;
                }
            }

            if (same) return;
        }

        _boolListCache[key] = new List<bool>(values);
        Token().SetBindValues(bind, values);
    }

    /// <summary>
    /// Updates the bound list of colors if the new values differ from the cached values.
    /// </summary>
    /// <param name="bind">The binding key associated with the current color list.</param>
    /// <param name="values">The new list of colors to be set if there are changes.</param>
    private void SetListIfChanged(NuiBind<Color> bind, List<Color> values)
    {
        string key = bind.Key;
        if (_colorListCache.TryGetValue(key, out List<Color>? old) && old.Count == values.Count)
        {
            bool same = true;
            for (int i = 0; i < values.Count; i++)
            {
                if (!old[i].Equals(values[i]))
                {
                    same = false;
                    break;
                }
            }

            if (same) return;
        }

        _colorListCache[key] = new List<Color>(values);
        Token().SetBindValues(bind, values);
    }

    /// <summary>
    /// Updates the name field in the view with the name of the item from the model.
    /// </summary>
    private void UpdateNameField()
    {
        SetIfChanged(View.ItemName, Model.Item.Name);
    }

    /// <summary>
    /// Updates the item power bindings in the crafting view to reflect the current power budget state.
    /// </summary>
    /// <remarks>
    /// This method synchronizes the maximum power budget and remaining powers with their respective UI bindings.
    /// Additionally, if the crafting operation is in progress and the remaining powers exceed the budget, a popup is displayed.
    /// </remarks>
    private void UpdateItemPowerBindings()
    {
        SetIfChanged(View.MaxPowers, Model.MaxBudget.ToString());
        int remaining = Model.RemainingPowers;
        SetIfChanged(View.RemainingPowers, remaining.ToString());

        if (_creating) DisplayPopupIfOverBudget(remaining);
    }

    /// <summary>
    /// Displays a popup warning message if the remaining power budget is less than zero.
    /// </summary>
    /// <param name="remaining">The number of remaining powers in the power budget.</param>
    private void DisplayPopupIfOverBudget(int remaining)
    {
        if (remaining < 0)
            GenericWindow
                .Builder()
                .For()
                .SimplePopup()
                .WithPlayer(_player)
                .WithTitle(title: "Mythal Forge: WARNING!!!!")
                .WithMessage(
                    message:
                    "This item is stronger than what a Mythal Forge can create. Take care not to weaken the item when editing it!")
                .OpenWithParent(Token());
    }

    /// <summary>
    /// Updates the bindings for each category and its corresponding properties in the Mythal Forge view.
    /// </summary>
    private void UpdateCategoryBindings()
    {
        Model.RefreshCategories();

        foreach (MythalCategoryModel.MythalCategory category in MythalCategories)
        {
            foreach (MythalCategoryModel.MythalProperty property in category.Properties)
            {
                SetIfChanged(View.CategoryView.EnabledPropertyBindings[property.Id], property.Selectable);
                SetIfChanged(View.CategoryView.EmphasizedProperties[property.Id], !property.Selectable);
                SetIfChanged(View.CategoryView.PowerCostColors[property.Id], property.Color);
                SetIfChanged(View.CategoryView.PowerCostTooltips[property.Id],
                    property.CostLabelTooltip ?? string.Empty);
            }
        }
    }

    /// <summary>
    /// Updates the item property bindings in the view based on the currently visible properties.
    /// </summary>
    /// <remarks>
    /// This method retrieves the list of visible properties from the model and updates the view's bindings
    /// with the corresponding property count, names, power costs, and whether the properties are removable.
    /// </remarks>
    private void UpdateItemPropertyBindings()
    {
        List<MythalCategoryModel.MythalProperty> visibleProperties = Model.VisibleProperties.ToList();

        SetIfChanged(View.ActivePropertiesView.PropertyCount, visibleProperties.Count);

        List<string> labels = visibleProperties.Select(m => m.Label).ToList();
        SetListIfChanged(View.ActivePropertiesView.PropertyNames, labels);

        List<string> powerCosts = visibleProperties.Select(m => m.Internal.PowerCost.ToString()).ToList();
        SetListIfChanged(View.ActivePropertiesView.PropertyPowerCosts, powerCosts);

        List<bool> removable = visibleProperties.Select(m => m.Internal.Removable).ToList();
        SetListIfChanged(View.ActivePropertiesView.Removable, removable);
    }

    /// <summary>
    /// Updates the bindings for the Change List view within the Mythal Forge interface, reflecting the current state of the Change List model.
    /// </summary>
    private void UpdateChangeListBindings()
    {
        List<ChangeListModel.ChangelistEntry> changes = Model.ChangeListModel.ChangeList();

        SetIfChanged(View.ChangelistView.ChangeCount, changes.Count);

        List<string> entryLabels = changes.Select(m => m.Label).ToList();
        SetListIfChanged(View.ChangelistView.PropertyLabel, entryLabels);

        List<string> entryCosts = changes.Select(m => m.Property.PowerCost.ToString()).ToList();
        SetListIfChanged(View.ChangelistView.CostString, entryCosts);

        List<Color> entryColors = changes.Select(m => m.State switch
        {
            ChangeListModel.ChangeState.Added => ColorConstants.Lime,
            ChangeListModel.ChangeState.Removed => ColorConstants.Red,
            _ => ColorConstants.White
        }).ToList();
        SetListIfChanged(View.ChangelistView.Colors, entryColors);
    }

    /// <summary>
    /// Updates the gold cost display and related UI bindings within the crafting interface.
    /// This method calculates the total gold cost for the current changes, updates the color
    /// and tooltip based on affordability, and enables or disables the apply action
    /// depending on the player's ability to proceed with the operation.
    /// </summary>
    private void UpdateGoldCost()
    {
        int total = Model.ChangeListModel.TotalGpCost();
        SetIfChanged(View.GoldCost, total.ToString());

        bool canAfford = total < (_player.LoginCreature?.Gold ?? 0);
        SetIfChanged(View.GoldCostColor, canAfford ? ColorConstants.White : ColorConstants.Red);
        SetIfChanged(View.GoldCostTooltip, canAfford ? "" : "You cannot afford this.");

        bool hasChanges = Model.ChangeListModel.ChangeList().Count > 0;
        // Allow apply if: within budget, OR if item started over budget and we're not making it worse
        int budgetFloor = Math.Min(0, Model.InitialRemainingPowers);
        bool withinBudget = Model.RemainingPowers >= budgetFloor;
        bool validAction = hasChanges && canAfford && Model.CanMakeCheck() && withinBudget;
        SetIfChanged(View.ApplyEnabled, validAction);
        SetIfChanged(View.EncourageGold, !canAfford);
    }

    /// <summary>
    /// Updates the displayed difficulty class in the Mythal Forge view based on the crafting model.
    /// </summary>
    /// <remarks>
    /// This method updates the color coding, tooltip information, difference in difficulty, and indicator to assist crafting based on the results
    /// of checks performed by the associated model.
    /// </remarks>
    private void UpdateDifficultyClass()
    {
        bool canMakeCheck = Model.CanMakeCheck();
        SetIfChanged(View.SkillColor, canMakeCheck ? ColorConstants.White : ColorConstants.Red);
        SetIfChanged(View.DifficultyClass, Model.GetCraftingDifficulty().ToString());
        SetIfChanged(View.SkillTooltip, Model.SkillToolTip());
        SetIfChanged(View.EncourageDifficulty, !canMakeCheck);
    }

    /// <summary>
    /// Creates the NUI window if it does not already exist.
    /// </summary>
    public override void Create()
    {
        _creating = true;
        if (_window == null)
            InitBefore();

        if (_window == null)
        {
            _player.SendServerMessage(
                message: "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
        _ledgerView.Presenter.Create();

        UpdateView();

        _creating = false;
    }

    /// <summary>
    /// Closes the NUI window.
    /// </summary>
    public override void Close()
    {
        if (_player.LoginCreature != null)
        {
            _player.LoginCreature.OnUnacquireItem -= PreventMunchkins;
            _player.LoginCreature.OnInventoryGoldRemove -= PreventGoldCheese;

        }

        OnForgeClosing();

        _token.Close();
    }

    /// <summary>
    /// Gets the NUI window token, creating the window if necessary.
    /// </summary>
    /// <returns>The NUI window token.</returns>
    public override NuiWindowToken Token() => _token;

    /// <summary>
    /// Invokes the <see cref="ViewUpdated"/> event to notify subscribers that the view has been updated.
    /// </summary>
    private void OnViewUpdated()
    {
        ViewUpdated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Invokes the ForgeClosing event, signaling that the forging process is about to close.
    /// </summary>
    private void OnForgeClosing()
    {
        ForgeClosing?.Invoke(this, EventArgs.Empty);
    }
}
