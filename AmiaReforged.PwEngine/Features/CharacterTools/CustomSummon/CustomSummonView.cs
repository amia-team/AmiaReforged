using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.CharacterTools.CustomSummon;

public sealed class CustomSummonView : ScryView<CustomSummonPresenter>
{
    private const float WindowW = 630f;
    private const float WindowH = 570f;
    private const float HeaderW = 540f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 4f;
    private const float HeaderLeftPad = 5f;

    public override CustomSummonPresenter Presenter { get; protected set; } = null!;

    // General control binds
    public readonly NuiBind<bool> AlwaysEnabled = new("cs_always_enabled");

    // List binds
    public readonly NuiBind<int> SummonCount = new("cs_summon_count");
    public readonly NuiBind<string> SummonNames = new("cs_summon_names");
    public readonly NuiBind<int> SelectedSummonIndex = new("cs_selected_summon");

    // Button references
    public NuiButtonImage SelectButton = null!;
    public NuiButtonImage CloseButton = null!;

    public CustomSummonView(NwPlayer player, NwItem widget)
    {
        Presenter = new CustomSummonPresenter(this, player, widget);
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
                        new NuiLabel("Select Your Summon")
                        {
                            Height = 25f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 10f },

                // Instructions
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 90f },
                        new NuiLabel("Click on a summon to select it, then click 'Save' to confirm.")
                        {
                            Height = 20f,
                            Width = 420f,
                            ForegroundColor = new Color(50, 40, 30)
                        }
                    }
                },
                new NuiSpacer { Height = 15f },

                // Summon list
                BuildSummonList(),

                new NuiSpacer { Height = 15f },

                // Action buttons
                BuildActionButtons()
            }
        };
    }

    private NuiElement BuildSummonList()
    {
        List<NuiListTemplateCell> summonRowTemplate =
        [
            new(new NuiButton(SummonNames)
            {
                Id = "cs_btn_select_summon",
                Width = 520f,
                Height = 35f
            })
        ];

        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 240f },
                        new NuiLabel("Available Summons:")
                        {
                            Width = 200f,
                            Height = 25f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 160f },
                        new NuiList(summonRowTemplate, SummonCount)
                        {
                            RowHeight = 40f,
                            Height = 175f,
                            Width = 300f
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
                        new NuiSpacer { Width = 180f },
                        new NuiButtonImage("ui_btn_save")
                        {
                            Id = "cs_btn_confirm",
                            Width = 120f,
                            Height = 35f,
                            Tooltip = "Set this summon as Active"
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButtonImage("ui_btn_cancel")
                        {
                            Id = "cs_btn_close",
                            Width = 120f,
                            Height = 35f,
                            Tooltip = "Close window"
                        }.Assign(out CloseButton)
                    }
                }
            }
        };
    }
}

