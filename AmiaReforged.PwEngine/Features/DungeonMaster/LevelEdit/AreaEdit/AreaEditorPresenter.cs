using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.DungeonMaster.AreaEdit;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.LevelEdit.AreaEdit;

/// <summary>
/// Refactored presenter - delegates responsibilities to specialized handlers
/// </summary>
public sealed class AreaEditorPresenter : ScryPresenter<AreaEditorView>
{
    public override AreaEditorView View { get; }

    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly NwPlayer _player;

    // State
    private readonly AreaEditorState _state = new();
    private readonly TileSelection _tileSelection = new();

    // Handlers - lazy initialized after token is available
    private AreaSelectionHandler? _areaHandler;
    private AreaSettingsManager? _settingsManager;
    private TileEditorHandler? _tileHandler;
    private InstanceManagerHandler? _instanceHandler;

    [Inject] private Lazy<DmAreaService>? AreaService { get; init; }
    [Inject] private Lazy<WindowDirector>? WindowDirector { get; init; }

    public AreaEditorPresenter(AreaEditorView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f)
        };
    }

    public override void Create()
    {
        if (_window is null) InitBefore();

        if (_window is null)
        {
            _player.SendServerMessage(
                "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
        Token().SetBindWatch(View.SearchBind, true);
        InitializeHandlers();
        _areaHandler!.RefreshAreaList();
    }

    private void InitializeHandlers()
    {
        _areaHandler = new AreaSelectionHandler(_player, _token, View, _state);
        _settingsManager = new AreaSettingsManager(_token, View);
        _tileHandler = new TileEditorHandler(_player, _token, View, _tileSelection);
        _instanceHandler = new InstanceManagerHandler(
            _player,
            _token,
            View,
            _state,
            AreaService!.Value,
            WindowDirector?.Value);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        // Handle search filter changes
        if (obj.EventType == NuiEventType.Watch && obj.ElementId == View.SearchBind.Key)
        {
            string search = _token.GetBindValue(View.SearchBind) ?? string.Empty;
            _areaHandler?.UpdateSearchFilter(search);
            return;
        }

        // Handle other watch events
        if (obj.EventType == NuiEventType.Watch)
        {
            return;
        }

        // Handle button clicks
        if (obj.EventType == NuiEventType.Click)
        {
            HandleButtonClick(obj);
        }
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent evt)
    {
        // Area Selection
        if (evt.ElementId == View.PickCurrentAreaButton.Id)
        {
            _areaHandler?.SelectCurrentArea();
            LoadSelectedAreaToUi();
            _instanceHandler?.RefreshInstanceList();
            return;
        }

        if (evt.ElementId == "btn_pick_row")
        {
            _areaHandler?.SelectAreaByIndex(evt.ArrayIndex);
            LoadSelectedAreaToUi();
            _instanceHandler?.RefreshInstanceList();
            return;
        }

        if (evt.ElementId == View.ReloadCurrentAreaButton.Id)
        {
            _areaHandler?.ReloadSelectedArea();
            return;
        }

        // Area Settings
        if (evt.ElementId == View.SaveSettingsButton.Id)
        {
            SaveSettingsFromUi();
            return;
        }

        // Instance Management
        if (evt.ElementId == View.SaveNewInstanceButton.Id)
        {
            _instanceHandler?.SaveInstance();
            return;
        }

        if (evt.ElementId == "btn_load_var")
        {
            _instanceHandler?.LoadInstance(evt.ArrayIndex);
            _instanceHandler?.RefreshInstanceList();
            return;
        }

        if (evt.ElementId == "btn_delete_var")
        {
            _instanceHandler?.DeleteInstance(evt.ArrayIndex);
            return;
        }

        // Tile Editing
        if (evt.ElementId == View.PickATileButton.Id)
        {
            _tileHandler?.StartTilePicker(_state.SelectedArea);
            return;
        }

        if (evt.ElementId == View.RotateOrientationClockwise.Id)
        {
            _tileHandler?.RotateClockwise();
            return;
        }

        if (evt.ElementId == View.RotateOrientationCounter.Id)
        {
            _tileHandler?.RotateCounterClockwise();
            return;
        }

        if (evt.ElementId == View.SaveTileButton.Id)
        {
            _tileHandler?.ApplyCurrentChanges();
            return;
        }

        if (evt.ElementId == View.PickNorthTile.Id)
        {
            _tileHandler?.PickNeighbor(TileEditorHandler.Direction.North);
            return;
        }

        if (evt.ElementId == View.PickLeftTile.Id)
        {
            _tileHandler?.PickNeighbor(TileEditorHandler.Direction.West);
            return;
        }

        if (evt.ElementId == View.PickRightTile.Id)
        {
            _tileHandler?.PickNeighbor(TileEditorHandler.Direction.East);
            return;
        }

        if (evt.ElementId == View.PickSouthTile.Id)
        {
            _tileHandler?.PickNeighbor(TileEditorHandler.Direction.South);
            return;
        }
    }

    private void LoadSelectedAreaToUi()
    {
        if (_state.SelectedArea is null || _settingsManager is null) return;
        _settingsManager.LoadToUi(_state.SelectedArea);
    }

    private void SaveSettingsFromUi()
    {
        if (_state.SelectedArea is null || _settingsManager is null) return;

        AreaSettings settings = _settingsManager.LoadFromUi();
        settings.ApplyToArea(_state.SelectedArea);
    }

    public override void Close()
    {
        // Cleanup if needed
    }
}
