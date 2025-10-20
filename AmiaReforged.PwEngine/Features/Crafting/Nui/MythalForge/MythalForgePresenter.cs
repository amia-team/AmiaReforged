using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.Crafting.Models.PropertyValidationRules;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ChangeList;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.MythalCategory;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;

/// <summary>
///     Represents the presenter for the Mythal Forge view.
/// </summary>
public sealed class MythalForgePresenter : ScryPresenter<MythalForgeView>
{
    public delegate void ForgeClosingEventHandler(MythalForgePresenter sender, EventArgs e);

    /// <summary>
    ///     An event raised every time Update is called.
    /// </summary>
    public delegate void ViewUpdatedEventHandler(MythalForgePresenter sender, EventArgs e);

    /// <summary>
    ///     The title of the Mythal Forge window.
    /// </summary>
    private const string WindowTitle = "Mythal Forge";

    private readonly MythalLedgerView _ledgerView;
    private readonly NwPlayer _player;

    public readonly MythalForgeModel Model;
    private bool _creating;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MythalForgePresenter" /> class.
    /// </summary>
    /// <param name="toolView">The view associated with this presenter.</param>
    /// <param name="propertyData">The crafting property data.</param>
    /// <param name="budget">The crafting budget service.</param>
    /// <param name="item">The item being crafted.</param>
    /// <param name="player">The player performing the crafting.</param>
    /// <param name="validator"></param>
    public MythalForgePresenter(MythalForgeView toolView, CraftingPropertyData propertyData,
        CraftingBudgetService budget,
        NwItem item, NwPlayer player, PropertyValidator validator, DifficultyClassCalculator dcCalculator)
    {
        Model = new MythalForgeModel(item, propertyData, budget, player, validator, dcCalculator);
        View = toolView;
        _player = player;
        _creating = false;

        _ledgerView = new MythalLedgerView(this, player);

        if (player.LoginCreature != null) player.LoginCreature.OnUnacquireItem += PreventMunchkins;
    }

    /// <summary>
    ///     Gets the view associated with this presenter.
    /// </summary>
    public override MythalForgeView View { get; }

    /// <summary>
    ///     Gets the list of Mythal categories from the model.
    /// </summary>
    public IReadOnlyList<MythalCategoryModel.MythalCategory> MythalCategories => Model.MythalCategoryModel.Categories;

    // Event using the delegate.
    public event ViewUpdatedEventHandler? ViewUpdated;

    public event ForgeClosingEventHandler? ForgeClosing;

    private void PreventMunchkins(ModuleEvents.OnUnacquireItem obj)
    {
        NwItem? item = obj.Item;

        if(item == null) return;
        if (!item.ResRef.Contains(value: "mythal")) return;

        GenericWindow
            .Builder()
            .For()
            .SimplePopup()
            .WithPlayer(Token().Player)
            .WithTitle(title: "Don't Try That")
            .WithMessage(message: "Don't try to game the system by dropping the mythals. You will lose all progress.")
            .OpenWithParent(Token());

        NwModule.Instance.SendMessageToAllDMs("Player " + Token().Player.PlayerName +
                                              " tried to drop a mythal while crafting.");
        _player.LoginCreature?.AcquireItem(item);
    }

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
    ///     Handles button click events. Gets passed in from the Window Director.
    /// </summary>
    /// <param name="eventData">The event data for the button click.</param>
        private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (Model.MythalCategoryModel.PropertyMap.TryGetValue(eventData.ElementId,
                out MythalCategoryModel.MythalProperty? property))
        {
            // Check budget, mythals, and validation synchronously
            int remaining = Model.RemainingPowers;
            bool hasMythals = Model.MythalCategoryModel.HasMythals(property.Internal.CraftingTier);
            bool canAfford = property.Internal.PowerCost <= remaining || property.Internal.PowerCost == 0;

            if (!hasMythals || !canAfford)
            {
                // reflect the reason without adding
                property.Selectable = false;
                property.CostLabelTooltip = !hasMythals ? "Not enough mythals." : "Not enough points left.";
            }
            else
            {
                // Use cached state to avoid allocations; single-property validation
                List<ItemProperty> currentProps = Model.Item.ItemProperties.ToList();
                List<ChangeListModel.ChangelistEntry> changeList = Model.ChangeListModel.ChangeList();
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

    private bool ApplyName()
    {
        string? newName = _token.GetBindValue(View.ItemName);
        if (string.IsNullOrEmpty(newName))
        {
            _player.SendServerMessage(message: "The item name cannot be empty.", ColorConstants.Orange);
            return true;
        }

        Model.Item.Name = newName;
        return false;
    }

    /// <summary>
    ///     Initializes the presenter and sets up initial data.
    /// </summary>
    public override void InitBefore()
    {
        // N.B: Other things can happen here, so if you're following this as an example
        // you can do more than just create the window here. The window is supposed to be created here, but you might
        // want to initialize other data that might not be present at construction time.
        _window = new NuiWindow(View.RootLayout(), WindowTitle)
        {
            Geometry = new NuiRect(400f, 400f, 1200f, 640f)
        };
    }

    /// <summary>
    ///     Updates the view with the latest data.
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

        // Raise the event to subscribed child windows.
        OnViewUpdated();
    }

    private void UpdateNameField()
    {
        Token().SetBindValue(View.ItemName, Model.Item.Name);
    }

    private void UpdateItemPowerBindings()
    {
        Token().SetBindValue(View.MaxPowers, Model.MaxBudget.ToString());
        int remaining = Model.RemainingPowers;
        Token().SetBindValue(View.RemainingPowers, remaining.ToString());

        if (_creating) DisplayPopupIfOverBudget(remaining);
    }

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

    private void UpdateCategoryBindings()
    {
        Model.RefreshCategories();

        foreach (MythalCategoryModel.MythalCategory category in MythalCategories)
        {
            foreach (MythalCategoryModel.MythalProperty property in category.Properties)
            {
                Token().SetBindValue(View.CategoryView.EnabledPropertyBindings[property.Id], property.Selectable);
                Token().SetBindValue(View.CategoryView.EmphasizedProperties[property.Id], !property.Selectable);
                Token().SetBindValue(View.CategoryView.PowerCostColors[property.Id], property.Color);
                Token().SetBindValue(View.CategoryView.PowerCostTooltips[property.Id], property.CostLabelTooltip);
            }
        }
    }

    private void UpdateItemPropertyBindings()
    {
        List<MythalCategoryModel.MythalProperty> visibleProperties = Model.VisibleProperties.ToList();

        int count = visibleProperties.Count;
        Token().SetBindValue(View.ActivePropertiesView.PropertyCount, count);

        List<string> labels = visibleProperties.Select(m => m.Label).ToList();
        Token().SetBindValues(View.ActivePropertiesView.PropertyNames, labels);

        List<string> powerCosts = visibleProperties.Select(m => m.Internal.PowerCost.ToString()).ToList();
        Token().SetBindValues(View.ActivePropertiesView.PropertyPowerCosts, powerCosts);

        List<bool> removable = visibleProperties.Select(m => m.Internal.Removable).ToList();
        Token().SetBindValues(View.ActivePropertiesView.Removable, removable);
    }

    private void UpdateChangeListBindings()
    {
        int count = Model.ChangeListModel.ChangeList().Count;
        Token().SetBindValue(View.ChangelistView.ChangeCount, count);

        List<string> entryLabels = Model.ChangeListModel.ChangeList().Select(m => m.Label).ToList();
        Token().SetBindValues(View.ChangelistView.PropertyLabel, entryLabels);

        List<string> entryCosts =
            Model.ChangeListModel.ChangeList().Select(m => m.Property.PowerCost.ToString()).ToList();
        Token().SetBindValues(View.ChangelistView.CostString, entryCosts);

        List<Color> entryColors = Model.ChangeListModel.ChangeList().Select(m => m.State switch
        {
            ChangeListModel.ChangeState.Added => ColorConstants.Lime,
            ChangeListModel.ChangeState.Removed => ColorConstants.Red,
            _ => ColorConstants.White
        }).ToList();

        Token().SetBindValues(View.ChangelistView.Colors, entryColors);
    }

    private void UpdateGoldCost()
    {
        Token().SetBindValue(View.GoldCost, Model.ChangeListModel.TotalGpCost().ToString());

        bool canAfford = Model.ChangeListModel.TotalGpCost() < _player.LoginCreature?.Gold;
        Token().SetBindValue(View.GoldCostColor, canAfford ? ColorConstants.White : ColorConstants.Red);
        Token().SetBindValue(View.GoldCostTooltip, canAfford ? "" : "You cannot afford this.");
        bool validAction = canAfford && Model.CanMakeCheck() && Model.RemainingPowers <= Model.MaxBudget;
        Token().SetBindValue(View.ApplyEnabled, validAction);
        Token().SetBindValue(View.EncourageGold, !canAfford);
    }

    private void UpdateDifficultyClass()
    {
        bool canMakeCheck = Model.CanMakeCheck();
        Token().SetBindValue(View.SkillColor, canMakeCheck ? ColorConstants.White : ColorConstants.Red);
        Token().SetBindValue(View.DifficultyClass, Model.GetCraftingDifficulty().ToString());
        Token().SetBindValue(View.SkillTooltip, Model.SkillToolTip());
        Token().SetBindValue(View.EncourageDifficulty, !canMakeCheck);
    }

    /// <summary>
    ///     Creates the NUI window if it does not already exist.
    /// </summary>
    public override void Create()
    {
        _creating = true;
        // Create the window if it's null.
        if (_window == null)
            // Try to create the window if it doesn't exist.
            InitBefore();

        // If the window wasn't created, then tell the user we screwed up.
        if (_window == null)
        {
            _player.SendServerMessage(
                message: "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        // This assigns out our token and renders the actual NUI window.
        _player.TryCreateNuiWindow(_window, out _token);
        _ledgerView.Presenter.Create();

        UpdateView();

        // Create the child view (the ledger)

        _creating = false;
    }

    /// <summary>
    ///     Closes the NUI window.
    /// </summary>
    public override void Close()
    {
        if (_player.LoginCreature != null) _player.LoginCreature.OnUnacquireItem -= PreventMunchkins;

        // Raise an event to child windows.
        OnForgeClosing();

        // Raise an event to the system.
        _token.Close();
    }

    /// <summary>
    ///     Gets the NUI window token, creating the window if necessary.
    /// </summary>
    /// <returns>The NUI window token.</returns>
    public override NuiWindowToken Token() => _token;

    private void OnViewUpdated()
    {
        ViewUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void OnForgeClosing()
    {
        ForgeClosing?.Invoke(this, EventArgs.Empty);
    }
}
