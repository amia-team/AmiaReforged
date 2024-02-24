using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.UI.PlayerTools;

public sealed class PlayerToolsWindowController : WindowController<PlayerToolsWindowView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    [Inject] private Lazy<IEnumerable<IWindowView>> AvailableWindows { get; init; }
    [Inject] private Lazy<WindowManager> WindowManager { get; init; }

    private List<IWindowView> _allWindows;
    private List<IWindowView>? _visibleWindows;


    public override void Init()
    {
        IEnumerable<IWindowView> playerToolWindows = AvailableWindows.Value.Where(view => view.ListInPlayerTools);

        _allWindows = playerToolWindows.OrderBy(view => view.Title).ToList();

        RefreshWindowList();
    }

    private void RefreshWindowList()
    {
        string search = Token.GetBindValue(View.Search)!;
        _visibleWindows = _allWindows.Where(view => view.Title.Contains(search!, StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<string> windowNames = _visibleWindows.Select(view => view.Title).ToList();
        Token.SetBindValues(View.WindowNames, windowNames);
        Token.SetBindValue(View.WindowCount, _visibleWindows.Count);
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
            IWindowView windowView = _visibleWindows[eventData.ArrayIndex];
            WindowManager.Value.OpenWindow(Token.Player, windowView);
        }
    }

    protected override void OnClose()
    {
        _visibleWindows = null;
    }
}