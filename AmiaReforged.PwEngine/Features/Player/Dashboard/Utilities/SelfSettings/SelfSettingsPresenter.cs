using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.SelfSettings;

public sealed class SelfSettingsPresenter : ScryPresenter<SelfSettingsView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private float _scaleFactor = 1.0f;

    // Geometry bind to force window position
    private readonly NuiBind<NuiRect> _geometryBind = new("window_geometry");

    // Base window dimensions (at 100% GUI scale)
    private const float BaseWindowX = 45f;
    private const float BaseWindowY = 130f;
    private const float BaseWindowWidth = 130f;
    private const float BaseWindowHeight = 70f;

    [Inject]
    private DevicePropertyService DevicePropertyService { get; init; } = null!;

    public override SelfSettingsView View { get; }

    public override NuiWindowToken Token() => _token;

    public SelfSettingsPresenter(SelfSettingsView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override void InitBefore()
    {
        // Get GUI scale and calculate scale factor
        int guiScalePercent = DevicePropertyService.GetGuiScale(_player);
        _scaleFactor = guiScalePercent / 100f;

        // Set the scale factor on the view so it can adjust element sizes
        View.SetScaleFactor(_scaleFactor);

        _window = new NuiWindow(View.RootLayout(), null!)
        {
            Geometry = _geometryBind,
            Transparent = true,
            Resizable = false,
            Closable = false,
            Collapsed = false,
            Border = false
        };
    }

    public override void Create()
    {
        if (_window is null)
        {
            _player.SendServerMessage("The self settings window could not be created.", ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Calculate scaled position - only scale width/height, not X/Y
        // NWN's GUI scaling handles the position automatically
        NuiRect scaledPosition = new(
            BaseWindowX,
            BaseWindowY,
            BaseWindowWidth / _scaleFactor,
            BaseWindowHeight / _scaleFactor
        );

        Token().SetBindValue(_geometryBind, scaledPosition);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        // NuiImage elements fire MouseUp events instead of Click
        if (ev.EventType != NuiEventType.Click && ev.EventType != NuiEventType.MouseUp) return;

        switch (ev.ElementId)
        {
            case "btn_hurt":
                HandleHurtYourself();
                break;
            case "btn_acp":
                HandleAcp();
                break;
        }
    }


    private void HandleHurtYourself()
    {
        // Get WindowDirector from AnvilCore
        WindowDirector? windowDirector = AnvilCore.GetService<WindowDirector>();
        if (windowDirector == null)
        {
            _player.SendServerMessage("Failed to open hurt yourself menu. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Check if HurtYourself window is already open - if so, close it (toggle)
        if (windowDirector.IsWindowOpen(_player, typeof(HurtYourselfPresenter)))
        {
            windowDirector.CloseWindow(_player, typeof(HurtYourselfPresenter));
            return;
        }

        // Create and open HurtYourself window
        HurtYourselfView hurtView = new();
        HurtYourselfPresenter hurtPresenter = new(hurtView, _player);
        windowDirector.OpenWindow(hurtPresenter);
    }

    private void HandleAcp()
    {
        // Get WindowDirector from AnvilCore
        WindowDirector? windowDirector = AnvilCore.GetService<WindowDirector>();
        if (windowDirector == null)
        {
            _player.SendServerMessage("Failed to open ACP menu. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Check if ACP window is already open - if so, close it (toggle)
        if (windowDirector.IsWindowOpen(_player, typeof(AcpPresenter)))
        {
            windowDirector.CloseWindow(_player, typeof(AcpPresenter));
            return;
        }

        // Create and open ACP window
        AcpView acpView = new();
        AcpPresenter acpPresenter = new(acpView, _player);
        windowDirector.OpenWindow(acpPresenter);
    }

    public override void UpdateView()
    {
        // No dynamic updates needed
    }

    public override void Close()
    {
        // Don't call RaiseCloseEvent() here - it causes infinite recursion
        // The WindowDirector handles cleanup when CloseWindow() is called
        _token.Close();
    }
}
