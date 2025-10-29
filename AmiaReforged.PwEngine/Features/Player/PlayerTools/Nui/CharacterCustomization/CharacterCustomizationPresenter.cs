using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

public sealed class CharacterCustomizationPresenter : ScryPresenter<CharacterCustomizationView>
{
    public override CharacterCustomizationView View { get; }

    private readonly NwPlayer _player;
    private readonly CharacterCustomizationModel _model;
    private NuiWindowToken _token;
    private bool _initializing;

    private static readonly string[] ArmorPartNames = new[]
    {
        "Right Foot", "Left Foot", "Right Shin", "Left Shin",
        "Right Thigh", "Left Thigh", "Pelvis", "Torso",
        "Belt", "Neck", "Right Forearm", "Left Forearm",
        "Right Bicep", "Left Bicep", "Right Shoulder", "Left Shoulder",
        "Right Hand", "Left Hand", "Robe", "All Parts"
    };

    public override NuiWindowToken Token() => _token;

    public CharacterCustomizationPresenter(CharacterCustomizationView view, NwPlayer player)
    {
        View = view;
        _player = player;
        _model = new CharacterCustomizationModel(player);
    }

    public override void Create()
    {
        NuiWindow window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(50f, 50f, 700f, 720f),
            Resizable = true
        };

        if (!_player.TryCreateNuiWindow(window, out _token))
            return;

        _initializing = true;
        try
        {
            // Initialize color palette resource binds (default to regular colors)
            InitializeColorPalette(false);

            // Initialize model buttons as enabled
            Token().SetBindValue(View.ModelButtonsEnabled, true);

            UpdateModeDisplay();
            UpdatePartDisplay();
        }
        finally
        {
            _initializing = false;
        }
    }

    public override void InitBefore()
    {
    }

    private void InitializeColorPalette(bool useMetal)
    {
        // Set all 176 color button resources
        string prefix = useMetal ? "cc_color_m_" : "cc_color_";

        for (int i = 0; i < 176; i++)
        {
            Token().SetBindValue(View.ColorResRef[i], $"{prefix}{i}");
        }

        Token().SetBindValue(View.UseMetalPalette, useMetal);
    }

    public override void Close()
    {
        // Closing the window via X button should save changes (same as Confirm)
        _model.ApplyChanges();
        _token.Close();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (_initializing) return;
        if (ev.EventType != NuiEventType.Click) return;

        // Mode selection
        if (ev.ElementId == View.ArmorButton.Id)
        {
            // Always reload armor when clicking the Armor button
            // This allows players to switch to a different equipped armor without closing the window
            _model.SetMode(CustomizationMode.Armor);

            // Set default color channel to Cloth 1 (channel 2)
            _model.SetColorChannel(2);
            InitializeColorPalette(false); // Start with regular palette
            _player.SendServerMessage("Cloth 1 color channel selected.", ColorConstants.Cyan);

            UpdateModeDisplay();
            UpdatePartDisplay();
            return;
        }

        if (ev.ElementId == View.EquipmentButton.Id)
        {
            _model.SetMode(CustomizationMode.Equipment);
            UpdateModeDisplay();
            return;
        }

        if (ev.ElementId == View.AppearanceButton.Id)
        {
            _model.SetMode(CustomizationMode.Appearance);
            UpdateModeDisplay();
            return;
        }

        // Part navigation (armor mode)
        if (ev.ElementId == View.PartLeftButton.Id)
        {
            int newPart = _model.CurrentArmorPart - 1;
            if (newPart < 0)
                newPart = 19; // Wrap to last part (All Parts)
            _model.SetArmorPart(newPart);
            UpdatePartDisplay();
            return;
        }

        if (ev.ElementId == View.PartRightButton.Id)
        {
            int newPart = _model.CurrentArmorPart + 1;
            if (newPart > 19)
                newPart = 0; // Wrap to first part (Right Foot)
            _model.SetArmorPart(newPart);
            UpdatePartDisplay();
            return;
        }

        // Model navigation (armor mode)
        if (ev.ElementId == View.ModelLeft10Button.Id)
        {
            _model.AdjustArmorPartModel(-10);
            int modelNum = _model.GetCurrentArmorPartModel();
            Token().SetBindValue(View.CurrentPartModel, modelNum);
            Token().SetBindValue(View.CurrentPartModelText, modelNum.ToString());
            return;
        }

        if (ev.ElementId == View.ModelLeftButton.Id)
        {
            _model.AdjustArmorPartModel(-1);
            int modelNum = _model.GetCurrentArmorPartModel();
            Token().SetBindValue(View.CurrentPartModel, modelNum);
            Token().SetBindValue(View.CurrentPartModelText, modelNum.ToString());
            return;
        }

        if (ev.ElementId == View.ModelRightButton.Id)
        {
            _model.AdjustArmorPartModel(1);
            int modelNum = _model.GetCurrentArmorPartModel();
            Token().SetBindValue(View.CurrentPartModel, modelNum);
            Token().SetBindValue(View.CurrentPartModelText, modelNum.ToString());
            return;
        }

        if (ev.ElementId == View.ModelRight10Button.Id)
        {
            _model.AdjustArmorPartModel(10);
            int modelNum = _model.GetCurrentArmorPartModel();
            Token().SetBindValue(View.CurrentPartModel, modelNum);
            Token().SetBindValue(View.CurrentPartModelText, modelNum.ToString());
            return;
        }

        // Color selection
        if (ev.ElementId.StartsWith("btn_color_"))
        {
            if (int.TryParse(ev.ElementId.Substring("btn_color_".Length), out int colorIndex))
            {
                HandleColorSelection(colorIndex);
            }
            return;
        }

        // Material type selection (Leather1=0, Leather2=1, Cloth1=2, Cloth2=3, Metal1=4, Metal2=5)
        if (ev.ElementId == View.Leather1Button.Id)
        {
            _model.SetColorChannel(0); // Leather 1
            InitializeColorPalette(false); // Use regular palette
            _player.SendServerMessage("Leather 1 color channel selected.", ColorConstants.Cyan);
            return;
        }
        if (ev.ElementId == View.Leather2Button.Id)
        {
            _model.SetColorChannel(1); // Leather 2
            InitializeColorPalette(false); // Use regular palette
            _player.SendServerMessage("Leather 2 color channel selected.", ColorConstants.Cyan);
            return;
        }
        if (ev.ElementId == View.Cloth1Button.Id)
        {
            _model.SetColorChannel(2); // Cloth 1
            InitializeColorPalette(false); // Use regular palette
            _player.SendServerMessage("Cloth 1 color channel selected.", ColorConstants.Cyan);
            return;
        }
        if (ev.ElementId == View.Cloth2Button.Id)
        {
            _model.SetColorChannel(3); // Cloth 2
            InitializeColorPalette(false); // Use regular palette
            _player.SendServerMessage("Cloth 2 color channel selected.", ColorConstants.Cyan);
            return;
        }
        if (ev.ElementId == View.Metal1Button.Id)
        {
            _model.SetColorChannel(4); // Metal 1
            InitializeColorPalette(true); // Use metal palette
            _player.SendServerMessage("Metal 1 color channel selected.", ColorConstants.Cyan);
            return;
        }
        if (ev.ElementId == View.Metal2Button.Id)
        {
            _model.SetColorChannel(5); // Metal 2
            InitializeColorPalette(true); // Use metal palette
            _player.SendServerMessage("Metal 2 color channel selected.", ColorConstants.Cyan);
            return;
        }

        // Action buttons
        if (ev.ElementId == View.SaveButton.Id)
        {
            // Save current changes as a preset
            _player.SendServerMessage("Save feature coming soon!", ColorConstants.Orange);
            return;
        }

        if (ev.ElementId == View.CancelButton.Id)
        {
            _model.RevertChanges();
            Close();
            return;
        }

        if (ev.ElementId == View.ConfirmButton.Id)
        {
            _model.ApplyChanges();
            Close();
        }
    }

    private void UpdateModeDisplay()
    {
        Token().SetBindValue(View.ArmorModeActive, _model.CurrentMode == CustomizationMode.Armor);
        Token().SetBindValue(View.EquipmentModeActive, _model.CurrentMode == CustomizationMode.Equipment);
        Token().SetBindValue(View.AppearanceModeActive, _model.CurrentMode == CustomizationMode.Appearance);

        string modeName = _model.CurrentMode switch
        {
            CustomizationMode.Armor => "Armor Customization",
            CustomizationMode.Equipment => "Equipment Customization",
            CustomizationMode.Appearance => "Appearance Customization",
            _ => "Character Customization"
        };

        Token().SetBindValue(View.ModeName, modeName);
    }

    private void UpdatePartDisplay()
    {
        if (_model.CurrentMode != CustomizationMode.Armor) return;

        int partIndex = _model.CurrentArmorPart;

        // Update all armor part overlay visibilities
        // If "All Parts" (index 19) is selected, show all parts except Robe (index 18)
        if (partIndex == 19)
        {
            // All Parts mode - show everything except Robe
            for (int i = 0; i < 19; i++)
            {
                Token().SetBindValue(View.ArmorPartVisible[i], i != 18); // Show all except Robe
            }
        }
        else
        {
            // Normal mode - show only the selected part
            for (int i = 0; i < 19; i++)
            {
                Token().SetBindValue(View.ArmorPartVisible[i], i == partIndex);
            }
        }

        if (partIndex >= 0 && partIndex < ArmorPartNames.Length)
        {
            Token().SetBindValue(View.PartName, ArmorPartNames[partIndex]);
        }

        // For "All Parts" mode, disable model changing and don't show model number
        bool isAllPartsMode = partIndex == 19;

        // Disable/enable model changer buttons based on mode
        Token().SetBindValue(View.ModelButtonsEnabled, !isAllPartsMode);

        if (isAllPartsMode)
        {
            Token().SetBindValue(View.CurrentPartModel, 0);
            Token().SetBindValue(View.CurrentPartModelText, "N/A");
        }
        else
        {
            int modelNum = _model.GetCurrentArmorPartModel();
            Token().SetBindValue(View.CurrentPartModel, modelNum);
            Token().SetBindValue(View.CurrentPartModelText, modelNum.ToString());
        }

        Token().SetBindValue(View.CurrentColor, _model.GetCurrentArmorPartColor());
    }

    private void HandleColorSelection(int colorIndex)
    {
        switch (_model.CurrentMode)
        {
            case CustomizationMode.Armor:
                _model.SetArmorPartColor(colorIndex);
                Token().SetBindValue(View.CurrentColor, colorIndex);
                _player.SendServerMessage($"Armor part color set to {colorIndex}", ColorConstants.Green);
                break;

            case CustomizationMode.Appearance:
                // For now, set hair color - could add UI to switch between hair/tattoo1/tattoo2
                _model.SetHairColor(colorIndex);
                _player.SendServerMessage($"Hair color set to {colorIndex}", ColorConstants.Green);
                break;
        }
    }
}

