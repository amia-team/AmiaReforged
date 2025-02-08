using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui;

public sealed class PlayerToolsWindowPresenter : ScryPresenter<PlayerToolsWindowView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    [Inject] private Lazy<List<IToolWindow>> AvailableWindows { get; init; } = null!;
    [Inject] private Lazy<WindowDirector> WindowDirector { get; init; } = null!;

    private List<IToolWindow> _allWindows = null!;
    private List<IToolWindow>? _visibleWindows;
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public PlayerToolsWindowPresenter(PlayerToolsWindowView view, NwPlayer player)
    {
        _player = player;
        View = view;
    }
    
    public override NuiWindowToken Token()
    {
        return _token;
    }

    public override PlayerToolsWindowView View { get; }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "Player Tools")
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f),
        };
    }

    public override void Create()
    {
        // Create the window if it's null.
        if (_window == null)
        {
            // Try to create the window if it doesn't exist.
            InitBefore();
        }

        // If the window wasn't created, then tell the user we screwed up.
        if (_window == null)
        {
            _player.SendServerMessage("The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
        IEnumerable<IToolWindow> playerToolWindows = AvailableWindows.Value.Where(view => view.ListInPlayerTools);

        _allWindows = playerToolWindows.OrderBy(view => view.Title).ToList();

        RefreshWindowList();
    }

    private void RefreshWindowList()
    {
        string search = Token().GetBindValue(View.Search)!;
        _visibleWindows = _allWindows.Where(view => view.Title.Contains(search!, StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<string> windowNames = _visibleWindows.Select(view => view.Title).ToList();
        Token().SetBindValues(View.WindowNames, windowNames);
        Token().SetBindValue(View.WindowCount, _visibleWindows.Count);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(eventData);
                break;
        }
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId == View.SearchButton.Id)
        {
            RefreshWindowList();
        }
        else if (eventData.ElementId == View.OpenWindowButton.Id && _visibleWindows != null &&
                 eventData.ArrayIndex >= 0 && eventData.ArrayIndex < _visibleWindows.Count)
        {
            Log.Info("Opening window from player tools.");
            IToolWindow window = _visibleWindows[eventData.ArrayIndex];
            IScryPresenter toolWindow = window.MakeWindow(Token().Player);
            WindowDirector.Value.OpenWindow(toolWindow);
        }
    }

    public override void Close()
    {
        _visibleWindows = null;
    }
}