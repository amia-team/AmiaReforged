using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.GuardSpawner;

/// <summary>
/// Presenter for the Guard Spawner DM tool. Handles user input and coordinates between View and Model.
/// </summary>
public sealed class GuardSpawnerPresenter : ScryPresenter<GuardSpawnerView>
{
    private readonly GuardSpawnerView _view;
    private readonly NwPlayer _player;
    private readonly GuardSpawnerModel _model;

    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override GuardSpawnerView View => _view;

    public GuardSpawnerPresenter(GuardSpawnerView view, NwPlayer player)
    {
        _view = view;
        _player = player;
        _model = new GuardSpawnerModel(player);

        // Subscribe to model events
        _model.OnModelUpdated += RefreshView;
        _model.OnWidgetLoaded += OnWidgetLoaded;
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(100f, 100f, GuardSpawnerView.GetWindowWidth(), GuardSpawnerView.GetWindowHeight()),
            Resizable = true
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            InitBefore();
        }

        if (_window == null)
        {
            _player.SendServerMessage(
                "The Guard Spawner window could not be created. Please report this to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Initialize bindings
        InitializeBindings();

        // Set up watch on settlement combo for dynamic creature list updates
        Token().SetBindWatch(View.SelectedSettlementIndex, true);
    }

    private void InitializeBindings()
    {
        // Settlement dropdown
        Token().SetBindValue(View.SettlementEntries, GuardSpawnerModel.GetSettlementOptions());
        Token().SetBindValue(View.SelectedSettlementIndex, 0);

        // Creature dropdown (empty until settlement selected)
        Token().SetBindValue(View.CreatureEntries, _model.GetCreatureOptions());
        Token().SetBindValue(View.SelectedCreatureIndex, 0);

        // Guard list
        Token().SetBindValues(View.GuardNames, new List<string>());
        Token().SetBindValue(View.GuardCount, 0);

        // Inputs
        Token().SetBindValue(View.QuantityText, "1");
        Token().SetBindValue(View.WidgetNameText, "");

        // Beacon button
        UpdateBeaconButton();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleClick(eventData);
                break;
            case NuiEventType.Watch:
                HandleWatch(eventData);
                break;
        }
    }

    private void HandleClick(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.ElementId)
        {
            case "btn_select_widget":
                _model.EnterWidgetTargetMode();
                break;

            case "btn_add_creature":
                HandleAddCreature();
                break;

            case "btn_remove_guard":
                HandleRemoveGuard(eventData.ArrayIndex);
                break;

            case "btn_beacon":
                HandleBeaconToggle();
                break;

            case "btn_save":
                HandleSave();
                break;

            case "btn_reset":
                HandleReset();
                break;
        }
    }

    private void HandleWatch(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.ElementId)
        {
            case "combo_settlement":
            case "selected_settlement":
                int selectedIndex = Token().GetBindValue(View.SelectedSettlementIndex);
                _model.SetSelectedSettlement(selectedIndex);
                RefreshCreatureDropdown();
                break;
        }
    }

    private void HandleAddCreature()
    {
        int creatureIndex = Token().GetBindValue(View.SelectedCreatureIndex);
        _model.AddCreatureByIndex(creatureIndex);
    }

    private void HandleRemoveGuard(int index)
    {
        _model.RemoveCreatureAt(index);
    }

    private void HandleBeaconToggle()
    {
        _model.ToggleBeacon();
        UpdateBeaconButton();
    }

    private async void HandleSave()
    {
        // Read current values from UI
        string qtyText = Token().GetBindValue(View.QuantityText) ?? "1";
        if (!int.TryParse(qtyText, out int qty) || qty < 1 || qty > 8)
        {
            _player.SendServerMessage("Quantity must be a number between 1 and 8.", ColorConstants.Orange);
            return;
        }
        _model.Quantity = qty;

        string widgetName = Token().GetBindValue(View.WidgetNameText) ?? "";
        _model.WidgetName = widgetName;

        // Build the widget
        bool success = await _model.BuildWidget();

        if (success)
        {
            // Reset after successful creation
            _model.Reset();
            InitializeBindings();
        }
    }

    private void HandleReset()
    {
        _model.Reset();
        InitializeBindings();
        _player.SendServerMessage("Guard Spawner reset.", ColorConstants.Cyan);
    }

    private void RefreshView()
    {
        RefreshGuardList();
        UpdateBeaconButton();
    }

    private void OnWidgetLoaded()
    {
        RefreshView();

        // Update quantity and name fields from loaded widget
        Token().SetBindValue(View.QuantityText, _model.Quantity.ToString());
        Token().SetBindValue(View.WidgetNameText, _model.WidgetName);
    }

    private void RefreshCreatureDropdown()
    {
        var options = _model.GetCreatureOptions();
        Token().SetBindValue(View.CreatureEntries, options);
        Token().SetBindValue(View.SelectedCreatureIndex, 0);
    }

    private void RefreshGuardList()
    {
        var guardNames = _model.ChosenCreatures.Select(c => c.DisplayName).ToList();
        Token().SetBindValues(View.GuardNames, guardNames);
        Token().SetBindValue(View.GuardCount, guardNames.Count);
    }

    private void UpdateBeaconButton()
    {
        if (_model.IsBeaconMode)
        {
            Token().SetBindValue(View.BeaconButtonLabel, "Beacon: ON");
            Token().SetBindValue(View.BeaconTooltip, "Click to disable Beacon Alliance settings. Currently: qty=4, all Beacon settlements included.");
        }
        else
        {
            Token().SetBindValue(View.BeaconButtonLabel, "Beacon: OFF");
            Token().SetBindValue(View.BeaconTooltip, "Click to enable Beacon Alliance settings (qty=4, all Beacon settlements).");
        }
    }

    public override void Close()
    {
        // Don't call RaiseCloseEvent() here - it causes infinite recursion
        // The WindowDirector handles cleanup when CloseWindow() is called
        Token().Close();
    }
}




