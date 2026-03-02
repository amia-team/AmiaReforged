using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.GameSettings;

public sealed class GameSettingsPresenter : ScryPresenter<GameSettingsView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private float _scaleFactor = 1.0f;

    // Geometry bind to force window position
    private readonly NuiBind<NuiRect> _geometryBind = new("window_geometry");

    // Base window dimensions (at 100% GUI scale)
    private const float BaseWindowX = 137f;
    private const float BaseWindowY = 130f;
    private const float BaseWindowWidth = 220f;
    private const float BaseWindowHeight = 70f;

    [Inject]
    private DevicePropertyService DevicePropertyService { get; init; } = null!;

    public override GameSettingsView View { get; }

    public override NuiWindowToken Token() => _token;

    public GameSettingsPresenter(GameSettingsView view, NwPlayer player)
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
            _player.SendServerMessage("The game settings window could not be created.", ColorConstants.Orange);
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

        UpdateXpBlockTooltip();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        // NuiImage elements fire MouseUp events instead of Click
        if (ev.EventType != NuiEventType.Click && ev.EventType != NuiEventType.MouseUp) return;

        switch (ev.ElementId)
        {
            case "btn_xp_block":
                HandleXpBlockToggle();
                break;
            case "btn_emote_symbol":
                HandleEmoteSymbol();
                break;
            case "btn_party_advertiser":
                HandlePartyAdvertiser();
                break;
            case "btn_pvp_tool":
                HandlePvpTool();
                break;
        }
    }

    private void HandleXpBlockToggle()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        // Check current XP block status (ds_xpbl variable)
        int xpBlock = creature.GetObjectVariable<LocalVariableInt>("ds_xpbl").Value;

        if (xpBlock == 1)
        {
            // Remove XP block
            creature.GetObjectVariable<LocalVariableInt>("ds_xpbl").Delete();
            _player.SendServerMessage("XP block removed.", ColorConstants.Cyan);
        }
        else
        {
            // Activate XP block
            creature.GetObjectVariable<LocalVariableInt>("ds_xpbl").Value = 1;
            _player.SendServerMessage("XP block activated.", ColorConstants.Cyan);
        }

        UpdateXpBlockTooltip();
    }

    private void UpdateXpBlockTooltip()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null) return;

        int xpBlock = creature.GetObjectVariable<LocalVariableInt>("ds_xpbl").Value;
        Token().SetBindValue(View.XpBlockTooltip, xpBlock == 1 ? "Remove XP Block" : "Block XP Gain");
    }

    private void HandleEmoteSymbol()
    {
        // Get WindowDirector from AnvilCore
        WindowDirector? windowDirector = AnvilCore.GetService<WindowDirector>();
        if (windowDirector == null)
        {
            _player.SendServerMessage("Failed to open emote symbol menu. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Check if EmoteSymbol window is already open - if so, close it (toggle)
        if (windowDirector.IsWindowOpen(_player, typeof(EmoteSymbolPresenter)))
        {
            windowDirector.CloseWindow(_player, typeof(EmoteSymbolPresenter));
            return;
        }

        // Create and open EmoteSymbol window
        EmoteSymbolView symbolView = new();
        EmoteSymbolPresenter symbolPresenter = new(symbolView, _player);
        windowDirector.OpenWindow(symbolPresenter);
    }

    private void HandlePartyAdvertiser()
    {
        // Get WindowDirector from AnvilCore
        WindowDirector? windowDirector = AnvilCore.GetService<WindowDirector>();
        if (windowDirector == null)
        {
            _player.SendServerMessage("Failed to open party advertiser. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Check if PartyAdvertiser window is already open - if so, close it (toggle)
        if (windowDirector.IsWindowOpen(_player, typeof(PartyAdvertiserPresenter)))
        {
            windowDirector.CloseWindow(_player, typeof(PartyAdvertiserPresenter));
            return;
        }

        // Get the injection service
        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            _player.SendServerMessage("Failed to load party advertiser. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Create and open PartyAdvertiser window
        PartyAdvertiserView partyView = new();
        PartyAdvertiserPresenter partyPresenter = new(partyView, _player);
        injector.Inject(partyPresenter);
        windowDirector.OpenWindow(partyPresenter);
    }

    private void HandlePvpTool()
    {
        // Get WindowDirector from AnvilCore
        WindowDirector? windowDirector = AnvilCore.GetService<WindowDirector>();
        if (windowDirector == null)
        {
            _player.SendServerMessage("Failed to open PvP tool. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Check if PvpTool window is already open - if so, close it (toggle)
        if (windowDirector.IsWindowOpen(_player, typeof(PvpToolPresenter)))
        {
            windowDirector.CloseWindow(_player, typeof(PvpToolPresenter));
            return;
        }

        // Create and open PvpTool window
        PvpToolView pvpView = new();
        PvpToolPresenter pvpPresenter = new(pvpView, _player);
        windowDirector.OpenWindow(pvpPresenter);
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
