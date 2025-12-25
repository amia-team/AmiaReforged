using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.CharacterTools.CustomSummon;

public sealed class CustomSummonDmView : ScryView<CustomSummonDmPresenter>
{
    private const float WindowW = 630f;
    private const float WindowH = 570f;
    private const float HeaderW = 540f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 4f;
    private const float HeaderLeftPad = 5f;

    public override CustomSummonDmPresenter Presenter { get; protected set; } = null!;

    // General control binds
    public readonly NuiBind<bool> AlwaysEnabled = new("csdm_always_enabled");

    // List binds
    public readonly NuiBind<int> SummonCount = new("csdm_summon_count");
    public readonly NuiBind<string> SummonNames = new("csdm_summon_names");
    public readonly NuiBind<int> SelectedSummonIndex = new("csdm_selected_summon");

    public CustomSummonDmView(NwPlayer player, NwItem widget)
    {
        Presenter = new CustomSummonDmPresenter(this, player, widget);
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
                        new NuiSpacer { Width = 200f },
                        new NuiLabel("Custom Summon Manager")
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
                        new NuiSpacer { Width = 60f },
                        new NuiLabel("Add summons by clicking 'Add' and targeting a cust_summon template creature.")
                        {
                            Height = 35f,
                            Width = 500f,
                            ForegroundColor = new Color(50, 40, 30)
                        }
                    }
                },
                new NuiSpacer { Height = 10f },

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
                Id = "csdm_btn_select_summon",
                Width = 300f,
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
                        new NuiSpacer { Width = 165f },
                        new NuiList(summonRowTemplate, SummonCount)
                        {
                            RowHeight = 40f,
                            Height = 200f,
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
                        new NuiSpacer { Width = 120f },
                        new NuiButton("Add")
                        {
                            Id = "csdm_btn_add",
                            Width = 120f,
                            Height = 35f,
                            Tooltip = "Add a new summon (target a creature)"
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButton("Remove")
                        {
                            Id = "csdm_btn_remove",
                            Width = 120f,
                            Height = 35f,
                            Tooltip = "Remove selected summon"
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButton("Cancel")
                        {
                            Id = "csdm_btn_close",
                            Width = 120f,
                            Height = 35f,
                            Tooltip = "Close window"
                        }
                    }
                }
            }
        };
    }
}

