using Anvil.API;
using Anvil.API.Events;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;

/// <summary>
/// Presenter for forge notice popups (item not supported, ownership, etc.).
/// </summary>
public sealed class ForgeNoticePresenter : ScryPresenter<ForgeNoticeView>
{
    private readonly NwPlayer _player;
    private readonly string _title;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public ForgeNoticePresenter(NwPlayer player, ForgeNoticeView view, string title)
    {
        _player = player;
        _title = title;
        View = view;
    }

    public override ForgeNoticeView View { get; }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType == NuiEventType.Click && obj.ElementId == "ok_button")
        {
            Close();
        }
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), _title)
        {
            Geometry = new NuiRect(550f, 400f, 470f, 180f),
            Resizable = false,
            Closable = true
        };
    }

    public override void UpdateView()
    {
        // No dynamic updates needed
    }

    public override void Create()
    {
        InitBefore();
        if (_window != null)
        {
            _player.TryCreateNuiWindow(_window, out _token);
        }
    }

    public override void Close()
    {
        _token.Close();
    }
}
