using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.StandaloneWindows;

public sealed class SimplePopupView : ScryView<SimplePopupPresenter>
{
    private readonly string _message;
    public sealed override SimplePopupPresenter Presenter { get; protected set; }

    public SimplePopupView(NwPlayer player, string message, string title)
    {
        _message = message;
        Presenter = new SimplePopupPresenter(player, this, title);
    }

    public override NuiLayout RootLayout()
    {
        NuiColumn popupLayout = new()
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(_message)
                        {
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiButton("OK")
                        {
                            Id = "ok_button",
                        }
                    }
                }
            }
        };
        return popupLayout;
    }
}

public sealed class SimplePopupPresenter : ScryPresenter<SimplePopupView>
{
    private readonly string _title;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly NwPlayer _player;

    public SimplePopupPresenter(NwPlayer player, SimplePopupView view, string title)
    {
        _player = player;
        _title = title;
        View = view;

        NwModule.Instance.OnNuiEvent += HandleOkButton;
    }

    private void HandleOkButton(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click) return;
        if (obj.ElementId != "ok_button") return;

        Close();
    }

    public override NuiWindowToken Token()
    {
        return _token;
    }

    public override SimplePopupView View { get; }

    public override void Initialize()
    {
        _window = new NuiWindow(View.RootLayout(), _title)
        {
            Geometry = new NuiRect(500f, 500f, 400f, 400f)
        };
    }

    public override void UpdateView()
    {
    }

    public override void Create()
    {
        Initialize();
        _player.TryCreateNuiWindow(_window!, out _token);
    }

    public override void Close()
    {
        _token.Close();
    }
}