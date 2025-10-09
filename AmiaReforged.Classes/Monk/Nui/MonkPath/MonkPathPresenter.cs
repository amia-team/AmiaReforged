using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Nui.MonkPath;

public sealed class MonkPathPresenter : ScryPresenter<MonkPathView>
{
    public delegate void ViewUpdatedEventHandler(MonkPathPresenter sender, MonkPathView senderView);
    public delegate void PathSelectionClosingEventHandler(MonkPathPresenter sender);

    private readonly ConfirmPathView _confirmPathView;
    private readonly NwPlayer _player;
    public override MonkPathView View { get; }
    public override NuiWindowToken Token() => _token;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    private const string WindowTitle = "Choose Your Path of Enlightenment";
    public MonkPathPresenter(MonkPathView toolView, NwPlayer player)
    {
        View = toolView;
        _player = player;

        _confirmPathView = new ConfirmPathView(this, player);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType == NuiEventType.Click)
            HandleButtonClick(eventData);
    }
    public event ViewUpdatedEventHandler? ViewUpdated;
    public event PathSelectionClosingEventHandler? PathSelectionClosing;

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (!Enum.TryParse(eventData.ElementId, out PathType path)) return;

        _token.SetBindValue(View.PathBind, path);

        _confirmPathView.Presenter.Create();

        OnViewUpdated();
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), WindowTitle)
        {
            Geometry = new NuiRect(400f, 400f, 600, 640f)
        };
    }

    public override void Create()
    {
        InitBefore();

        if (_window == null) return;

        _player.TryCreateNuiWindow(_window, out _token);
    }

    private void OnViewUpdated()
    {
        ViewUpdated?.Invoke(this, View);
    }

    public override void Close()
    {
        // Close child windows
        OnPathSelectionClosing();

        _token.Close();
    }

    private void OnPathSelectionClosing()
    {
        PathSelectionClosing?.Invoke(this);
    }
}
