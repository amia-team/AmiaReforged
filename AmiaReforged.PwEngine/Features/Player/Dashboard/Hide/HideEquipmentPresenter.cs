using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Hide;

public sealed class HideEquipmentPresenter : ScryPresenter<HideEquipmentView>
{
    public override HideEquipmentView View { get; }

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override NuiWindowToken Token() => _token;

    public HideEquipmentPresenter(HideEquipmentView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), null!)
        {
            Geometry = new NuiRect(115f, 110f, 150f, 120f),
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
        if (_window is null)
        {
            _player.SendServerMessage("The window could not be created.", ColorConstants.Orange);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
            return;

        // Don't subscribe to OnNuiEvent here - WindowDirector.HandleNuiEvents already handles this
        // and calls presenter.ProcessEvent(obj) when events occur

        UpdateButtonStates();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_toggle_helmet":
                ToggleHelmetVisibility();
                break;

            case "btn_toggle_shield":
                ToggleShieldVisibility();
                break;
            case "btn_toggle_cloak":
                ToggleCloakVisibility();
                break;
        }
    }

    private void ToggleHelmetVisibility()
    {
        if (_player.LoginCreature == null)
        {
            _player.SendServerMessage("No character available.", ColorConstants.Orange);
            return;
        }

        NwItem? helmet = _player.LoginCreature.GetItemInSlot(InventorySlot.Head);

        if (helmet == null)
        {
            _player.SendServerMessage("You don't have a helmet equipped.", ColorConstants.Orange);
            return;
        }

        // Toggle the hidden state
        bool isCurrentlyHidden = helmet.HiddenWhenEquipped == 1;
        helmet.HiddenWhenEquipped = isCurrentlyHidden ? 0 : 1;

        string message = isCurrentlyHidden ? "Helmet is now visible." : "Helmet is now hidden.";
        _player.SendServerMessage(message, ColorConstants.Cyan);

        UpdateButtonStates();
    }

    private void ToggleCloakVisibility()
    {
        if (_player.LoginCreature == null)
        {
            _player.SendServerMessage("No character available.", ColorConstants.Orange);
            return;
        }

        NwItem? cloak = _player.LoginCreature.GetItemInSlot(InventorySlot.Cloak);

        if (cloak == null)
        {
            _player.SendServerMessage("You don't have a cloak equipped.", ColorConstants.Orange);
            return;
        }

        // Toggle the hidden state
        bool isCurrentlyHidden = cloak.HiddenWhenEquipped == 1;
        cloak.HiddenWhenEquipped = isCurrentlyHidden ? 0 : 1;

        string message = isCurrentlyHidden ? "Cloak is now visible." : "Cloak is now hidden.";
        _player.SendServerMessage(message, ColorConstants.Cyan);

        UpdateButtonStates();
    }

    private void ToggleShieldVisibility()
    {
        if (_player.LoginCreature == null)
        {
            _player.SendServerMessage("No character available.", ColorConstants.Orange);
            return;
        }

        NwItem? shield = _player.LoginCreature.GetItemInSlot(InventorySlot.LeftHand);

        if (shield == null)
        {
            _player.SendServerMessage("You don't have a shield equipped.", ColorConstants.Orange);
            return;
        }

        // Check if it's actually a shield (base item category)
        if (shield.BaseItem.Category != BaseItemCategory.Shield)
        {
            _player.SendServerMessage("You don't have a shield equipped in your off-hand.", ColorConstants.Orange);
            return;
        }

        // Toggle the hidden state
        bool isCurrentlyHidden = shield.HiddenWhenEquipped == 1;
        shield.HiddenWhenEquipped = isCurrentlyHidden ? 0 : 1;

        string message = isCurrentlyHidden ? "Shield is now visible." : "Shield is now hidden.";
        _player.SendServerMessage(message, ColorConstants.Cyan);

        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        if (_player.LoginCreature == null)
        {
            Token().SetBindValue(View.HelmetButtonLabel, "Hide Helmet");
            Token().SetBindValue(View.ShieldButtonLabel, "Hide Shield");
            Token().SetBindValue(View.CloakButtonLabel, "Hide Cloak");
            Token().SetBindValue(View.HelmetButtonEnabled, false);
            Token().SetBindValue(View.ShieldButtonEnabled, false);
            return;
        }

        // Update helmet button
        NwItem? helmet = _player.LoginCreature.GetItemInSlot(InventorySlot.Head);
        if (helmet != null)
        {
            bool isHelmetHidden = helmet.HiddenWhenEquipped == 1;
            Token().SetBindValue(View.HelmetButtonLabel, isHelmetHidden ? "Show Helmet" : "Hide Helmet");
            Token().SetBindValue(View.HelmetButtonEnabled, true);
        }
        else
        {
            Token().SetBindValue(View.HelmetButtonLabel, "Hide Helmet");
            Token().SetBindValue(View.HelmetButtonEnabled, false);
        }

        // Update cloak button
        NwItem? cloak = _player.LoginCreature.GetItemInSlot(InventorySlot.Cloak);
        if (cloak != null)
        {
            bool isCloakHidden = cloak.HiddenWhenEquipped == 1;
            Token().SetBindValue(View.CloakButtonLabel, isCloakHidden ? "Show Cloak" : "Hide Cloak");
            Token().SetBindValue(View.CloakButtonEnabled, true);
        }
        else
        {
            Token().SetBindValue(View.CloakButtonLabel, "Hide Cloak");
            Token().SetBindValue(View.CloakButtonEnabled, false);
        }

        // Update shield button
        NwItem? shield = _player.LoginCreature.GetItemInSlot(InventorySlot.LeftHand);
        bool hasShield = shield?.BaseItem.Category == BaseItemCategory.Shield;

        if (hasShield)
        {
            bool isShieldHidden = shield!.HiddenWhenEquipped == 1;
            Token().SetBindValue(View.ShieldButtonLabel, isShieldHidden ? "Show Shield" : "Hide Shield");
            Token().SetBindValue(View.ShieldButtonEnabled, true);
        }
        else
        {
            Token().SetBindValue(View.ShieldButtonLabel, "Hide Shield");
            Token().SetBindValue(View.ShieldButtonEnabled, false);
        }
    }

    public override void Close()
    {
        // WindowDirector handles event routing, so we don't need to unsubscribe
        _token.Close();
    }
}
