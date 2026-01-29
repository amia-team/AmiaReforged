using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.GameSettings;

public sealed class GameSettingsPresenter : ScryPresenter<GameSettingsView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    // Geometry bind to force window position
    private readonly NuiBind<NuiRect> _geometryBind = new("window_geometry");
    private static readonly NuiRect WindowPosition = new(177f, 130f, 220f, 70f);

    public override GameSettingsView View { get; }

    public override NuiWindowToken Token() => _token;

    public GameSettingsPresenter(GameSettingsView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override void InitBefore()
    {
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

        // Force the window position using the bind
        Token().SetBindValue(_geometryBind, WindowPosition);

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

        // Create and open PartyAdvertiser window
        PartyAdvertiserView partyView = new();
        PartyAdvertiserPresenter partyPresenter = new(partyView, _player);
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
