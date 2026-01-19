using AmiaReforged.PwEngine.Features.Player.Dashboard.Hide;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard;

public sealed class PlayerDashboardPresenter : ScryPresenter<PlayerDashboardView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    [Inject]
    private PlayerDashboardService DashboardService { get; init; } = null!;

    [Inject]
    private PrayerService PrayerService { get; init; } = null!;

    public PlayerDashboardPresenter(PlayerDashboardView view, NwPlayer player)
    {
        _player = player;
        View = view;
    }

    public override PlayerDashboardView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), null!)
        {
            Geometry = new NuiRect(0f, 50f, 120f, 250f),
            Collapsed = false,
            Transparent = true,
            Resizable = false,
            Closable = false,
            Border = false
        };
    }

    public override void Create()
    {

        if (_window == null)
        {
            _player.SendServerMessage(
                "The dashboard window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
        _token.OnNuiEvent += ProcessEvent;
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click) return;

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
            case "btn_player_tools":
                HandlePlayerToolsButtonClick();
                break;
            case "btn_utilities":
                HandleUtilitiesButtonClick();
                break;
            case "btn_close":
                Close();
                break;
        }
    }

    private void HandleRestButtonClick()
    {
        NwCreature? creature = _player.LoginCreature;

        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
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

        // Raise the close event to notify WindowDirector to properly clean up
        RaiseCloseEvent();

        // Trigger rest action - this will call OnRestStarted again, but AR_RestChoice is now 1
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


        // Create and show the Hide Equipment window
        HideEquipmentView hideView = new();
        HideEquipmentPresenter hidePresenter = new(hideView, _player);
        hidePresenter.Create();
    }

    private void HandleEmotesButtonClick()
    {
        _player.SendServerMessage("Emotes feature - Coming soon!", ColorConstants.Orange);
        // TODO: Implement emotes/animations system
    }

    private void HandlePlayerToolsButtonClick()
    {
        _player.SendServerMessage("Player Tools feature - Coming soon!", ColorConstants.Orange);
        // TODO: Open Player Tools NUI instead of using feat
    }

    private void HandleUtilitiesButtonClick()
    {
        _player.SendServerMessage("Utilities feature - Coming soon!", ColorConstants.Orange);
        // TODO: Implement utilities menu
    }

    public override void UpdateView()
    {
        // Update any dynamic content here in the future
    }

    public override void Close()
    {
        _token.OnNuiEvent -= ProcessEvent;
        _token.Close();
    }
}
