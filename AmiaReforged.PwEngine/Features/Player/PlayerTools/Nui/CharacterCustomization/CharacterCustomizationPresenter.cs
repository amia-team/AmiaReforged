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
        "Left Thigh", "Right Thigh", "Pelvis", "Torso",
        "Belt", "Neck", "Right Forearm", "Left Forearm",
        "Right Bicep", "Left Bicep", "Right Shoulder", "Left Shoulder",
        "Right Hand", "Left Hand", "Robe"
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
        var window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(50f, 50f, 700f, 720f),
            Resizable = false
        };

        if (!_player.TryCreateNuiWindow(window, out _token))
            return;

        _initializing = true;
        try
        {
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

    public override void Close()
    {
        _token.Close();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (_initializing) return;
        if (ev.EventType != NuiEventType.Click) return;

        // Mode selection
        if (ev.ElementId == View.ArmorButton.Id)
        {
            _model.SetMode(CustomizationMode.Armor);
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
            var newPart = Math.Max(0, _model.CurrentArmorPart - 1);
            _model.SetArmorPart(newPart);
            UpdatePartDisplay();
            return;
        }

        if (ev.ElementId == View.PartRightButton.Id)
        {
            var newPart = Math.Min(18, _model.CurrentArmorPart + 1);
            _model.SetArmorPart(newPart);
            UpdatePartDisplay();
            return;
        }

        // Model navigation (armor mode)
        if (ev.ElementId == View.ModelLeftButton.Id)
        {
            _model.AdjustArmorPartModel(-1);
            var modelNum = _model.GetCurrentArmorPartModel();
            Token().SetBindValue(View.CurrentPartModel, modelNum);
            Token().SetBindValue(View.CurrentPartModelText, modelNum.ToString());
            return;
        }

        if (ev.ElementId == View.ModelRightButton.Id)
        {
            _model.AdjustArmorPartModel(1);
            var modelNum = _model.GetCurrentArmorPartModel();
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
            _player.SendServerMessage("Changes reverted.", ColorConstants.Orange);
            Close();
            return;
        }

        if (ev.ElementId == View.ConfirmButton.Id)
        {
            _model.ApplyChanges();
            Close();
            return;
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

        var partIndex = _model.CurrentArmorPart;
        if (partIndex >= 0 && partIndex < ArmorPartNames.Length)
        {
            Token().SetBindValue(View.PartName, ArmorPartNames[partIndex]);
        }

        var modelNum = _model.GetCurrentArmorPartModel();
        Token().SetBindValue(View.CurrentPartModel, modelNum);
        Token().SetBindValue(View.CurrentPartModelText, modelNum.ToString());
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

