using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities;

public sealed class UtilitiesPresenter : ScryPresenter<UtilitiesView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private float _scaleFactor = 1.0f;

    // Geometry bind to force window position
    private readonly NuiBind<NuiRect> _geometryBind = new("window_geometry");

    // Base window dimensions (at 100% GUI scale)
    private const float BaseWindowX = 25f;
    private const float BaseWindowY = 85f;
    private const float BaseWindowWidth = 220f;
    private const float BaseWindowHeight = 70f;

    [Inject]
    private DevicePropertyService DevicePropertyService { get; init; } = null!;

    public override UtilitiesView View { get; }

    public override NuiWindowToken Token() => _token;

    public UtilitiesPresenter(UtilitiesView view, NwPlayer player)
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
            _player.SendServerMessage("The utilities window could not be created.", ColorConstants.Orange);
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
            case "btn_save":
                HandleSaveCharacter();
                break;
            case "btn_summon":
                HandleSummonOptions();
                break;
            case "btn_self":
                HandleSelfSettings();
                break;
            case "btn_game":
                HandleGameSettings();
                break;
        }
    }

    private void HandleSaveCharacter()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        // Check conditions from AR_ExportPlayer
        if (_player.IsDM || creature.IsDMPossessed)
        {
            _player.SendServerMessage("- DM Avatars cannot be saved. -", ColorConstants.Orange);
            return;
        }

        if (creature.IsResting)
        {
            _player.SendServerMessage("- Resting characters cannot be saved. -", ColorConstants.Orange);
            return;
        }

        // Export the character
        NWScript.ExportSingleCharacter(creature);
        _player.SendServerMessage("- Your character has been saved. -", ColorConstants.Cyan);
    }

    private void HandleSummonOptions()
    {
        // Get WindowDirector from AnvilCore
        WindowDirector? windowDirector = AnvilCore.GetService<WindowDirector>();
        if (windowDirector == null)
        {
            _player.SendServerMessage("Failed to open summon options. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Check if SummonOptions window is already open - if so, close it (toggle)
        if (windowDirector.IsWindowOpen(_player, typeof(SummonOptions.SummonOptionsPresenter)))
        {
            windowDirector.CloseWindow(_player, typeof(SummonOptions.SummonOptionsPresenter));
            return;
        }

        // Create and open SummonOptions window
        SummonOptions.SummonOptionsView summonView = new();
        SummonOptions.SummonOptionsPresenter summonPresenter = new(summonView, _player);
        windowDirector.OpenWindow(summonPresenter);
    }

    private void HandleSelfSettings()
    {
        // Get WindowDirector from AnvilCore
        WindowDirector? windowDirector = AnvilCore.GetService<WindowDirector>();
        if (windowDirector == null)
        {
            _player.SendServerMessage("Failed to open self settings. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Check if SelfSettings window is already open - if so, close it (toggle)
        if (windowDirector.IsWindowOpen(_player, typeof(SelfSettings.SelfSettingsPresenter)))
        {
            windowDirector.CloseWindow(_player, typeof(SelfSettings.SelfSettingsPresenter));
            return;
        }

        // Get the injection service
        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            _player.SendServerMessage("Failed to load self settings. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Create and open SelfSettings window
        SelfSettings.SelfSettingsView selfView = new();
        SelfSettings.SelfSettingsPresenter selfPresenter = new(selfView, _player);
        injector.Inject(selfPresenter);
        windowDirector.OpenWindow(selfPresenter);
    }

    private void HandleGameSettings()
    {
        // Get WindowDirector from AnvilCore
        WindowDirector? windowDirector = AnvilCore.GetService<WindowDirector>();
        if (windowDirector == null)
        {
            _player.SendServerMessage("Failed to open game settings. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Check if GameSettings window is already open - if so, close it (toggle)
        if (windowDirector.IsWindowOpen(_player, typeof(GameSettings.GameSettingsPresenter)))
        {
            windowDirector.CloseWindow(_player, typeof(GameSettings.GameSettingsPresenter));
            return;
        }

        // Get the injection service
        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            _player.SendServerMessage("Failed to load game settings. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Create and open GameSettings window
        GameSettings.GameSettingsView gameView = new();
        GameSettings.GameSettingsPresenter gamePresenter = new(gameView, _player);
        injector.Inject(gamePresenter);
        windowDirector.OpenWindow(gamePresenter);
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
