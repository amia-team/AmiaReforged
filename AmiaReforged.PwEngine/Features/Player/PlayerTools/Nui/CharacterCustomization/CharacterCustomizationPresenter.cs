using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

public sealed class CharacterCustomizationPresenter(CharacterCustomizationView view, NwPlayer player)
    : ScryPresenter<CharacterCustomizationView>
{
    public override CharacterCustomizationView View { get; } = view;

    private readonly CharacterCustomizationModel _model = new(player);
    private NuiWindowToken _token;
    private bool _initializing;

    [Inject] private Lazy<WindowDirector> WindowDirector { get; set; } = null!;

    private static readonly string[] ArmorPartNames =
    [
        "Right Foot", "Left Foot", "Right Shin", "Left Shin",
        "Right Thigh", "Left Thigh", "Pelvis", "Torso",
        "Belt", "Neck", "Right Forearm", "Left Forearm",
        "Right Bicep", "Left Bicep", "Right Shoulder", "Left Shoulder",
        "Right Hand", "Left Hand", "Robe", "All Parts"
    ];

    public override NuiWindowToken Token() => _token;

    public override void Create()
    {
        NuiWindow window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(50f, 50f, 700f, 800f),
            Resizable = true
        };

        if (!player.TryCreateNuiWindow(window, out _token))
            return;

        // Send warning about ACP animation compatibility
        player.SendServerMessage("If you are using an ACP animation, this may not function correctly. Return to Default if you have problems crafting.", ColorConstants.Orange);

        _initializing = true;
        try
        {
            InitializeColorPalette(false);
            Token().SetBindValue(View.ModelButtonsEnabled, true);

            for (int i = 0; i < 19; i++)
            {
                Token().SetBindValue(View.ArmorPartVisible[i], false);
            }

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
        string prefix = useMetal ? "cc_color_m_" : "cc_color_";

        for (int i = 0; i < 176; i++)
        {
            Token().SetBindValue(View.ColorResRef[i], $"{prefix}{i}");
        }

        Token().SetBindValue(View.UseMetalPalette, useMetal);
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

        if (ev.ElementId == View.ArmorButton.Id)
        {
            _model.SetMode(CustomizationMode.Armor);
            _model.SetColorChannel(2);
            InitializeColorPalette(false);
            player.SendServerMessage("Cloth 1 color channel selected.", ColorConstants.Cyan);
            UpdateModeDisplay();
            UpdatePartDisplay();
            return;
        }

        if (ev.ElementId == View.EquipmentButton.Id)
        {
            player.SendServerMessage("Equipment button clicked - attempting to open window...", ColorConstants.Cyan);

            if (WindowDirector?.Value == null)
            {
                player.SendServerMessage("WARNING: WindowDirector is not injected in CharacterCustomizationPresenter!", ColorConstants.Red);
                player.SendServerMessage("This presenter may not have been properly initialized.", ColorConstants.Orange);
            }

            try
            {
                EquipmentCustomizationView equipmentView = new EquipmentCustomizationView(player);
                InjectionService? injector = AnvilCore.GetService<InjectionService>();
                if (injector != null)
                {
                    injector.Inject(equipmentView.Presenter);
                    player.SendServerMessage("Equipment presenter dependencies injected successfully", ColorConstants.Green);
                }
                else
                {
                    player.SendServerMessage("WARNING: InjectionService is null!", ColorConstants.Orange);
                }

                if (WindowDirector?.Value != null)
                {
                    WindowDirector.Value.OpenWindow(equipmentView.Presenter);
                    player.SendServerMessage("Equipment window opened successfully!", ColorConstants.Green);
                }
                else
                {
                    player.SendServerMessage("ERROR: WindowDirector is still null after injection!", ColorConstants.Red);
                }
            }
            catch (Exception ex)
            {
                player.SendServerMessage($"ERROR opening equipment window: {ex.Message}", ColorConstants.Red);
            }

            return;
        }

        if (ev.ElementId == View.AppearanceButton.Id)
        {
            player.SendServerMessage("Appearance button clicked - attempting to open window...", ColorConstants.Cyan);

            try
            {
                AppearanceCustomizationView appearanceView = new AppearanceCustomizationView(player);
                InjectionService? injector = AnvilCore.GetService<InjectionService>();
                if (injector != null)
                {
                    injector.Inject(appearanceView.Presenter);
                }

                if (WindowDirector?.Value != null)
                {
                    WindowDirector.Value.OpenWindow(appearanceView.Presenter);
                    player.SendServerMessage("Appearance window opened successfully!", ColorConstants.Green);
                }
                else
                {
                    player.SendServerMessage("ERROR: WindowDirector is null!", ColorConstants.Red);
                }
            }
            catch (Exception ex)
            {
                player.SendServerMessage($"ERROR opening appearance window: {ex.Message}", ColorConstants.Red);
            }

            return;
        }

        if (ev.ElementId == View.PartLeftButton.Id)
        {
            int newPart = _model.CurrentArmorPart - 1;
            if (newPart < 0)
                newPart = 19;
            _model.SetArmorPart(newPart);
            UpdatePartDisplay();
            return;
        }

        if (ev.ElementId == View.PartRightButton.Id)
        {
            int newPart = _model.CurrentArmorPart + 1;
            if (newPart > 19)
                newPart = 0;
            _model.SetArmorPart(newPart);
            UpdatePartDisplay();
            return;
        }

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

        if (ev.ElementId.StartsWith("btn_color_"))
        {
            if (int.TryParse(ev.ElementId.Substring("btn_color_".Length), out int colorIndex))
            {
                HandleColorSelection(colorIndex);
            }
            return;
        }

        if (ev.ElementId == View.Leather1Button.Id)
        {
            _model.SetColorChannel(0); // Leather 1
            InitializeColorPalette(false);
            player.SendServerMessage("Leather 1 color channel selected.", ColorConstants.Cyan);
            return;
        }
        if (ev.ElementId == View.Leather2Button.Id)
        {
            _model.SetColorChannel(1); // Leather 2
            InitializeColorPalette(false);
            player.SendServerMessage("Leather 2 color channel selected.", ColorConstants.Cyan);
            return;
        }
        if (ev.ElementId == View.Cloth1Button.Id)
        {
            _model.SetColorChannel(2); // Cloth 1
            InitializeColorPalette(false);
            player.SendServerMessage("Cloth 1 color channel selected.", ColorConstants.Cyan);
            return;
        }
        if (ev.ElementId == View.Cloth2Button.Id)
        {
            _model.SetColorChannel(3); // Cloth 2
            InitializeColorPalette(false);
            player.SendServerMessage("Cloth 2 color channel selected.", ColorConstants.Cyan);
            return;
        }
        if (ev.ElementId == View.Metal1Button.Id)
        {
            _model.SetColorChannel(4); // Metal 1
            InitializeColorPalette(true); // Use metal palette
            player.SendServerMessage("Metal 1 color channel selected.", ColorConstants.Cyan);
            return;
        }
        if (ev.ElementId == View.Metal2Button.Id)
        {
            _model.SetColorChannel(5); // Metal 2
            InitializeColorPalette(true); // Use metal palette
            player.SendServerMessage("Metal 2 color channel selected.", ColorConstants.Cyan);
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
            return;
        }

        if (ev.ElementId == View.CloseButton.Id)
        {
            _model.RevertChanges();
            Close();
            return;
        }

        if (ev.ElementId == View.ConfirmButton?.Id)
        {
            _model.ConfirmAndClose();
            Close();
        }

        if (ev.ElementId == View.CopyToOtherSideButton?.Id)
        {
            _model.CopyToOtherSide();
            return;
        }

        if (ev.ElementId == View.CopyAppearanceButton?.Id)
        {
            player.SendServerMessage("Select an armor in your inventory to copy the appearance to.", ColorConstants.Cyan);
            player.EnterTargetMode(OnArmorCopyTargetSelected);
            return;
        }
    }

    private void OnArmorCopyTargetSelected(ModuleEvents.OnPlayerTarget target)
    {
        if (target.TargetObject is NwItem targetItem)
        {
            _model.CopyAppearanceToItem(targetItem);
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

        if (partIndex == 19)
        {
            for (int i = 0; i < 19; i++)
            {
                Token().SetBindValue(View.ArmorPartVisible[i], i != 18);
            }
        }
        else
        {
            for (int i = 0; i < 19; i++)
            {
                Token().SetBindValue(View.ArmorPartVisible[i], i == partIndex);
            }
        }

        if (partIndex >= 0 && partIndex < ArmorPartNames.Length)
        {
            Token().SetBindValue(View.PartName, ArmorPartNames[partIndex]);
        }

        bool isAllPartsMode = partIndex == 19;
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
                player.SendServerMessage($"Armor part color set to {colorIndex}", ColorConstants.Green);
                break;

            case CustomizationMode.Appearance:
                _model.SetHairColor(colorIndex);
                player.SendServerMessage($"Hair color set to {colorIndex}", ColorConstants.Green);
                break;
        }
    }
}

