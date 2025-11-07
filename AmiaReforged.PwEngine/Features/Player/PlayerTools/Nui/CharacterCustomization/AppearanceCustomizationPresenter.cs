﻿using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

public sealed class AppearanceCustomizationPresenter(AppearanceCustomizationView view, NwPlayer player)
    : ScryPresenter<AppearanceCustomizationView>
{
    public override AppearanceCustomizationView View { get; } = view;

    private readonly AppearanceCustomizationModel _model = new(player);
    private NuiWindowToken _token;
    private bool _initializing;

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
    }

    public override void Create()
    {
        NuiWindow window = new NuiWindow(View.RootLayout(), "Appearance Customization")
        {
            Geometry = new NuiRect(50f, 50f, 700f, 800f),
            Resizable = false
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
        Token().SetBindValue(View.HeadControlsEnabled, false);
        Token().SetBindValue(View.HeadModelText, "1");

        Token().SetBindValue(View.ScaleControlsEnabled, false);
        Token().SetBindValue(View.ScaleText, "100%");

        // Enable voiceset and portrait confirm buttons
        Token().SetBindValue(View.VoicesetConfirmEnabled, true);
        Token().SetBindValue(View.PortraitConfirmEnabled, true);

        // Initialize tattoo controls
        Token().SetBindValue(View.TattooControlsEnabled, false);
        Token().SetBindValue(View.TattooPartName, "");
        Token().SetBindValue(View.TattooModelText, "1");
        for (int i = 0; i < 9; i++)
        {
            Token().SetBindValue(View.TattooPartVisible[i], false);
        }

        // Enable channel selection buttons
        Token().SetBindValue(View.ChannelButtonsEnabled, true);

        // Initialize color palette (default to Tattoo1 channel)
        Token().SetBindValue(View.CurrentColorChannel, 0);
        InitializeColorPalettes();

        // Load all initial appearance values (head, scale, voiceset, portrait, tattoos, colors)
        _model.LoadInitialValues();
        UpdateHeadDisplay();
        UpdateScaleDisplay();
        UpdateVoicesetDisplay();
        UpdatePortraitDisplay();
    }

    private void InitializeColorPalettes()
    {
        // Initialize tattoo color palette (cloth/leather colors)
        for (int i = 0; i < 176; i++)
        {
            Token().SetBindValue(View.TattooColorResRef[i], $"cc_color_{i}");
        }

        // Initialize hair color palette (hair colors)
        for (int i = 0; i < 176; i++)
        {
            Token().SetBindValue(View.HairColorResRef[i], $"cc_color_h_{i}");
        }
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (_initializing) return;

        // Handle window close event (X button)
        if (ev.EventType == NuiEventType.Close)
        {
            _model.RevertChanges();
            return;
        }

        if (ev.EventType == NuiEventType.Click)
        {
            HandleClickEvent(ev);
        }
    }

    private void HandleClickEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.ElementId == View.HeadButton.Id)
        {
            _model.SelectHead();
            Token().SetBindValue(View.HeadControlsEnabled, true);
            UpdateHeadDisplay();
            return;
        }

        if (ev.ElementId == View.HeadModelLeft10Button.Id)
        {
            _model.AdjustHeadModel(-10);
            UpdateHeadDisplay();
            return;
        }

        if (ev.ElementId == View.HeadModelLeftButton.Id)
        {
            _model.AdjustHeadModel(-1);
            UpdateHeadDisplay();
            return;
        }

        if (ev.ElementId == View.HeadModelRightButton.Id)
        {
            _model.AdjustHeadModel(1);
            UpdateHeadDisplay();
            return;
        }

        if (ev.ElementId == View.HeadModelRight10Button.Id)
        {
            _model.AdjustHeadModel(10);
            UpdateHeadDisplay();
            return;
        }

        if (ev.ElementId == View.HeadModelSetButton.Id)
        {
            HandleHeadModelInput();
            return;
        }

        if (ev.ElementId == View.ScaleButton.Id)
        {
            _model.SelectScale();
            Token().SetBindValue(View.ScaleControlsEnabled, true);
            UpdateScaleDisplay();
            return;
        }

        if (ev.ElementId == View.ScaleDecreaseButton.Id)
        {
            _model.AdjustScale(-2);
            UpdateScaleDisplay();
            return;
        }

        if (ev.ElementId == View.ScaleIncreaseButton.Id)
        {
            _model.AdjustScale(2);
            UpdateScaleDisplay();
            return;
        }

        if (ev.ElementId == View.VoicesetConfirmButton.Id)
        {
            string? input = Token().GetBindValue(View.NewVoicesetText);
            if (!string.IsNullOrEmpty(input))
            {
                _model.SetVoiceset(input);
                UpdateVoicesetDisplay();
            }
            return;
        }

        if (ev.ElementId == View.PortraitConfirmButton.Id)
        {
            string? input = Token().GetBindValue(View.NewPortraitText);
            if (!string.IsNullOrEmpty(input))
            {
                _model.SetPortrait(input);
                UpdatePortraitDisplay();
            }
            return;
        }

        if (ev.ElementId == View.TattooButton.Id)
        {
            _model.SelectTattoo();
            Token().SetBindValue(View.TattooControlsEnabled, true);
            UpdateTattooDisplay();
            return;
        }

        if (ev.ElementId == View.TattooPartLeftButton.Id)
        {
            _model.CycleTattooPart(-1);
            UpdateTattooDisplay();
            return;
        }

        if (ev.ElementId == View.TattooPartRightButton.Id)
        {
            _model.CycleTattooPart(1);
            UpdateTattooDisplay();
            return;
        }

        if (ev.ElementId == View.TattooModelLeftButton.Id)
        {
            _model.AdjustTattooModel(-1);
            UpdateTattooDisplay();
            return;
        }

        if (ev.ElementId == View.TattooModelRightButton.Id)
        {
            _model.AdjustTattooModel(1);
            UpdateTattooDisplay();
            return;
        }

        // Channel selection buttons
        if (ev.ElementId == View.Tattoo1Button.Id)
        {
            _model.SelectColorChannel(0);
            Token().SetBindValue(View.CurrentColorChannel, 0);
            UpdateColorPaletteDisplay();
            return;
        }

        if (ev.ElementId == View.Tattoo2Button.Id)
        {
            _model.SelectColorChannel(1);
            Token().SetBindValue(View.CurrentColorChannel, 1);
            UpdateColorPaletteDisplay();
            return;
        }

        if (ev.ElementId == View.HairButton.Id)
        {
            _model.SelectColorChannel(2);
            Token().SetBindValue(View.CurrentColorChannel, 2);
            UpdateColorPaletteDisplay();
            return;
        }

        // Color palette buttons
        if (ev.ElementId.StartsWith("btn_app_color_"))
        {
            if (int.TryParse(ev.ElementId.Replace("btn_app_color_", ""), out int colorIndex))
            {
                _model.SetColor(colorIndex);
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
            UpdateHeadDisplay();
            UpdateScaleDisplay();
            UpdateVoicesetDisplay();
            UpdatePortraitDisplay();
            UpdateTattooDisplay();
            return;
        }

        if (ev.ElementId == View.CloseButton.Id)
        {
            _model.ConfirmAndClose();
            Close();
        }
    }

    private void HandleHeadModelInput()
    {
        string? input = Token().GetBindValue(View.HeadModelText);
        if (string.IsNullOrEmpty(input))
        {
            player.SendServerMessage("Please enter a head model number.", ColorConstants.Orange);
            return;
        }

        if (int.TryParse(input, out int modelNumber))
        {
            _model.SetHeadModelDirect(modelNumber);
            UpdateHeadDisplay();
        }
        else
        {
            player.SendServerMessage("Invalid head model number.", ColorConstants.Orange);
        }
    }

    private void UpdateHeadDisplay()
    {
        Token().SetBindValue(View.HeadModelText, _model.HeadModel.ToString());
    }

    private void UpdateScaleDisplay()
    {
        // Round to nearest integer to ensure 1.06 displays as 106%, not 105%
        int scalePercent = (int)Math.Round(_model.Scale * 100);
        Token().SetBindValue(View.ScaleText, $"{scalePercent}%");
    }

    private void UpdateVoicesetDisplay()
    {
        Token().SetBindValue(View.CurrentVoicesetText, $"{_model.CurrentSoundset} ({_model.CurrentSoundsetResRef})");
        Token().SetBindValue(View.NewVoicesetText, "");
    }

    private void UpdatePortraitDisplay()
    {
        Token().SetBindValue(View.CurrentPortraitText, _model.CurrentPortrait);
        Token().SetBindValue(View.NewPortraitText, "");

        // Set portrait preview to medium version (append 'm' to resref)
        string portraitPreview = string.IsNullOrEmpty(_model.NewPortrait) ? _model.CurrentPortrait : _model.NewPortrait;
        if (!string.IsNullOrEmpty(portraitPreview))
        {
            // Append 'm' for medium portrait
            Token().SetBindValue(View.PortraitResRef, $"{portraitPreview}m");
        }
        else
        {
            Token().SetBindValue(View.PortraitResRef, "gui_po_nwnlogo_");
        }
    }

    private void UpdateTattooDisplay()
    {
        // Update part name display
        Token().SetBindValue(View.TattooPartName, _model.GetCurrentTattooPartName());

        // Update model number display
        Token().SetBindValue(View.TattooModelText, _model.GetCurrentTattooModel().ToString());

        // Update visibility for all tattoo parts - show ONLY the currently selected part (always visible so player can see which part they're working on)
        int currentPartIndex = _model.GetCurrentTattooPartIndex();
        for (int i = 0; i < 9; i++)
        {
            // Show only the currently selected part (always show it, even if model = 1 "no tattoo")
            Token().SetBindValue(View.TattooPartVisible[i], i == currentPartIndex);
        }
    }

    private void UpdateColorPaletteDisplay()
    {
        // Switch color palette images based on selected channel
        // Channels 0 and 1 (Tattoo1, Tattoo2) use cloth/leather palette
        // Channel 2 (Hair) uses hair palette
        int currentChannel = Token().GetBindValue(View.CurrentColorChannel);

        for (int i = 0; i < 176; i++)
        {
            if (currentChannel == 2) // Hair channel
            {
                Token().SetBindValue(View.TattooColorResRef[i], $"cc_color_h_{i}");
            }
            else // Tattoo channels
            {
                Token().SetBindValue(View.TattooColorResRef[i], $"cc_color_{i}");
            }
        }
    }
}

