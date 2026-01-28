using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.SelfSettings;

public sealed class SelfSettingsPresenter : ScryPresenter<SelfSettingsView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    // Geometry bind to force window position
    private readonly NuiBind<NuiRect> _geometryBind = new("window_geometry");
    private static readonly NuiRect WindowPosition = new(45f, 130f, 170f, 70f);

    public override SelfSettingsView View { get; }

    public override NuiWindowToken Token() => _token;

    public SelfSettingsPresenter(SelfSettingsView view, NwPlayer player)
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
            _player.SendServerMessage("The self settings window could not be created.", ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Force the window position using the bind
        Token().SetBindValue(_geometryBind, WindowPosition);

        UpdateBubbleTooltip();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        // NuiImage elements fire MouseUp events instead of Click
        if (ev.EventType != NuiEventType.Click && ev.EventType != NuiEventType.MouseUp) return;

        switch (ev.ElementId)
        {
            case "btn_bubble":
                HandleCollisionBubbleToggle();
                break;
            case "btn_hurt":
                HandleHurtYourself();
                break;
            case "btn_acp":
                HandleAcp();
                break;
        }
    }

    private void HandleCollisionBubbleToggle()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        // Check if the cutscene ghost effect is already applied
        bool hasGhostEffect = false;
        foreach (Effect effect in creature.ActiveEffects)
        {
            if (effect.EffectType == EffectType.CutsceneGhost)
            {
                hasGhostEffect = true;
                creature.RemoveEffect(effect);
                _player.SendServerMessage("Collision bubble applied.", ColorConstants.Cyan);
                break;
            }
        }

        if (!hasGhostEffect)
        {
            // Apply cutscene ghost effect (removes collision)
            Effect ghostEffect = Effect.CutsceneGhost();
            creature.ApplyEffect(EffectDuration.Permanent, ghostEffect);
            _player.SendServerMessage("Collision bubble removed.", ColorConstants.Cyan);
        }

        UpdateBubbleTooltip();
    }

    private void UpdateBubbleTooltip()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null) return;

        bool hasGhostEffect = false;
        foreach (Effect effect in creature.ActiveEffects)
        {
            if (effect.EffectType == EffectType.CutsceneGhost)
            {
                hasGhostEffect = true;
                break;
            }
        }

        // When CutsceneGhost is active, collision is REMOVED, so tooltip should say "Apply Collision Bubble"
        // When CutsceneGhost is not active, collision exists, so tooltip should say "Remove Collision Bubble"
        Token().SetBindValue(View.BubbleTooltip, hasGhostEffect ? "Apply Collision Bubble" : "Remove Collision Bubble");
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
