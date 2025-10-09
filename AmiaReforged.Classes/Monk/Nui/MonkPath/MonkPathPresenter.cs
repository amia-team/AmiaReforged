using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Nui.MonkPath;

public sealed class MonkPathPresenter(MonkPathView view, NwPlayer player) : ScryPresenter<MonkPathView>
{
    private const string WindowTitle = "Choose Your Path of Enlightenment";
    public override MonkPathView View { get; } = view;
    public override NuiWindowToken Token() => _token;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType == NuiEventType.Click)
            HandleButtonClick(eventData);
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (!Enum.TryParse(eventData.ElementId, out PathType pathType)) return;

        switch (pathType)
        {
            case PathType.CrashingMeteor:
                break;
            case PathType.SwingingCenser:
                break;
            case PathType.FloatingLeaf:
                break;
            case PathType.FickleStrand:
                break;
            case PathType.IroncladBull:
                break;
            case PathType.SplinteredChalice:
                break;
            case PathType.EchoingValley:
                break;
        }
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), WindowTitle)
        {
            Geometry = new NuiRect(400f, 400f, 1200f, 640f)
        };
    }

    public override void Create()
    {
        InitBefore();

        if (_window == null) return;

        player.TryCreateNuiWindow(_window, out _token);

        UpdateView();
    }

    public override void Close()
    {
        _token.Close();
    }
}
