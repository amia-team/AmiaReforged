using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

public sealed class AppearanceCustomizationView : ScryView<AppearanceCustomizationPresenter>
{
    private const float WindowW = 700f;
    private const float WindowH = 900f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 8f;
    private const float HeaderLeftPad = 25f;

    public override AppearanceCustomizationPresenter Presenter { get; protected set; }

    public readonly NuiBind<string> HeadModelText = new("app_head_model");
    public readonly NuiBind<bool> HeadControlsEnabled = new("app_head_controls_enabled");

    public readonly NuiBind<string> ScaleText = new("app_scale");
    public readonly NuiBind<bool> ScaleControlsEnabled = new("app_scale_controls_enabled");

    public readonly NuiBind<string> CurrentVoicesetText = new("app_current_voiceset");
    public readonly NuiBind<string> NewVoicesetText = new("app_new_voiceset");
    public readonly NuiBind<bool> VoicesetConfirmEnabled = new("app_voiceset_confirm_enabled");
    public NuiTextEdit VoicesetInputField = null!;
    public NuiButtonImage VoicesetConfirmButton = null!;

    public readonly NuiBind<string> CurrentPortraitText = new("app_current_portrait");
    public readonly NuiBind<string> NewPortraitText = new("app_new_portrait");
    public readonly NuiBind<string> PortraitResRef = new("app_portrait_resref");
    public readonly NuiBind<bool> PortraitConfirmEnabled = new("app_portrait_confirm_enabled");
    public NuiTextEdit PortraitInputField = null!;
    public NuiButtonImage PortraitConfirmButton = null!;

    public NuiButtonImage HeadButton = null!;
    public NuiButtonImage HeadModelLeft10Button = null!;
    public NuiButtonImage HeadModelLeftButton = null!;
    public NuiButtonImage HeadModelRightButton = null!;
    public NuiButtonImage HeadModelRight10Button = null!;
    public NuiButtonImage HeadModelSetButton = null!;
    public NuiTextEdit HeadModelInputField = null!;

    public NuiButtonImage ScaleButton = null!;
    public NuiButtonImage ScaleDecreaseButton = null!;
    public NuiButtonImage ScaleIncreaseButton = null!;

    public NuiButtonImage SaveButton = null!;
    public NuiButtonImage CancelButton = null!;
    public NuiButtonImage CloseButton = null!;

    public AppearanceCustomizationView(NwPlayer player)
    {
        Presenter = new AppearanceCustomizationPresenter(this, player);
    }

    public override NuiLayout RootLayout()
    {
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))]
        };

        NuiElement headerOverlay = BuildHeaderOverlay();
        NuiSpacer headerSpacer = new NuiSpacer { Height = HeaderH + HeaderTopPad + 6f };

        return new NuiColumn
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            {
                bgLayer,
                headerOverlay,
                headerSpacer,
                BuildMainContent()
            }
        };
    }

    private NuiElement BuildHeaderOverlay()
    {
        return new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))]
        };
    }

    private NuiElement BuildMainContent()
    {
        return new NuiColumn
        {
            Children =
            {
                // Instructions label
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 230f },
                        new NuiLabel("Select an appearance to modify.")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },

                // Head section
                BuildHeadSection(),

                new NuiSpacer { Height = 10f },

                // Scale section
                BuildScaleSection(),

                new NuiSpacer { Height = 10f },

                // Voiceset section
                BuildVoicesetSection(),

                new NuiSpacer { Height = 10f },

                // Portrait section
                BuildPortraitSection(),

                new NuiSpacer { Height = 10f },

                // Action buttons
                BuildActionButtons()
            }
        };
    }

    private NuiElement BuildHeadSection()
    {
        return new NuiColumn
        {
            Children =
            {
                // Head button row
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 290f },
                        ImagePlatedLabeledButton("btn_head", "Head", "Change Head", out HeadButton, "cc_head_btn", 75f, 75f)
                    }
                },
                new NuiSpacer { Height = 10f },

                // Head model controls row
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 120f },
                        new NuiLabel("Head Model:")
                        {
                            Width = 120f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_head_model_left10", "-10", out HeadModelLeft10Button, 30f, 30f, "ui_btn_sm_min10", HeadControlsEnabled),
                        ImageButton("btn_head_model_left", "-1", out HeadModelLeftButton, 30f, 30f, "ui_btn_sm_min1", HeadControlsEnabled),
                        new NuiLabel(HeadModelText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_head_model_right", "+1", out HeadModelRightButton, 30f, 30f, "ui_btn_sm_plus1", HeadControlsEnabled),
                        ImageButton("btn_head_model_right10", "+10", out HeadModelRight10Button, 30f, 30f, "ui_btn_sm_plus10", HeadControlsEnabled),
                        new NuiTextEdit("", HeadModelText, 10, false)
                        {
                            Width = 60f,
                            Height = 30f,
                            Enabled = HeadControlsEnabled
                        }.Assign(out HeadModelInputField),
                        ImageButton("btn_head_model_set", "Set head to specific number", out HeadModelSetButton, 30f, 30f, "ui_btn_sm_check", HeadControlsEnabled)
                    }
                }
            }
        };
    }

    private NuiElement BuildScaleSection()
    {
        return new NuiColumn
        {
            Children =
            {
                // Scale button row
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 290f },
                        ImagePlatedLabeledButton("btn_scale", "Scale", "Change Character Scale", out ScaleButton, "cc_scale", 75f, 75f)
                    }
                },
                new NuiSpacer { Height = 10f },

                // Scale controls row
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 180f },
                        new NuiLabel("Scale:")
                        {
                            Width = 80f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_scale_decrease", "-2%", out ScaleDecreaseButton, 30f, 30f, "ui_btn_sm_min", ScaleControlsEnabled),
                        new NuiLabel(ScaleText)
                        {
                            Width = 80f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_scale_increase", "+2%", out ScaleIncreaseButton, 30f, 30f, "ui_btn_sm_plus", ScaleControlsEnabled)
                    }
                }
            }
        };
    }

    private NuiElement BuildVoicesetSection()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 50f },
                        new NuiLabel("Current Voiceset:")
                        {
                            Width = 150f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiLabel(CurrentVoicesetText)
                        {
                            Width = 400f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 50f },
                        new NuiLabel("New Voiceset:")
                        {
                            Width = 150f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiTextEdit("Enter soundset number", NewVoicesetText, 50, false)
                        {
                            Width = 300f,
                            Height = 30f
                        }.Assign(out VoicesetInputField),
                        new NuiSpacer { Width = 10f },
                        ImageButton("btn_voiceset_confirm", "Confirm voiceset change", out VoicesetConfirmButton, 30f, 30f, "ui_btn_sm_check", VoicesetConfirmEnabled)
                    }
                }
            }
        };
    }

    private NuiElement BuildPortraitSection()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 50f },
                        new NuiLabel("Current Portrait:")
                        {
                            Width = 150f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiLabel(CurrentPortraitText)
                        {
                            Width = 200f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 80f },
                        // Portrait preview image (medium size - 64x100)
                        new NuiImage(PortraitResRef)
                        {
                            Width = 64f,
                            Height = 100f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiSpacer { Width = 50f },
                        new NuiColumn
                        {
                            Children =
                            {
                                new NuiLabel(" New Portrait:")
                                {
                                    Width = 150f,
                                    Height = 30f,
                                    VerticalAlign = NuiVAlign.Middle,
                                    ForegroundColor = new Color(30, 20, 12)
                                },
                                new NuiRow
                                {
                                    Children =
                                    {
                                        new NuiTextEdit("Enter portrait resref", NewPortraitText, 50, false)
                                        {
                                            Width = 300f,
                                            Height = 30f
                                        }.Assign(out PortraitInputField),
                                        new NuiSpacer { Width = 10f },
                                        ImageButton("btn_portrait_confirm", "Confirm portrait change", out PortraitConfirmButton, 30f, 30f, "ui_btn_sm_check", PortraitConfirmEnabled)
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private NuiElement BuildActionButtons()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 80f },
                        ImagePlatedLabeledButton("btn_save", "", "Save Changes", out SaveButton, "ui_btn_save"),
                        new NuiSpacer { Width = 20f },
                        ImagePlatedLabeledButton("btn_cancel", "", "Discard changes and revert to last save", out CancelButton, "ui_btn_discard"),
                        new NuiSpacer { Width = 20f },
                        ImagePlatedLabeledButton("btn_close", "", "Close window", out CloseButton, "ui_btn_cancel")
                    }
                }
            }
        };
    }

    private NuiElement ImageButton(string id, string tooltip, out NuiButtonImage button, float w, float h, string resRef, NuiBind<bool>? enabled = null)
    {
        button = new NuiButtonImage(resRef)
        {
            Id = id,
            Width = w,
            Height = h,
            Tooltip = tooltip,
            Enabled = enabled ?? new NuiBind<bool>("true")
        };
        return button;
    }

    private static NuiElement ImagePlatedLabeledButton(string id, string label, string tooltip, out NuiButtonImage logicalButton,
        string resRef, float width = 150f, float height = 38f)
    {
        NuiButtonImage btn = new NuiButtonImage(resRef)
        {
            Id = id,
            Width = width,
            Height = height,
            Tooltip = tooltip
        }.Assign(out logicalButton);

        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Width = width,
                    Children =
                    {
                        btn
                    }
                },
                new NuiRow
                {
                    Width = width,
                    Children =
                    {
                        new NuiLabel(label)
                        {
                            Width = width,
                            Height = 18f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                }
            }
        };
    }
}

