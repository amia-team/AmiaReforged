using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.BuildChecker;

public sealed class BuildCheckerView : ScryView<BuildCheckerPresenter>, IToolWindow
{
    private const float WindowW = 630f;
    private const float WindowH = 670f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 8f;
    private const float HeaderLeftPad = 5f;

    public override BuildCheckerPresenter Presenter { get; protected set; }

    // Binds
    public readonly NuiBind<string> CharacterInfo = new("char_info");
    public readonly NuiBind<string> LevelupInfo = new("levelup_info");
    public readonly NuiBind<int> LevelFilter = new("level_filter");
    public readonly NuiBind<bool> RedoButtonsEnabled = new("redo_buttons_enabled");
    public readonly NuiBind<string> AutoRebuildLevel = new("auto_rebuild_level");

    // Button IDs
    public readonly NuiButton RedoLastLevelButton = new("Redo Last Level") { Id = "btn_redo_last_level" };
    public readonly NuiButton RedoLast2LevelsButton = new("Redo Last 2 Levels") { Id = "btn_redo_last_2_levels" };
    public readonly NuiButton AutoRebuildButton = new("Auto-Rebuild") { Id = "btn_auto_rebuild" };

    public string Id => "playertools.buildchecker";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Character Build Inspector";
    public string CategoryTag { get; }

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public BuildCheckerView(NwPlayer player)
    {
        Presenter = new BuildCheckerPresenter(this, player);
        CategoryTag = "Character";
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

                // Character info display
                new NuiRow
                {
                    Height = 30f,
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

                // Levelup information display with redo buttons on the right
                new NuiRow
                {
                    Height = 400f,
                    Children =
                    [
                        new NuiSpacer { Width = 10f },
                        new NuiText(LevelupInfo)
                        {
                            Width = 500f,
                            Scrollbars = NuiScrollbars.Y
                        },
                        new NuiSpacer { Width = 10f },
                        // Button column on the right
                        new NuiColumn
                        {
                            Width = 50f,
                            Children =
                            [
                                new NuiSpacer { Height = 10f },
                                new NuiButtonImage("ui_btn_redo1")
                                {
                                    Id = "btn_redo_last_level",
                                    Width = 40f,
                                    Height = 40f,
                                    Tooltip = "Redo Last Level",
                                    Enabled = RedoButtonsEnabled
                                },
                                new NuiSpacer { Height = 10f },
                                new NuiButtonImage("ui_btn_redo2")
                                {
                                    Id = "btn_redo_last_2_levels",
                                    Width = 40f,
                                    Height = 40f,
                                    Tooltip = "Redo Last 2 Levels",
                                    Enabled = RedoButtonsEnabled
                                },
                                new NuiSpacer { Height = 10f },
                                new NuiButtonImage("cc_turn_right")
                                {
                                    Id = "btn_auto_rebuild",
                                    Width = 40f,
                                    Height = 40f,
                                    Tooltip = "Auto-Rebuild Character"
                                }
                            ]
                        }
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

    public NuiWindow BuildAutoRebuildModal()
    {
        const float modalW = 400f;
        const float modalH = 350f;

        NuiColumn layout = new NuiColumn
        {
            Width = modalW,
            Height = modalH,
            Children =
            [
                // Background
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    DrawList = new()
                    {
                        new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, modalW, modalH))
                    }
                },

                // Title
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiLabel("Auto-Rebuild")
                        {
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },

                new NuiSpacer { Height = 10f },

                // Description line 1
                new NuiRow
                {
                    Height = 30f,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("Auto-Rebuild your character.")
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },

                // Description line 2
                new NuiRow
                {
                    Height = 30f,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("You can use this once every 6 months.")
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },

                new NuiSpacer { Height = 20f },

                // Delevel to input
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("Delevel to:")
                        {
                            Width = 100f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiTextEdit("", AutoRebuildLevel, 2, false)
                        {
                            Width = 100f,
                            Tooltip = "Enter level 1-27 (must be lower than current level)"
                        }
                    ]
                },

                new NuiSpacer { Height = 30f },

                // Confirm and Cancel buttons
                new NuiRow
                {
                    Height = 50f,
                    Children =
                    [
                        new NuiSpacer { Width = 60f },
                        new NuiButtonImage("ui_btn_save")
                        {
                            Id = "btn_auto_rebuild_confirm",
                            Width = 128f,
                            Height = 32f,
                            Tooltip = "Confirm Auto-Rebuild"
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiButtonImage("ui_btn_cancel")
                        {
                            Id = "btn_auto_rebuild_cancel",
                            Width = 128f,
                            Height = 32f,
                            Tooltip = "Cancel"
                        }
                    ]
                }
            ]
        };

        return new NuiWindow(layout, "Auto-Rebuild")
        {
            Geometry = new NuiRect(450f, 250f, modalW, modalH),
            Resizable = true,
            Closable = true
        };
    }
}

