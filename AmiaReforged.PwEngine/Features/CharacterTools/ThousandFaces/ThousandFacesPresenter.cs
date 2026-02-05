using AmiaReforged.PwEngine.Features.Module;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.CharacterTools.ThousandFaces;

public sealed class ThousandFacesPresenter(ThousandFacesView view, NwPlayer player, PlayerNameOverrideService playerNameOverrideService)
    : ScryPresenter<ThousandFacesView>
{
    public override ThousandFacesView View { get; } = view;

    private readonly ThousandFacesModel _model = new(player, playerNameOverrideService);
    private readonly AppearanceCache _appearanceCache = AnvilCore.GetService<AppearanceCache>()!;
    private NuiWindowToken _token;
    private NuiWindowToken? _skinSearchModalToken;
    private List<(int id, string label)> _currentSkinSearchResults = new();
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

            // Skin Search control
            case "btn_search_skin":
                OpenSkinSearchModal();
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

    private void OpenSkinSearchModal()
    {
        // Close existing modal if open
        CloseSkinSearchModal();

        // Ensure cache is initialized
        _appearanceCache.EnsureInitialized();

        // Create the modal window (only done once)
        NuiWindow modal = View.BuildSkinSearchModal();
        if (player.TryCreateNuiWindow(modal, out NuiWindowToken modalToken))
        {
            _skinSearchModalToken = modalToken;
            // Initialize the search text bind
            _skinSearchModalToken.Value.SetBindValue(View.SkinSearchText, "");
            // Subscribe to modal events
            _skinSearchModalToken.Value.OnNuiEvent += HandleSkinSearchModalEvent;

            // Load first 20 appearances by default
            List<(int id, string label)> appearances = _appearanceCache.GetAllAppearances();

            if (appearances.Count == 0)
            {
                player.SendServerMessage("No valid appearances found in cache. The cache may not have initialized properly.", ColorConstants.Red);
                return;
            }

            // Show first 20 by default
            appearances = appearances.Take(20).ToList();
            player.SendServerMessage($"Showing first 20 of {_appearanceCache.AllAppearances.Count} valid appearances. Use search to find specific skins.", ColorConstants.Cyan);

            // Update the list bindings
            UpdateSkinSearchList(appearances);
        }
    }

    private void HandleSkinSearchModalEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_skin_search":
                string skinSearchTerm = _skinSearchModalToken.HasValue
                    ? _skinSearchModalToken.Value.GetBindValue(View.SkinSearchText) ?? ""
                    : "";
                if (!string.IsNullOrWhiteSpace(skinSearchTerm))
                {
                    PerformSkinSearch(skinSearchTerm);
                }
                else
                {
                    player.SendServerMessage("Please enter a search term (e.g. 'Cat', 'Dragon', 'Wolf').", ColorConstants.Orange);
                }
                break;
            case "btn_skin_search_close":
                CloseSkinSearchModal();
                break;
            case "btn_set_skin":
                // NuiList button click - get the row index from the event
                int rowIndex = ev.ArrayIndex;
                if (rowIndex >= 0 && rowIndex < _currentSkinSearchResults.Count)
                {
                    int appearanceId = _currentSkinSearchResults[rowIndex].id;
                    _model.SetAppearance(appearanceId);
                    UpdateAppearanceDisplay();
                    player.SendServerMessage($"Appearance set to {_currentSkinSearchResults[rowIndex].label} (ID: {appearanceId})", ColorConstants.Green);
                }
                break;
        }
    }

    private void CloseSkinSearchModal()
    {
        if (_skinSearchModalToken.HasValue)
        {
            // Unsubscribe from events
            _skinSearchModalToken.Value.OnNuiEvent -= HandleSkinSearchModalEvent;
            try
            {
                _skinSearchModalToken.Value.Close();
            }
            catch
            {
                // ignore
            }
            _skinSearchModalToken = null;
        }
        _currentSkinSearchResults.Clear();
    }

    /// <summary>
    /// Performs a skin search and updates the list without recreating the window.
    /// </summary>
    private void PerformSkinSearch(string searchTerm)
    {
        if (!_skinSearchModalToken.HasValue) return;

        // Get filtered appearances
        List<(int id, string label)> appearances = _appearanceCache.SearchAppearances(searchTerm);

        if (appearances.Count == 0)
        {
            player.SendServerMessage($"No appearances found matching '{searchTerm}'.", ColorConstants.Orange);
        }
        else
        {
            player.SendServerMessage($"Found {appearances.Count} appearances matching '{searchTerm}'.", ColorConstants.Green);
        }

        // Update the list bindings (without recreating the window)
        UpdateSkinSearchList(appearances);
    }

    /// <summary>
    /// Updates the NuiList bindings to display the given appearances.
    /// </summary>
    private void UpdateSkinSearchList(List<(int id, string label)> appearances)
    {
        if (!_skinSearchModalToken.HasValue) return;

        // Store the current results for row index lookup when clicking
        _currentSkinSearchResults = appearances;

        // Build the bind arrays
        List<string> ids = appearances.Select(a => a.id.ToString()).ToList();
        List<string> labels = appearances.Select(a => a.label).ToList();

        // Update the bindings
        _skinSearchModalToken.Value.SetBindValues(View.SkinListIds, ids);
        _skinSearchModalToken.Value.SetBindValues(View.SkinListLabels, labels);
        _skinSearchModalToken.Value.SetBindValue(View.SkinListCount, appearances.Count);
    }
}
