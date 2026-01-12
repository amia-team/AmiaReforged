using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
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
    public override NuiWindowToken Token() => _token;

    public override void Create()
    {
        NuiWindow window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(50f, 50f, 700f, 800f),
            Closable = false,
            Resizable = true
        };

        if (!player.TryCreateNuiWindow(window, out _token))
            return;

        _initializing = true;
        try
        {
            InitializeColorPalette();
            InitializeBindValues();
            _model.InitializeAllEquipment(); // Load all equipment and save initial backup
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
        Token().SetBindValue(View.AlwaysEnabled, true);

        Token().SetBindValue(View.WeaponSelected, false);
        Token().SetBindValue(View.OffHandSelected, false);
        Token().SetBindValue(View.BootsSelected, false);
        Token().SetBindValue(View.HelmetSelected, false);
        Token().SetBindValue(View.CloakSelected, false);

        Token().SetBindValue(View.WeaponControlsEnabled, false);
        Token().SetBindValue(View.OffHandControlsEnabled, false);
        Token().SetBindValue(View.OffHandMidBotEnabled, false);
        Token().SetBindValue(View.BootsControlsEnabled, false);
        Token().SetBindValue(View.HelmetControlsEnabled, false);
        Token().SetBindValue(View.CloakControlsEnabled, false);
        Token().SetBindValue(View.ChannelButtonsEnabled, false);

        Token().SetBindValue(View.WeaponTopModelText, "1");
        Token().SetBindValue(View.WeaponMidModelText, "1");
        Token().SetBindValue(View.WeaponBotModelText, "1");
        Token().SetBindValue(View.WeaponScaleText, "100%");

        Token().SetBindValue(View.OffHandTopModelText, "1");
        Token().SetBindValue(View.OffHandMidModelText, "1");
        Token().SetBindValue(View.OffHandBotModelText, "1");
        Token().SetBindValue(View.OffHandScaleText, "100%");

        Token().SetBindValue(View.BootsTopModelText, "1");
        Token().SetBindValue(View.BootsMidModelText, "1");
        Token().SetBindValue(View.BootsBotModelText, "1");

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

        // Handle window close event (X button)
        if (ev.EventType == NuiEventType.Close)
        {
            _model.RevertChanges();
            _model.ConfirmAndClose();
            return;
        }

        if (ev.EventType != NuiEventType.Click) return;

        Log.Info($"Equipment Customization Event: {ev.ElementId}");

        if (ev.ElementId == View.WeaponButton.Id)
        {
            Log.Info("Weapon button clicked");
            _model.SelectEquipmentType(EquipmentType.Weapon);
            UpdateEquipmentTypeDisplay();
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.OffHandButton.Id)
        {
            Log.Info("Off-Hand button clicked");
            _model.SelectEquipmentType(EquipmentType.OffHand);
            UpdateEquipmentTypeDisplay();
            UpdateOffHandDisplay();
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

        // Copy button handlers
        if (ev.ElementId == View.WeaponCopyButton.Id)
        {
            player.SendServerMessage("Select an item in your inventory to copy the weapon appearance to.", ColorConstants.Cyan);
            player.EnterTargetMode(OnWeaponCopyTargetSelected);
            return;
        }

        if (ev.ElementId == View.OffHandCopyButton.Id)
        {
            player.SendServerMessage("Select an item in your inventory to copy the off-hand appearance to.", ColorConstants.Cyan);
            player.EnterTargetMode(OnOffHandCopyTargetSelected);
            return;
        }

        if (ev.ElementId == View.BootsCopyButton.Id)
        {
            player.SendServerMessage("Select an item in your inventory to copy the boots appearance to.", ColorConstants.Cyan);
            player.EnterTargetMode(OnBootsCopyTargetSelected);
            return;
        }

        if (ev.ElementId == View.HelmetCopyButton.Id)
        {
            player.SendServerMessage("Select an item in your inventory to copy the helmet appearance to.", ColorConstants.Cyan);
            player.EnterTargetMode(OnHelmetCopyTargetSelected);
            return;
        }

        if (ev.ElementId == View.CloakCopyButton.Id)
        {
            player.SendServerMessage("Select an item in your inventory to copy the cloak appearance to.", ColorConstants.Cyan);
            player.EnterTargetMode(OnCloakCopyTargetSelected);
            return;
        }

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

        if (ev.ElementId == View.WeaponTopModelLeft10Button.Id)
        {
            _model.AdjustWeaponTopModel(-10);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponTopModelRight10Button.Id)
        {
            _model.AdjustWeaponTopModel(10);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponMidModelLeft10Button.Id)
        {
            _model.AdjustWeaponMidModel(-10);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponMidModelRight10Button.Id)
        {
            _model.AdjustWeaponMidModel(10);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponBotModelLeft10Button.Id)
        {
            _model.AdjustWeaponBotModel(-10);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponBotModelRight10Button.Id)
        {
            _model.AdjustWeaponBotModel(10);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponScaleMinusButton.Id)
        {
            _model.AdjustWeaponScale(-5);
            UpdateWeaponDisplay();
            return;
        }

        if (ev.ElementId == View.WeaponScalePlusButton.Id)
        {
            _model.AdjustWeaponScale(5);
            UpdateWeaponDisplay();
            return;
        }

        // Off-Hand button handlers
        if (ev.ElementId == View.OffHandTopModelLeftButton.Id)
        {
            _model.AdjustOffHandTopModel(-1);
            UpdateOffHandDisplay();
            return;
        }

        if (ev.ElementId == View.OffHandTopModelRightButton.Id)
        {
            _model.AdjustOffHandTopModel(1);
            UpdateOffHandDisplay();
            return;
        }

        if (ev.ElementId == View.OffHandMidModelLeftButton.Id)
        {
            _model.AdjustOffHandMidModel(-1);
            UpdateOffHandDisplay();
            return;
        }

        if (ev.ElementId == View.OffHandMidModelRightButton.Id)
        {
            _model.AdjustOffHandMidModel(1);
            UpdateOffHandDisplay();
            return;
        }

        if (ev.ElementId == View.OffHandBotModelLeftButton.Id)
        {
            _model.AdjustOffHandBotModel(-1);
            UpdateOffHandDisplay();
            return;
        }

        if (ev.ElementId == View.OffHandBotModelRightButton.Id)
        {
            _model.AdjustOffHandBotModel(1);
            UpdateOffHandDisplay();
            return;
        }

        if (ev.ElementId == View.OffHandTopModelLeft10Button.Id)
        {
            _model.AdjustOffHandTopModel(-10);
            UpdateOffHandDisplay();
            return;
        }

        if (ev.ElementId == View.OffHandTopModelRight10Button.Id)
        {
            _model.AdjustOffHandTopModel(10);
            UpdateOffHandDisplay();
            return;
        }

        if (ev.ElementId == View.OffHandMidModelLeft10Button.Id)
        {
            _model.AdjustOffHandMidModel(-10);
            UpdateOffHandDisplay();
            return;
        }

        if (ev.ElementId == View.OffHandMidModelRight10Button.Id)
        {
            _model.AdjustOffHandMidModel(10);
            UpdateOffHandDisplay();
            return;
        }

        if (ev.ElementId == View.OffHandBotModelLeft10Button.Id)
        {
            _model.AdjustOffHandBotModel(-10);
            UpdateOffHandDisplay();
            return;
        }

        if (ev.ElementId == View.OffHandBotModelRight10Button.Id)
        {
            _model.AdjustOffHandBotModel(10);
            UpdateOffHandDisplay();
            return;
        }

        if (ev.ElementId == View.OffHandScaleMinusButton.Id)
        {
            _model.AdjustOffHandScale(-5);
            UpdateOffHandDisplay();
            return;
        }

        if (ev.ElementId == View.OffHandScalePlusButton.Id)
        {
            _model.AdjustOffHandScale(5);
            UpdateOffHandDisplay();
            return;
        }

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

        if (ev.ElementId == View.Cloth1Button.Id)
        {
            if (_model.CurrentEquipmentType == EquipmentType.Helmet)
            {
                _model.SetHelmetColorChannel(2);
            }
            else if (_model.CurrentEquipmentType == EquipmentType.Cloak)
            {
                _model.SetCloakColorChannel(2);
            }
            InitializeColorPalette();
            player.SendServerMessage("Cloth 1 color channel selected.", ColorConstants.Cyan);
            return;
        }

        if (ev.ElementId == View.Cloth2Button.Id)
        {
            if (_model.CurrentEquipmentType == EquipmentType.Helmet)
            {
                _model.SetHelmetColorChannel(3);
            }
            else if (_model.CurrentEquipmentType == EquipmentType.Cloak)
            {
                _model.SetCloakColorChannel(3);
            }
            InitializeColorPalette();
            player.SendServerMessage("Cloth 2 color channel selected.", ColorConstants.Cyan);
            return;
        }

        if (ev.ElementId == View.Leather1Button.Id)
        {
            if (_model.CurrentEquipmentType == EquipmentType.Helmet)
            {
                _model.SetHelmetColorChannel(0);
            }
            else if (_model.CurrentEquipmentType == EquipmentType.Cloak)
            {
                _model.SetCloakColorChannel(0);
            }
            InitializeColorPalette();
            player.SendServerMessage("Leather 1 color channel selected.", ColorConstants.Cyan);
            return;
        }

        if (ev.ElementId == View.Leather2Button.Id)
        {
            if (_model.CurrentEquipmentType == EquipmentType.Helmet)
            {
                _model.SetHelmetColorChannel(1);
            }
            else if (_model.CurrentEquipmentType == EquipmentType.Cloak)
            {
                _model.SetCloakColorChannel(1);
            }
            InitializeColorPalette();
            player.SendServerMessage("Leather 2 color channel selected.", ColorConstants.Cyan);
            return;
        }

        if (ev.ElementId == View.Metal1Button.Id)
        {
            if (_model.CurrentEquipmentType == EquipmentType.Helmet)
            {
                _model.SetHelmetColorChannel(4);
            }
            else if (_model.CurrentEquipmentType == EquipmentType.Cloak)
            {
                _model.SetCloakColorChannel(4);
            }
            InitializeColorPalette(true); // Use metal palette
            player.SendServerMessage("Metal 1 color channel selected.", ColorConstants.Cyan);
            return;
        }

        if (ev.ElementId == View.Metal2Button.Id)
        {
            if (_model.CurrentEquipmentType == EquipmentType.Helmet)
            {
                _model.SetHelmetColorChannel(5);
            }
            else if (_model.CurrentEquipmentType == EquipmentType.Cloak)
            {
                _model.SetCloakColorChannel(5);
            }
            InitializeColorPalette(true); // Use metal palette
            player.SendServerMessage("Metal 2 color channel selected.", ColorConstants.Cyan);
            return;
        }

        if (ev.ElementId.StartsWith("btn_color_"))
        {
            if (int.TryParse(ev.ElementId.Substring("btn_color_".Length), out int colorIndex))
            {
                HandleColorSelection(colorIndex);
            }
            return;
        }

        if (ev.ElementId == View.SaveButton.Id)
        {
            _model.ApplyChanges();
            return;
        }

        if (ev.ElementId == View.CancelButton.Id)
        {
            _model.RevertChanges();
            UpdateDisplays();
            return;
        }

        if (ev.ElementId == View.CloseButton.Id)
        {
            _model.RevertChanges();
            _model.ConfirmAndClose();
            Close();
        }
    }

    private void UpdateEquipmentTypeDisplay()
    {
        Token().SetBindValue(View.WeaponSelected, _model.CurrentEquipmentType == EquipmentType.Weapon);
        Token().SetBindValue(View.OffHandSelected, _model.CurrentEquipmentType == EquipmentType.OffHand);
        Token().SetBindValue(View.BootsSelected, _model.CurrentEquipmentType == EquipmentType.Boots);
        Token().SetBindValue(View.HelmetSelected, _model.CurrentEquipmentType == EquipmentType.Helmet);
        Token().SetBindValue(View.CloakSelected, _model.CurrentEquipmentType == EquipmentType.Cloak);

        Token().SetBindValue(View.WeaponControlsEnabled, _model.CurrentEquipmentType == EquipmentType.Weapon);
        Token().SetBindValue(View.OffHandControlsEnabled, _model.CurrentEquipmentType == EquipmentType.OffHand);
        Token().SetBindValue(View.OffHandMidBotEnabled, _model.CurrentEquipmentType == EquipmentType.OffHand && !_model.OffHandIsSimple);
        Token().SetBindValue(View.BootsControlsEnabled, _model.CurrentEquipmentType == EquipmentType.Boots);
        Token().SetBindValue(View.HelmetControlsEnabled, _model.CurrentEquipmentType == EquipmentType.Helmet);
        Token().SetBindValue(View.CloakControlsEnabled, _model.CurrentEquipmentType == EquipmentType.Cloak);

        bool isHelmetOrCloak = _model.CurrentEquipmentType == EquipmentType.Helmet ||
                               _model.CurrentEquipmentType == EquipmentType.Cloak;
        Token().SetBindValue(View.ChannelButtonsEnabled, isHelmetOrCloak);
    }

    private void UpdateWeaponDisplay()
    {
        Token().SetBindValue(View.WeaponTopModelText, _model.WeaponTopModel.ToString());
        Token().SetBindValue(View.WeaponMidModelText, _model.WeaponMidModel.ToString());
        Token().SetBindValue(View.WeaponBotModelText, _model.WeaponBotModel.ToString());
        Token().SetBindValue(View.WeaponScaleText, $"{_model.WeaponScale}%");
        Token().SetBindValue(View.WeaponMidBotEnabled, _model.CurrentEquipmentType == EquipmentType.Weapon && !_model.WeaponIsSimple);
    }

    private void UpdateOffHandDisplay()
    {
        Token().SetBindValue(View.OffHandTopModelText, _model.OffHandTopModel.ToString());
        Token().SetBindValue(View.OffHandMidModelText, _model.OffHandMidModel.ToString());
        Token().SetBindValue(View.OffHandBotModelText, _model.OffHandBotModel.ToString());
        Token().SetBindValue(View.OffHandScaleText, $"{_model.OffHandScale}%");
        Token().SetBindValue(View.OffHandMidBotEnabled, _model.CurrentEquipmentType == EquipmentType.OffHand && !_model.OffHandIsSimple);
    }

    private void UpdateBootsDisplay()
    {
        Token().SetBindValue(View.BootsTopModelText, _model.BootsTopModel.ToString());
        Token().SetBindValue(View.BootsMidModelText, _model.BootsMidModel.ToString());
        Token().SetBindValue(View.BootsBotModelText, _model.BootsBotModel.ToString());
    }

    private void UpdateHelmetDisplay()
    {
        Token().SetBindValue(View.HelmetAppearanceText, _model.HelmetAppearance.ToString());
    }

    private void UpdateCloakDisplay()
    {
        Token().SetBindValue(View.CloakAppearanceText, _model.CloakAppearance.ToString());
    }

    private void UpdateDisplays()
    {
        UpdateWeaponDisplay();
        UpdateOffHandDisplay();
        UpdateBootsDisplay();
        UpdateHelmetDisplay();
        UpdateCloakDisplay();
    }

    private void HandleColorSelection(int colorIndex)
    {
        if (_model.CurrentEquipmentType == EquipmentType.Helmet)
        {
            _model.SetHelmetColor(colorIndex);
            UpdateHelmetDisplay();
        }
        else if (_model.CurrentEquipmentType == EquipmentType.Cloak)
        {
            _model.SetCloakColor(colorIndex);
            UpdateCloakDisplay();
        }
    }

    private void OnWeaponCopyTargetSelected(ModuleEvents.OnPlayerTarget target)
    {
        if (target.TargetObject is NwItem targetItem)
        {
            _model.CopyAppearanceToItem(targetItem, EquipmentType.Weapon);
        }
    }

    private void OnOffHandCopyTargetSelected(ModuleEvents.OnPlayerTarget target)
    {
        if (target.TargetObject is NwItem targetItem)
        {
            _model.CopyAppearanceToItem(targetItem, EquipmentType.OffHand);
        }
    }

    private void OnBootsCopyTargetSelected(ModuleEvents.OnPlayerTarget target)
    {
        if (target.TargetObject is NwItem targetItem)
        {
            _model.CopyAppearanceToItem(targetItem, EquipmentType.Boots);
        }
    }

    private void OnHelmetCopyTargetSelected(ModuleEvents.OnPlayerTarget target)
    {
        if (target.TargetObject is NwItem targetItem)
        {
            _model.CopyAppearanceToItem(targetItem, EquipmentType.Helmet);
        }
    }

    private void OnCloakCopyTargetSelected(ModuleEvents.OnPlayerTarget target)
    {
        if (target.TargetObject is NwItem targetItem)
        {
            _model.CopyAppearanceToItem(targetItem, EquipmentType.Cloak);
        }
    }
}

