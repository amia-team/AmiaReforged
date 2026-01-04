using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.CharacterTools.ThousandFaces;

public sealed class ThousandFacesPresenter(ThousandFacesView view, NwPlayer player, PlayerNameOverrideService playerNameOverrideService)
    : ScryPresenter<ThousandFacesView>
{
    public override ThousandFacesView View { get; } = view;

    private readonly ThousandFacesModel _model = new(player, playerNameOverrideService);
    private NuiWindowToken _token;
    private bool _initializing;

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
    }

    public override void Create()
    {
        NuiWindow window = new NuiWindow(View.RootLayout(), "One Thousand Faces")
        {
            Geometry = new NuiRect(50f, 50f, 700f, 800f),
            Resizable = true
        };

        if (!player.TryCreateNuiWindow(window, out _token))
            return;

        _initializing = true;
        try
        {
            InitializeBindValues();
        }
        finally
        {
            _initializing = false;
        }
    }

    public override void Close()
    {
        _model.RevertChanges();
        _token.Close();
    }

    private void InitializeBindValues()
    {
        // Enable all controls immediately (this is different from CharacterCustomization where they start disabled)
        Token().SetBindValue(View.AlwaysEnabled, true);

        // Enable soundset and portrait confirm buttons
        Token().SetBindValue(View.SoundsetConfirmEnabled, true);
        Token().SetBindValue(View.PortraitConfirmEnabled, true);
        Token().SetBindValue(View.TempNameConfirmEnabled, true);

        // Initialize color palettes
        InitializeColorPalettes();

        // Set default color channel to hair
        Token().SetBindValue(View.CurrentColorChannel, 1);

        // Load all initial values from model
        _model.LoadInitialValues();
        UpdateHeadDisplay();
        UpdateAppearanceDisplay();
        UpdateScaleDisplay();
        UpdateSoundsetDisplay();
        UpdatePortraitDisplay();
        UpdateColorPalette();

        // Clear the input fields for soundset and portrait (leave them empty for player input)
        Token().SetBindValue(View.NewSoundsetText, "");
        Token().SetBindValue(View.NewPortraitText, "");
        Token().SetBindValue(View.TempNameText, "");
    }

    private void InitializeColorPalettes()
    {
        // Initialize skin color palette (skin colors)
        for (int i = 0; i < 176; i++)
        {
            Token().SetBindValue(View.SkinColorResRef[i], $"cc_color_s_{i}");
        }

        // Initialize hair color palette
        for (int i = 0; i < 176; i++)
        {
            Token().SetBindValue(View.HairColorResRef[i], $"cc_color_h_{i}");
        }

        // Initialize tattoo color palette (cloth/leather colors)
        for (int i = 0; i < 176; i++)
        {
            Token().SetBindValue(View.TattooColorResRef[i], $"cc_color_{i}");
        }
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (_initializing) return;

        switch (ev.EventType)
        {
            case NuiEventType.Click:
                HandleClick(ev.ElementId);
                break;
            case NuiEventType.Close:
                _model.RevertChanges();
                break;
        }
    }

    private void HandleClick(string elementId)
    {
        switch (elementId)
        {
            // Head controls
            case "btn_head_left10":
                _model.ModifyHead(-10);
                UpdateHeadDisplay();
                break;
            case "btn_head_left":
                _model.ModifyHead(-1);
                UpdateHeadDisplay();
                break;
            case "btn_head_right":
                _model.ModifyHead(1);
                UpdateHeadDisplay();
                break;
            case "btn_head_right10":
                _model.ModifyHead(10);
                UpdateHeadDisplay();
                break;
            case "btn_head_set":
                if (int.TryParse(Token().GetBindValue(View.HeadModelText), out int headModel))
                {
                    _model.SetHead(headModel);
                    UpdateHeadDisplay();
                }
                break;

            // Appearance controls
            case "btn_appearance_left10":
                _model.ModifyAppearance(-10);
                UpdateAppearanceDisplay();
                break;
            case "btn_appearance_left":
                _model.ModifyAppearance(-1);
                UpdateAppearanceDisplay();
                break;
            case "btn_appearance_right":
                _model.ModifyAppearance(1);
                UpdateAppearanceDisplay();
                break;
            case "btn_appearance_right10":
                _model.ModifyAppearance(10);
                UpdateAppearanceDisplay();
                break;
            case "btn_appearance_set":
                if (int.TryParse(Token().GetBindValue(View.AppearanceModelText), out int appearanceModel))
                {
                    _model.SetAppearance(appearanceModel);
                    UpdateAppearanceDisplay();
                }
                break;

            // Base race appearance buttons
            case "btn_appearance_dwarf":
                _model.SetAppearance(0);
                UpdateAppearanceDisplay();
                break;
            case "btn_appearance_elf":
                _model.SetAppearance(1);
                UpdateAppearanceDisplay();
                break;
            case "btn_appearance_gnome":
                _model.SetAppearance(2);
                UpdateAppearanceDisplay();
                break;
            case "btn_appearance_halfling":
                _model.SetAppearance(3);
                UpdateAppearanceDisplay();
                break;
            case "btn_appearance_halfelf":
                _model.SetAppearance(4);
                UpdateAppearanceDisplay();
                break;
            case "btn_appearance_halforc":
                _model.SetAppearance(5);
                UpdateAppearanceDisplay();
                break;
            case "btn_appearance_human":
                _model.SetAppearance(6);
                UpdateAppearanceDisplay();
                break;

            // Swap Gender control
            case "btn_swap_gender":
                _model.SwapGender();
                UpdateAppearanceDisplay();
                break;

            // Scale controls
            case "btn_scale_min":
                _model.SetScale(0.4f); // MinScale
                UpdateScaleDisplay();
                break;
            case "btn_scale_decrease10":
                _model.ModifyScale(-0.10f);
                UpdateScaleDisplay();
                break;
            case "btn_scale_decrease":
                _model.ModifyScale(-0.02f);
                UpdateScaleDisplay();
                break;
            case "btn_scale_increase":
                _model.ModifyScale(0.02f);
                UpdateScaleDisplay();
                break;
            case "btn_scale_increase10":
                _model.ModifyScale(0.10f);
                UpdateScaleDisplay();
                break;
            case "btn_scale_max":
                _model.SetScale(1.2f); // MaxScale
                UpdateScaleDisplay();
                break;

            // Temporary Name controls
            case "btn_tempname_confirm":
                string tempName = Token().GetBindValue(View.TempNameText) ?? "";
                if (!string.IsNullOrWhiteSpace(tempName))
                {
                    _model.SetTemporaryName(tempName);
                    Token().SetBindValue(View.TempNameText, ""); // Clear the input field after successful change
                }
                break;
            case "btn_restore_name":
                _model.RestoreOriginalName();
                Token().SetBindValue(View.TempNameText, ""); // Clear the input field
                break;

            // Soundset controls
            case "btn_soundset_confirm":
                string soundsetInput = Token().GetBindValue(View.NewSoundsetText) ?? "";
                if (!string.IsNullOrWhiteSpace(soundsetInput) && int.TryParse(soundsetInput, out int soundsetId))
                {
                    _model.SetSoundset(soundsetId);
                    UpdateSoundsetDisplay();
                    Token().SetBindValue(View.NewSoundsetText, ""); // Clear the input field after successful change
                }
                break;

            // Portrait controls
            case "btn_portrait_confirm":
                string portraitResRef = Token().GetBindValue(View.NewPortraitText) ?? "";
                if (!string.IsNullOrWhiteSpace(portraitResRef))
                {
                    _model.SetPortrait(portraitResRef);
                    UpdatePortraitDisplay();
                    Token().SetBindValue(View.NewPortraitText, ""); // Clear the input field after successful change
                }
                break;

            // Color channel buttons
            case "btn_skin":
                _model.SetColorChannel(0);
                Token().SetBindValue(View.CurrentColorChannel, 0);
                UpdateColorPalette();
                break;
            case "btn_hair":
                _model.SetColorChannel(1);
                Token().SetBindValue(View.CurrentColorChannel, 1);
                UpdateColorPalette();
                break;
            case "btn_tattoo1":
                _model.SetColorChannel(2);
                Token().SetBindValue(View.CurrentColorChannel, 2);
                UpdateColorPalette();
                break;
            case "btn_tattoo2":
                _model.SetColorChannel(3);
                Token().SetBindValue(View.CurrentColorChannel, 3);
                UpdateColorPalette();
                break;

            // Action buttons
            case "btn_save":
                _model.SaveChanges();
                // Re-save the current state as the new backup point
                _model.LoadInitialValues();
                UpdateAllDisplays();
                player.SendServerMessage("Changes saved successfully!", ColorConstants.Green);
                break;
            case "btn_discard":
                _model.RevertChanges();
                UpdateAllDisplays();
                player.SendServerMessage("Changes reverted.", ColorConstants.Orange);
                break;
            case "btn_cancel":
                _model.RevertChanges();
                _token.Close();
                break;

            // Color palette buttons
            default:
                if (elementId.StartsWith("btn_color_"))
                {
                    if (int.TryParse(elementId.Replace("btn_color_", ""), out int colorIndex))
                    {
                        _model.SetColor(colorIndex);
                    }
                }
                break;
        }
    }

    private void UpdateHeadDisplay()
    {
        Token().SetBindValue(View.HeadModelText, _model.HeadModel.ToString());
    }

    private void UpdateAppearanceDisplay()
    {
        Token().SetBindValue(View.AppearanceModelText, _model.AppearanceType.ToString());
    }

    private void UpdateScaleDisplay()
    {
        Token().SetBindValue(View.ScaleText, $"{(_model.Scale * 100):F0}%");
    }

    private void UpdateSoundsetDisplay()
    {
        Token().SetBindValue(View.CurrentSoundsetText, _model.NewSoundsetResRef);
        // Don't update NewSoundsetText here - leave it for player input
    }

    private void UpdatePortraitDisplay()
    {
        Token().SetBindValue(View.CurrentPortraitText, _model.NewPortrait);
        // Don't update NewPortraitText here - leave it for player input

        // Set portrait preview to medium version (append 'm' to resref)
        string portraitPreview = _model.NewPortrait;
        if (!string.IsNullOrEmpty(portraitPreview))
        {
            Token().SetBindValue(View.PortraitResRef, $"{portraitPreview}m");
        }
        else
        {
            Token().SetBindValue(View.PortraitResRef, "gui_po_nwnlogo_");
        }
    }

    private void UpdateColorPalette()
    {
        // Update the color palette based on the selected channel
        int channel = _model.GetColorChannel();

        for (int i = 0; i < 176; i++)
        {
            string resRef = channel switch
            {
                0 => $"cc_color_s_{i}", // Skin
                1 => $"cc_color_h_{i}", // Hair
                2 => $"cc_color_{i}",   // Tattoo 1 (cloth/leather)
                3 => $"cc_color_{i}",   // Tattoo 2 (cloth/leather)
                _ => $"cc_color_{i}"
            };

            Token().SetBindValue(View.SkinColorResRef[i], resRef);
        }
    }

    private void UpdateAllDisplays()
    {
        UpdateHeadDisplay();
        UpdateAppearanceDisplay();
        UpdateScaleDisplay();
        UpdateSoundsetDisplay();
        UpdatePortraitDisplay();
    }
}

