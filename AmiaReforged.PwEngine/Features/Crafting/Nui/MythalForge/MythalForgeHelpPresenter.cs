using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;

/// <summary>
///     Presenter for the Mythal Forge help guide window.
/// </summary>
public sealed class MythalForgeHelpPresenter : ScryPresenter<MythalForgeHelpView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public MythalForgeHelpPresenter(NwPlayer player, MythalForgeHelpView view)
    {
        _player = player;
        View = view;
    }

    public override MythalForgeHelpView View { get; }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.Token != _token) return;
        if (obj.EventType != NuiEventType.Click) return;
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "Forge Help")
        {
            Geometry = new NuiRect(300f, 100f, 650f, 600f),
            Resizable = true,
            Collapsed = false
        };
    }

    public override void UpdateView()
    {
        // No dynamic updates needed for help window
    }

    public override void Create()
    {
        InitBefore();
        _player.TryCreateNuiWindow(_window!, out _token);
    }

    public override void Close()
    {
        _token.Close();
    }
}

