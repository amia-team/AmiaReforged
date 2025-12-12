using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.CharacterTools.HeightChanger;

public sealed class HeightChangerView : ScryView<HeightChangerPresenter>
{
    private const float WindowW = 660f;
    private const float WindowH = 480f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 4f;
    private const float HeaderLeftPad = 20f;

    public override HeightChangerPresenter Presenter { get; protected set; } = null!;

    // General control binds
    public readonly NuiBind<bool> AlwaysEnabled = new("hc_always_enabled");

    // Target selection binds
    public readonly NuiBind<int> TargetSelection = new("hc_target_selection");
    public readonly NuiBind<List<NuiComboEntry>> TargetOptions = new("hc_target_options");

    // Slider bind
    public readonly NuiBind<float> HeightSlider = new("hc_height_slider");
    public readonly NuiBind<string> HeightLabel = new("hc_height_label");

    // Button references
    public NuiCombo TargetComboBox = null!;
    public NuiButtonImage RefreshButton = null!;
    public NuiButton GroundButton = null!;
    public NuiButton Height05Button = null!;
    public NuiButton Height10Button = null!;
    public NuiButton Height15Button = null!;
    public NuiSliderFloat HeightSliderControl = null!;
    public NuiButtonImage CloseButton = null!;

    public HeightChangerView(NwPlayer player)
    {
        Presenter = new HeightChangerPresenter(this, player);
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
                // Title label
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 230f },
                        new NuiLabel("Height Adjustment Tool")
                        {
                            Height = 25f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 10f },

                // Target selection
                BuildTargetSelection(),

                // Quick settings buttons
                BuildQuickSettingsButtons(),

                // Slider
                BuildSlider(),

                // Action buttons
                BuildActionButtons()
            }
        };
    }

    private NuiElement BuildTargetSelection()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 100f },
                        new NuiLabel("Select Target:")
                        {
                            Width = 120f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiCombo
                        {
                            Width = 300f,
                            Height = 30f,
                            Entries = TargetOptions,
                            Selected = TargetSelection
                        }.Assign(out TargetComboBox),
                        new NuiSpacer { Width = 10f },
                        ImageButton("btn_refresh", "Refresh target list", out RefreshButton, 30f, 30f, "cc_turn_right", AlwaysEnabled)
                    }
                },
                new NuiSpacer { Height = 15f }
            }
        };
    }

    private NuiElement BuildQuickSettingsButtons()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 100f },
                        new NuiLabel("Quick Settings:")
                        {
                            Width = 120f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiButton("Ground")
                        {
                            Id = "btn_ground",
                            Width = 80f,
                            Height = 30f,
                            Tooltip = "Set height to ground level (0.0)"
                        }.Assign(out GroundButton),
                        new NuiSpacer { Width = 5f },
                        new NuiButton("0.5")
                        {
                            Id = "btn_05",
                            Width = 60f,
                            Height = 30f,
                            Tooltip = "Set height to 0.5"
                        }.Assign(out Height05Button),
                        new NuiSpacer { Width = 5f },
                        new NuiButton("1.0")
                        {
                            Id = "btn_10",
                            Width = 60f,
                            Height = 30f,
                            Tooltip = "Set height to 1.0"
                        }.Assign(out Height10Button),
                        new NuiSpacer { Width = 5f },
                        new NuiButton("1.5")
                        {
                            Id = "btn_15",
                            Width = 60f,
                            Height = 30f,
                            Tooltip = "Set height to 1.5"
                        }.Assign(out Height15Button)
                    }
                },
                new NuiSpacer { Height = 15f }
            }
        };
    }

    private NuiElement BuildSlider()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 100f },
                        new NuiLabel("Fine Adjustment:")
                        {
                            Width = 120f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiSliderFloat(HeightSlider, 0.0f, 2.0f)
                        {
                            Width = 300f,
                            Height = 30f,
                            Tooltip = "Adjust height from 0.0 to 2.0"
                        }.Assign(out HeightSliderControl),
                        new NuiSpacer { Width = 10f },
                        new NuiLabel(HeightLabel)
                        {
                            Width = 50f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 15f }
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
                        new NuiSpacer { Width = 230f },
                        ImagePlatedLabeledButton("btn_close", "", "Close window", out CloseButton, "ui_btn_cancel")
                    }
                }
            }
        };
    }

    private NuiElement ImageButton(string id, string tooltip, out NuiButtonImage button, float w, float h, string resRef, NuiBind<bool>? enabled = null)
    {
        NuiBind<bool> enabledBind = enabled ?? AlwaysEnabled;

        button = new NuiButtonImage(resRef)
        {
            Id = id,
            Width = w,
            Height = h,
            Tooltip = tooltip,
            Enabled = enabledBind
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
                btn
            }
        };
    }
}

