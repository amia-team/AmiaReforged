using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

public sealed class EquipmentCustomizationPresenter(EquipmentCustomizationView view, NwPlayer player)
    : ScryPresenter<EquipmentCustomizationView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override EquipmentCustomizationView View { get; } = view;

    private readonly EquipmentCustomizationModel _model = new(player);
    private NuiWindowToken _token;
    private bool _initializing;

    [Inject] private Lazy<WindowDirector> WindowDirector { get; set; } = null!;

    public override NuiWindowToken Token() => _token;

    public override void Create()
    {
        NuiWindow window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(50f, 50f, 700f, 720f),
            Resizable = true
        };

        if (!player.TryCreateNuiWindow(window, out _token))
            return;

        _initializing = true;
        try
        {
            InitializeColorPalette();
            InitializeBindValues();
        }
        finally
        {
            _initializing = false;
        }
    }

    private void InitializeColorPalette(bool useMetal = false)
    {
        string prefix = useMetal ? "cc_color_m_" : "cc_color_";

        for (int i = 0; i < 176; i++)
        {
            Token().SetBindValue(View.ColorResRef[i], $"{prefix}{i}");
        }
    }

    private void InitializeBindValues()
    {
        Token().SetBindValue(View.WeaponSelected, false);
        Token().SetBindValue(View.BootsSelected, false);
        Token().SetBindValue(View.HelmetSelected, false);
        Token().SetBindValue(View.CloakSelected, false);

        Token().SetBindValue(View.WeaponControlsEnabled, false);
        Token().SetBindValue(View.BootsControlsEnabled, false);
        Token().SetBindValue(View.HelmetControlsEnabled, false);
        Token().SetBindValue(View.CloakControlsEnabled, false);
        Token().SetBindValue(View.ChannelButtonsEnabled, false);

        Token().SetBindValue(View.WeaponTopModelText, "1");
        Token().SetBindValue(View.WeaponMidModelText, "1");
        Token().SetBindValue(View.WeaponBotModelText, "1");
        Token().SetBindValue(View.WeaponTopColorText, "1");
        Token().SetBindValue(View.WeaponMidColorText, "1");
        Token().SetBindValue(View.WeaponBotColorText, "1");

        Token().SetBindValue(View.BootsTopModelText, "1");
        Token().SetBindValue(View.BootsMidModelText, "1");
        Token().SetBindValue(View.BootsBotModelText, "1");
        Token().SetBindValue(View.BootsTopColorText, "1");
        Token().SetBindValue(View.BootsMidColorText, "1");
        Token().SetBindValue(View.BootsBotColorText, "1");

        Token().SetBindValue(View.HelmetAppearanceText, "1");
        Token().SetBindValue(View.CloakAppearanceText, "1");
    }

    public override void InitBefore()
    {
    }

    public override void Close()
    {
        _model.RevertChanges();
        _token.Close();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (_initializing) return;
        if (ev.EventType != NuiEventType.Click) return;

        Log.Info($"Equipment Customization Event: {ev.ElementId}");

        // Equipment Type Selection
        if (ev.ElementId == View.WeaponButton.Id)
        {
            Log.Info("Weapon button clicked");
            _model.SelectEquipmentType(EquipmentType.Weapon);
            UpdateEquipmentTypeDisplay();
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.BootsButton.Id)
        {
            Log.Info("Boots button clicked");
            _model.SelectEquipmentType(EquipmentType.Boots);
            UpdateEquipmentTypeDisplay();
            UpdateBootsDisplay();
            return;
        }

        if (ev.ElementId == View.HelmetButton.Id)
        {
            Log.Info("Helmet button clicked");
            _model.SelectEquipmentType(EquipmentType.Helmet);
            UpdateEquipmentTypeDisplay();
            UpdateHelmetDisplay();
            return;
        }

        if (ev.ElementId == View.CloakButton.Id)
        {
            Log.Info("Cloak button clicked");
            _model.SelectEquipmentType(EquipmentType.Cloak);
            UpdateEquipmentTypeDisplay();
            UpdateCloakDisplay();
            return;
        }

        // Weapon Controls
        if (ev.ElementId == View.WeaponTopModelLeftButton.Id)
        {
            _model.AdjustWeaponTopModel(-1);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponTopModelRightButton.Id)
        {
            _model.AdjustWeaponTopModel(1);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponMidModelLeftButton.Id)
        {
            _model.AdjustWeaponMidModel(-1);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponMidModelRightButton.Id)
        {
            _model.AdjustWeaponMidModel(1);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponBotModelLeftButton.Id)
        {
            _model.AdjustWeaponBotModel(-1);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponBotModelRightButton.Id)
        {
            _model.AdjustWeaponBotModel(1);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponTopColorLeftButton.Id)
        {
            _model.AdjustWeaponTopColor(-1);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponTopColorRightButton.Id)
        {
            _model.AdjustWeaponTopColor(1);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponMidColorLeftButton.Id)
        {
            _model.AdjustWeaponMidColor(-1);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponMidColorRightButton.Id)
        {
            _model.AdjustWeaponMidColor(1);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponBotColorLeftButton.Id)
        {
            _model.AdjustWeaponBotColor(-1);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponBotColorRightButton.Id)
        {
            _model.AdjustWeaponBotColor(1);
            UpdateWeaponDisplay();
            return;
        }

        // Boots Controls
        if (ev.ElementId == View.BootsTopModelLeftButton.Id)
        {
            _model.AdjustBootsTopModel(-1);
            UpdateBootsDisplay();
            return;
        }

        if (ev.ElementId == View.BootsTopModelRightButton.Id)
        {
            _model.AdjustBootsTopModel(1);
            UpdateBootsDisplay();
            return;
        }

        if (ev.ElementId == View.BootsMidModelLeftButton.Id)
        {
            _model.AdjustBootsMidModel(-1);
            UpdateBootsDisplay();
            return;
        }

        if (ev.ElementId == View.BootsMidModelRightButton.Id)
        {
            _model.AdjustBootsMidModel(1);
            UpdateBootsDisplay();
            return;
        }

        if (ev.ElementId == View.BootsBotModelLeftButton.Id)
        {
            _model.AdjustBootsBotModel(-1);
            UpdateBootsDisplay();
            return;
        }

        if (ev.ElementId == View.BootsBotModelRightButton.Id)
        {
            _model.AdjustBootsBotModel(1);
            UpdateBootsDisplay();
            return;
        }

        if (ev.ElementId == View.BootsTopColorLeftButton.Id)
        {
            _model.AdjustBootsTopColor(-1);
            UpdateBootsDisplay();
            return;
        }

        if (ev.ElementId == View.BootsTopColorRightButton.Id)
        {
            _model.AdjustBootsTopColor(1);
            UpdateBootsDisplay();
            return;
        }

        if (ev.ElementId == View.BootsMidColorLeftButton.Id)
        {
            _model.AdjustBootsMidColor(-1);
            UpdateBootsDisplay();
            return;
        }

        if (ev.ElementId == View.BootsMidColorRightButton.Id)
        {
            _model.AdjustBootsMidColor(1);
            UpdateBootsDisplay();
            return;
        }

        if (ev.ElementId == View.BootsBotColorLeftButton.Id)
        {
            _model.AdjustBootsBotColor(-1);
            UpdateBootsDisplay();
            return;
        }

        if (ev.ElementId == View.BootsBotColorRightButton.Id)
        {
            _model.AdjustBootsBotColor(1);
            UpdateBootsDisplay();
            return;
        }

        // Helmet Controls
        if (ev.ElementId == View.HelmetAppearanceLeftButton.Id)
        {
            _model.AdjustHelmetAppearance(-1);
            UpdateHelmetDisplay();
            return;
        }

        if (ev.ElementId == View.HelmetAppearanceRightButton.Id)
        {
            _model.AdjustHelmetAppearance(1);
            UpdateHelmetDisplay();
            return;
        }

        // Cloak Controls
        if (ev.ElementId == View.CloakAppearanceLeftButton.Id)
        {
            _model.AdjustCloakAppearance(-1);
            UpdateCloakDisplay();
            return;
        }

        if (ev.ElementId == View.CloakAppearanceRightButton.Id)
        {
            _model.AdjustCloakAppearance(1);
            UpdateCloakDisplay();
            return;
        }

        // Color Channel Selection
        if (ev.ElementId == View.Cloth1Button.Id)
        {
            InitializeColorPalette();
            player.SendServerMessage("Cloth 1 color channel selected.", ColorConstants.Cyan);
            return;
        }

        if (ev.ElementId == View.Cloth2Button.Id)
        {
            InitializeColorPalette();
            player.SendServerMessage("Cloth 2 color channel selected.", ColorConstants.Cyan);
            return;
        }

        if (ev.ElementId == View.Leather1Button.Id)
        {
            InitializeColorPalette();
            player.SendServerMessage("Leather 1 color channel selected.", ColorConstants.Cyan);
            return;
        }

        if (ev.ElementId == View.Leather2Button.Id)
        {
            InitializeColorPalette();
            player.SendServerMessage("Leather 2 color channel selected.", ColorConstants.Cyan);
            return;
        }

        if (ev.ElementId == View.Metal1Button.Id)
        {
            InitializeColorPalette(true); // Use metal palette
            player.SendServerMessage("Metal 1 color channel selected.", ColorConstants.Cyan);
            return;
        }

        if (ev.ElementId == View.Metal2Button.Id)
        {
            InitializeColorPalette(true); // Use metal palette
            player.SendServerMessage("Metal 2 color channel selected.", ColorConstants.Cyan);
            return;
        }

        // Color Palette (for Helmet/Cloak)
        if (ev.ElementId.StartsWith("btn_color_"))
        {
            if (int.TryParse(ev.ElementId.Substring("btn_color_".Length), out int colorIndex))
            {
                HandleColorSelection(colorIndex);
            }
            return;
        }

        // Action Buttons
        if (ev.ElementId == View.SaveButton.Id)
        {
            _model.ApplyChanges();
            return;
        }

        if (ev.ElementId == View.CancelButton.Id)
        {
            _model.RevertChanges();
            return;
        }

        if (ev.ElementId == View.CloseButton.Id)
        {
            Close();
        }
    }

    private void UpdateEquipmentTypeDisplay()
    {
        Token().SetBindValue(View.WeaponSelected, _model.CurrentEquipmentType == EquipmentType.Weapon);
        Token().SetBindValue(View.BootsSelected, _model.CurrentEquipmentType == EquipmentType.Boots);
        Token().SetBindValue(View.HelmetSelected, _model.CurrentEquipmentType == EquipmentType.Helmet);
        Token().SetBindValue(View.CloakSelected, _model.CurrentEquipmentType == EquipmentType.Cloak);

        Token().SetBindValue(View.WeaponControlsEnabled, _model.CurrentEquipmentType == EquipmentType.Weapon);
        Token().SetBindValue(View.BootsControlsEnabled, _model.CurrentEquipmentType == EquipmentType.Boots);
        Token().SetBindValue(View.HelmetControlsEnabled, _model.CurrentEquipmentType == EquipmentType.Helmet);
        Token().SetBindValue(View.CloakControlsEnabled, _model.CurrentEquipmentType == EquipmentType.Cloak);

        // Enable channel buttons only for Helmet or Cloak
        bool isHelmetOrCloak = _model.CurrentEquipmentType == EquipmentType.Helmet ||
                               _model.CurrentEquipmentType == EquipmentType.Cloak;
        Token().SetBindValue(View.ChannelButtonsEnabled, isHelmetOrCloak);
    }

    private void UpdateWeaponDisplay()
    {
        Token().SetBindValue(View.WeaponTopModelText, _model.WeaponTopModel.ToString());
        Token().SetBindValue(View.WeaponMidModelText, _model.WeaponMidModel.ToString());
        Token().SetBindValue(View.WeaponBotModelText, _model.WeaponBotModel.ToString());
        Token().SetBindValue(View.WeaponTopColorText, _model.WeaponTopColor.ToString());
        Token().SetBindValue(View.WeaponMidColorText, _model.WeaponMidColor.ToString());
        Token().SetBindValue(View.WeaponBotColorText, _model.WeaponBotColor.ToString());
    }

    private void UpdateBootsDisplay()
    {
        Token().SetBindValue(View.BootsTopModelText, _model.BootsTopModel.ToString());
        Token().SetBindValue(View.BootsMidModelText, _model.BootsMidModel.ToString());
        Token().SetBindValue(View.BootsBotModelText, _model.BootsBotModel.ToString());
        Token().SetBindValue(View.BootsTopColorText, _model.BootsTopColor.ToString());
        Token().SetBindValue(View.BootsMidColorText, _model.BootsMidColor.ToString());
        Token().SetBindValue(View.BootsBotColorText, _model.BootsBotColor.ToString());
    }

    private void UpdateHelmetDisplay()
    {
        Token().SetBindValue(View.HelmetAppearanceText, _model.HelmetAppearance.ToString());
    }

    private void UpdateCloakDisplay()
    {
        Token().SetBindValue(View.CloakAppearanceText, _model.CloakAppearance.ToString());
    }

    private void HandleColorSelection(int colorIndex)
    {
        if (_model.CurrentEquipmentType == EquipmentType.Helmet)
        {
            player.SendServerMessage($"Helmet color set to {colorIndex}.", ColorConstants.Cyan);
            // TODO: Apply color to helmet
        }
        else if (_model.CurrentEquipmentType == EquipmentType.Cloak)
        {
            player.SendServerMessage($"Cloak color set to {colorIndex}.", ColorConstants.Cyan);
            // TODO: Apply color to cloak
        }
    }
}

