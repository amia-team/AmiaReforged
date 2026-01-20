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
            Geometry = new NuiRect(25f, 160f, 170f, 70f),
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

        _player.SendServerMessage("ACP menu coming soon!", ColorConstants.Yellow);
        // TODO: Implement ACP modal
    }

    public override void UpdateView()
    {
        // No dynamic updates needed
    }

    public override void Close()
    {
        _token.Close();
    }
}
