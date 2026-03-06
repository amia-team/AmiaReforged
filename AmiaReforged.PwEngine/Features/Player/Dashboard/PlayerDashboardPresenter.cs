﻿using AmiaReforged.PwEngine.Features.Player.Dashboard.Emotes;
using AmiaReforged.PwEngine.Features.Player.Dashboard.Hide;
using AmiaReforged.PwEngine.Features.Player.Dashboard.Pray;
using AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard;

public sealed class PlayerDashboardPresenter : ScryPresenter<PlayerDashboardView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private float _scaleFactor = 1.0f;

    // Geometry bind to force window position
    private readonly NuiBind<NuiRect> _geometryBind = new("window_geometry");

    // Base dashboard dimensions (at 100% GUI scale)
    private const float BaseDashboardX = 0f;
    private const float BaseDashboardY = 40f;
    private const float BaseDashboardWidth = 320f;
    private const float BaseDashboardHeight = 80f;

    [Inject]
    private PlayerDashboardService DashboardService { get; init; } = null!;

    [Inject]
    private DevicePropertyService DevicePropertyService { get; init; } = null!;

    [Inject]
    private PrayerService PrayerService { get; init; } = null!;

    [Inject]
    private Lazy<WindowDirector> WindowDirector { get; init; } = null!;

    public PlayerDashboardPresenter(PlayerDashboardView view, NwPlayer player)
    {
        _player = player;
        View = view;
    }

    public override PlayerDashboardView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        // Get GUI scale and calculate scale factor before building the layout
        // This ensures the View uses the correct sizes when RootLayout() is called
        // GUI scale is returned as percentage (100 = 100%, 150 = 150%)
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
        // WindowDirector calls InitBefore() before calling Create(), so _window should already exist
        if (_window == null)
        {
            _player.SendServerMessage(
                "The dashboard window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Calculate the scaled dashboard position using the scale factor from InitBefore()
        // We divide by scale factor to compensate - when GUI is scaled up 1.5x,
        // we make our window 1/1.5 = 0.67x smaller so it appears at normal size
        NuiRect scaledPosition = new(
            BaseDashboardX / _scaleFactor,
            BaseDashboardY / _scaleFactor,
            BaseDashboardWidth / _scaleFactor,
            BaseDashboardHeight / _scaleFactor
        );

        // Force the window position using the bind
        Token().SetBindValue(_geometryBind, scaledPosition);

        // Don't subscribe to OnNuiEvent here - WindowDirector.HandleNuiEvents already handles this
        // and calls presenter.ProcessEvent(obj) when events occur
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        // NuiImage elements fire MouseUp events instead of Click
        if (obj.EventType != NuiEventType.Click && obj.EventType != NuiEventType.MouseUp) return;

        switch (obj.ElementId)
        {
            case "btn_rest":
                HandleRestButtonClick();
                break;
            case "btn_pray":
                HandlePrayButtonClick();
                break;
            case "btn_hide":
                HandleHideButtonClick();
                break;
            case "btn_emotes":
                HandleEmotesButtonClick();
                break;
            case "btn_bubble":
                HandleCollisionBubbleToggle();
                break;
            case "btn_player_tools":
                HandlePlayerToolsButtonClick();
                break;
            case "btn_utilities":
                HandleUtilitiesButtonClick();
                break;
            case "btn_close":
                RaiseCloseEvent();
                break;
        }
    }

    private void HandleRestButtonClick()
    {
        NwCreature? creature = _player.LoginCreature;

        if (creature == null)
        {
            return;
        }

        // Insta rest on test and dev servers
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (environment != "live")
        {
            creature.ForceRest();
            _player.FloatingTextString("Test Insta Rest, Be Refreshed!", false, false);
            return;
        }

        // Check if on rest cooldown
        int cooldownRemaining = DashboardService.GetRestCooldownRemaining(creature);

        if (cooldownRemaining > 0)
        {
            int minutes = cooldownRemaining / 60;
            string timeString = minutes > 1 ? $"{minutes} minutes" : $"{minutes} minute";
            _player.SendServerMessage($"You must wait up to {timeString} before resting again.", ColorConstants.Orange);
            return; // Don't close dashboard, just show message
        }

        // Set AR_RestChoice to 1 so the rest system knows this is a player-initiated rest
        NWScript.SetLocalInt(creature, sVarName: "AR_RestChoice", 1);
        int checkValue = NWScript.GetLocalInt(creature, "AR_RestChoice");

        // Clear action queue and trigger rest
        // Dashboard stays open so player can continue to use it during rest
        creature.ClearActionQueue();
        creature.ActionRest();

        // Check for death from debuffs (from ca_rest_rest script)
        if (creature.HP < -9)
        {
            if (creature.IsPlayerControlled)
            {
                NWScript.ExecuteScript("mod_pla_death", creature);
            }
        }
    }

    private void HandlePrayButtonClick()
    {
        NwCreature? creature = _player.ControlledCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        // Call the prayer service
        PrayerService.PrayFromDashboard(_player, creature);
    }

    private void HandleHideButtonClick()
    {
        if (_player.LoginCreature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        // Check if Hide Equipment window is already open - if so, close it (toggle behavior)
        if (WindowDirector.Value.IsWindowOpen(_player, typeof(HideEquipmentPresenter)))
        {
            WindowDirector.Value.CloseWindow(_player, typeof(HideEquipmentPresenter));
            return;
        }

        // Get the injection service
        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            _player.SendServerMessage("Failed to load the hide equipment window. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Create the Hide Equipment window and let WindowDirector manage it
        HideEquipmentView hideView = new();
        HideEquipmentPresenter hidePresenter = new(hideView, _player);
        injector.Inject(hidePresenter);
        WindowDirector.Value.OpenWindow(hidePresenter);
    }

    private void HandleEmotesButtonClick()
    {
        if (_player.LoginCreature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        // Check if Emotes window is already open - if so, close it (toggle behavior)
        if (WindowDirector.Value.IsWindowOpen(_player, typeof(EmotesPresenter)))
        {
            WindowDirector.Value.CloseWindow(_player, typeof(EmotesPresenter));
            return;
        }

        // Get the injection service
        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            _player.SendServerMessage(
                "Failed to load the emotes window due to missing DI container. Screenshot this and report it as a bug.",
                ColorConstants.Red);
            return;
        }

        // Create the Emotes window and inject dependencies before opening
        EmotesView emotesView = new();
        EmotesPresenter emotesPresenter = new(emotesView, _player);
        injector.Inject(emotesPresenter);
        WindowDirector.Value.OpenWindow(emotesPresenter);
    }

    private void HandlePlayerToolsButtonClick()
    {
        // Check if Player Tools window is already open - if so, close it (toggle behavior)
        if (WindowDirector.Value.IsWindowOpen(_player, typeof(PlayerToolsWindowPresenter)))
        {
            WindowDirector.Value.CloseWindow(_player, typeof(PlayerToolsWindowPresenter));
            return;
        }

        // Get the injection service
        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            _player.SendServerMessage(
                "Failed to load the player tools due to missing DI container. Screenshot this and report it as a bug.",
                ColorConstants.Red);
            return;
        }

        // Create the Player Tools window
        PlayerToolsWindowView window = new(_player);
        PlayerToolsWindowPresenter presenter = window.Presenter;

        // Inject dependencies and open the window
        injector.Inject(presenter);
        WindowDirector.Value.OpenWindow(presenter);
    }

    private void HandleUtilitiesButtonClick()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        // Check if Utilities window is already open - if so, close it (toggle behavior)
        if (WindowDirector.Value.IsWindowOpen(_player, typeof(Utilities.UtilitiesPresenter)))
        {
            WindowDirector.Value.CloseWindow(_player, typeof(Utilities.UtilitiesPresenter));
            return;
        }

        // Get the injection service
        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            _player.SendServerMessage("Failed to load the utilities window. Please report this bug.", ColorConstants.Red);
            return;
        }

        // Create the Utilities window
        Utilities.UtilitiesView utilitiesView = new();
        Utilities.UtilitiesPresenter utilitiesPresenter = new(utilitiesView, _player);
        injector.Inject(utilitiesPresenter);
        WindowDirector.Value.OpenWindow(utilitiesPresenter);
    }

    private void HandleCollisionBubbleToggle()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        // Get PC Key to save preference
        NwItem? pcKey = creature.Inventory.Items.FirstOrDefault(i => i.ResRef == "ds_pckey");

        // Check if the cutscene ghost effect is already applied
        bool hasGhostEffect = false;
        foreach (Effect effect in creature.ActiveEffects)
        {
            if (effect.EffectType == EffectType.CutsceneGhost)
            {
                hasGhostEffect = true;
                creature.RemoveEffect(effect);
                _player.SendServerMessage("Collision bubble applied.", ColorConstants.Cyan);

                // Save preference: 0 means bubble is ON (ghost effect removed)
                if (pcKey != null)
                {
                    pcKey.GetObjectVariable<LocalVariableInt>("EffectGhost").Value = 0;
                }
                break;
            }
        }

        if (!hasGhostEffect)
        {
            // Apply cutscene ghost effect (removes collision)
            Effect ghostEffect = Effect.CutsceneGhost();
            creature.ApplyEffect(EffectDuration.Permanent, ghostEffect);
            _player.SendServerMessage("Collision bubble removed.", ColorConstants.Cyan);

            // Save preference: 1 means bubble is OFF (ghost effect applied)
            if (pcKey != null)
            {
                pcKey.GetObjectVariable<LocalVariableInt>("EffectGhost").Value = 1;
            }
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

    public override void UpdateView()
    {
        UpdateBubbleTooltip();
    }

    public override void Close()
    {
        // Don't call RaiseCloseEvent() here - it causes infinite recursion
        // The WindowDirector handles cleanup when CloseWindow() is called
        // Sub-menu closing is handled by PlayerDashboardService.CloseAllDashboardSubMenus()
        _token.Close();
    }
}
