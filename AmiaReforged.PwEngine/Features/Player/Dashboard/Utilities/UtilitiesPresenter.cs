using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities;

public sealed class UtilitiesPresenter : ScryPresenter<UtilitiesView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override UtilitiesView View { get; }

    public override NuiWindowToken Token() => _token;

    public UtilitiesPresenter(UtilitiesView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), null!)
        {
            Geometry = new NuiRect(25f, 85f, 220f, 70f),
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

        // Check if polymorphed by iterating through active effects
        bool isPolymorphed = false;
        foreach (Effect effect in creature.ActiveEffects)
        {
            if (effect.EffectType == EffectType.Polymorph)
            {
                isPolymorphed = true;
                break;
            }
        }

        if (isPolymorphed)
        {
            _player.SendServerMessage("- Polymorphed characters cannot be saved. -", ColorConstants.Orange);
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
        _player.SendServerMessage("Summon Options coming soon!", ColorConstants.Yellow);
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

        // Create and open SelfSettings window
        SelfSettings.SelfSettingsView selfView = new();
        SelfSettings.SelfSettingsPresenter selfPresenter = new(selfView, _player);
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

        // Create and open GameSettings window
        GameSettings.GameSettingsView gameView = new();
        GameSettings.GameSettingsPresenter gamePresenter = new(gameView, _player);
        windowDirector.OpenWindow(gamePresenter);
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
