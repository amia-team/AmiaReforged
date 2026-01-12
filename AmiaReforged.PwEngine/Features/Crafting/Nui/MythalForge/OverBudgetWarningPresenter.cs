using Anvil.API;
using Anvil.API.Events;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;

/// <summary>
/// Presenter for the over-budget warning popup in Mythal Forge.
/// </summary>
public sealed class OverBudgetWarningPresenter : ScryPresenter<OverBudgetWarningView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public OverBudgetWarningPresenter(NwPlayer player, OverBudgetWarningView view)
    {
        _player = player;
        View = view;
    }

    public override OverBudgetWarningView View { get; }

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
        _window = new NuiWindow(View.RootLayout(), "Mythal Forge: WARNING!!!!")
        {
            Geometry = new NuiRect(550f, 400f, 470f, 210f),
            Resizable = false,
            Closable = false
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

