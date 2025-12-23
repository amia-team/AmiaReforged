using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.RebuildTool;

public sealed class RebuildToolView : ScryView<RebuildToolPresenter>, IDmWindow
{
    private const float WindowW = 630f;
    private const float WindowH = 780f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 8f;
    private const float HeaderLeftPad = 5f;

    public override RebuildToolPresenter Presenter { get; protected set; }

    // Binds
    public readonly NuiBind<bool> CharacterSelected = new("char_selected");
    public readonly NuiBind<string> CharacterInfo = new("char_info");
    public readonly NuiBind<string> LevelupInfo = new("levelup_info");
    public readonly NuiBind<string> FeatId = new("feat_id");
    public readonly NuiBind<string> Level = new("level");
    public readonly NuiBind<int> LevelFilter = new("level_filter");

    // Buttons
    public NuiButtonImage SelectCharacterButton = null!;
    public NuiButtonImage AddFeatButton = null!;
    public NuiButtonImage RemoveFeatButton = null!;

    public string Title => "Character Rebuild Tool";
    public bool ListInDmTools => true;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public RebuildToolView(NwPlayer player)
    {
        Presenter = new RebuildToolPresenter(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    private NuiElement BuildHeaderOverlay()
    {
        return new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = new()
            {
                new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))
            }
        };
    }

    public override NuiLayout RootLayout()
    {
        // Background parchment (draw-only)
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = new() { new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH)) }
        };

        NuiElement headerOverlay = BuildHeaderOverlay();
        NuiSpacer headerSpacer = new NuiSpacer { Height = HeaderH + HeaderTopPad + 6f };

        NuiColumn root = new()
        {
            Width = WindowW,
            Height = WindowH,
            Margin = 15f,
            Children =
            [
                bgLayer,
                headerOverlay,
                headerSpacer,

                // Character selection section
                new NuiRow
                {
                    Height = 50f,
                    Children =
                    [
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("Select Character:")
                        {
                            Width = 130f,
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Left,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiButtonImage("nui_pick")
                        {
                            Id = "btn_select_char",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "Select a player character"
                        }.Assign(out SelectCharacterButton)
                    ]
                },

                // Character info display
                new NuiRow
                {
                    Height = 30f,
                    Visible = CharacterSelected,
                    Children =
                    [
                        new NuiSpacer { Width = 10f },
                        new NuiLabel(CharacterInfo)
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Left,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },

                // Level filter dropdown
                new NuiRow
                {
                    Height = 35f,
                    Visible = CharacterSelected,
                    Children =
                    [
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("View Level:")
                        {
                            Width = 90f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiCombo
                        {
                            Width = 150f,
                            Selected = LevelFilter,
                            Entries = new NuiValue<List<NuiComboEntry>>([
                                new NuiComboEntry("All Levels", 0),
                                new NuiComboEntry("Level 1", 1),
                                new NuiComboEntry("Level 2", 2),
                                new NuiComboEntry("Level 3", 3),
                                new NuiComboEntry("Level 4", 4),
                                new NuiComboEntry("Level 5", 5),
                                new NuiComboEntry("Level 6", 6),
                                new NuiComboEntry("Level 7", 7),
                                new NuiComboEntry("Level 8", 8),
                                new NuiComboEntry("Level 9", 9),
                                new NuiComboEntry("Level 10", 10),
                                new NuiComboEntry("Level 11", 11),
                                new NuiComboEntry("Level 12", 12),
                                new NuiComboEntry("Level 13", 13),
                                new NuiComboEntry("Level 14", 14),
                                new NuiComboEntry("Level 15", 15),
                                new NuiComboEntry("Level 16", 16),
                                new NuiComboEntry("Level 17", 17),
                                new NuiComboEntry("Level 18", 18),
                                new NuiComboEntry("Level 19", 19),
                                new NuiComboEntry("Level 20", 20),
                                new NuiComboEntry("Level 21", 21),
                                new NuiComboEntry("Level 22", 22),
                                new NuiComboEntry("Level 23", 23),
                                new NuiComboEntry("Level 24", 24),
                                new NuiComboEntry("Level 25", 25),
                                new NuiComboEntry("Level 26", 26),
                                new NuiComboEntry("Level 27", 27),
                                new NuiComboEntry("Level 28", 28),
                                new NuiComboEntry("Level 29", 29),
                                new NuiComboEntry("Level 30", 30)
                            ])
                        }
                    ]
                },

                Divider(),

                // Levelup information display
                new NuiRow
                {
                    Height = 370f,
                    Width = 500f,
                    Visible = CharacterSelected,
                    Children =
                    [
                        new NuiSpacer { Width = 10f },
                        new NuiText(LevelupInfo)
                        {
                            Scrollbars = NuiScrollbars.Y
                        }
                    ]
                },

                Divider(),

                // Feat management label
                new NuiRow
                {
                    Height = 25f,
                    Visible = CharacterSelected,
                    Children =
                    [
                        new NuiLabel("Add or Remove Feats:")
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },

                // Feat management section
                new NuiRow
                {
                    Height = 50f,
                    Visible = CharacterSelected,
                    Children =
                    [
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("Feat ID:")
                        {
                            Width = 80f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiTextEdit("", FeatId, 10, false)
                        {
                            Width = 100f
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("Level:")
                        {
                            Width = 60f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiTextEdit("", Level, 2, false)
                        {
                            Width = 60f
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiButtonImage("ui_btn_sm_plus")
                        {
                            Id = "btn_add_feat",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "Add feat to character at specified level"
                        }.Assign(out AddFeatButton),
                        new NuiSpacer { Width = 10f },
                        new NuiButtonImage("ui_btn_sm_min")
                        {
                            Id = "btn_remove_feat",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "Remove feat from character"
                        }.Assign(out RemoveFeatButton)
                    ]
                }
            ]
        };

        return root;
    }

    private NuiElement Divider(float thickness = 1f, byte alpha = 48)
    {
        // Transparent row with a faint horizontal line
        return new NuiRow
        {
            Height = thickness + 4f, // a bit of breathing room
            DrawList = new()
            {
                // line across the full window width
                new NuiDrawListLine(new Color(0, 0, 0, alpha), false, thickness + 2f,
                    new NuiVector(0.0f, 100.0f),
                    new NuiVector(0.0f, 400.0f))
            }
        };
    }
}

