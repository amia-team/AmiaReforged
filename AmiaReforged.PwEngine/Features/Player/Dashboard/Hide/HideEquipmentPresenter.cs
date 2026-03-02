using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Hide;

public sealed class HideEquipmentPresenter : ScryPresenter<HideEquipmentView>
{
    public override HideEquipmentView View { get; }

    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private float _scaleFactor = 1.0f;

    // Geometry bind to force window position
    private readonly NuiBind<NuiRect> _geometryBind = new("window_geometry");

    // Base window dimensions (at 100% GUI scale)
    private const float BaseWindowX = 25f;
    private const float BaseWindowY = 85f;
    private const float BaseWindowWidth = 170f;
    private const float BaseWindowHeight = 70f;

    [Inject]
    private DevicePropertyService DevicePropertyService { get; init; } = null!;

    public override NuiWindowToken Token() => _token;

    public HideEquipmentPresenter(HideEquipmentView view, NwPlayer player)
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
        // WindowDirector calls InitBefore() before calling Create(), so _window should already exist
        if (_window is null)
        {
            _player.SendServerMessage("The window could not be created.", ColorConstants.Orange);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
            return;

        // Calculate scaled position - only scale width/height, not X/Y
        // NWN's GUI scaling handles the position automatically
        NuiRect scaledPosition = new(
            BaseWindowX,
            BaseWindowY,
            BaseWindowWidth / _scaleFactor,
            BaseWindowHeight / _scaleFactor
        );

        Token().SetBindValue(_geometryBind, scaledPosition);

        // Don't subscribe to OnNuiEvent here - WindowDirector.HandleNuiEvents already handles this
        // and calls presenter.ProcessEvent(obj) when events occur

        UpdateButtonStates();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        // NuiImage elements fire MouseUp events instead of Click
        if (ev.EventType != NuiEventType.Click && ev.EventType != NuiEventType.MouseUp) return;

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
            Token().SetBindValue(View.HelmetTooltip, "Hide/Show Helmet");
            Token().SetBindValue(View.ShieldTooltip, "Hide/Show Shield");
            Token().SetBindValue(View.CloakTooltip, "Hide/Show Cloak");
            return;
        }

        // Update helmet tooltip
        NwItem? helmet = _player.LoginCreature.GetItemInSlot(InventorySlot.Head);
        if (helmet != null)
        {
            bool isHelmetHidden = helmet.HiddenWhenEquipped == 1;
            Token().SetBindValue(View.HelmetTooltip, isHelmetHidden ? "Show Helmet" : "Hide Helmet");
        }
        else
        {
            Token().SetBindValue(View.HelmetTooltip, "No Helmet Equipped");
        }

        // Update cloak tooltip
        NwItem? cloak = _player.LoginCreature.GetItemInSlot(InventorySlot.Cloak);
        if (cloak != null)
        {
            bool isCloakHidden = cloak.HiddenWhenEquipped == 1;
            Token().SetBindValue(View.CloakTooltip, isCloakHidden ? "Show Cloak" : "Hide Cloak");
        }
        else
        {
            Token().SetBindValue(View.CloakTooltip, "No Cloak Equipped");
        }

        // Update shield tooltip
        NwItem? shield = _player.LoginCreature.GetItemInSlot(InventorySlot.LeftHand);
        bool hasShield = shield?.BaseItem.Category == BaseItemCategory.Shield;

        if (hasShield)
        {
            bool isShieldHidden = shield!.HiddenWhenEquipped == 1;
            Token().SetBindValue(View.ShieldTooltip, isShieldHidden ? "Show Shield" : "Hide Shield");
        }
        else
        {
            Token().SetBindValue(View.ShieldTooltip, "No Shield Equipped");
        }
    }

    public override void Close()
    {
        // Don't call RaiseCloseEvent() here - it causes infinite recursion
        // The WindowDirector handles cleanup when CloseWindow() is called
        _token.Close();
    }
}
