using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.Module;
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
    public readonly NuiBind<string> RebuildLevel = new("rebuild_level");
    public readonly NuiBind<string> ReturnToLevel = new("return_to_level");
    public readonly NuiBind<string> CurrentRaceInfo = new("current_race_info");
    public readonly NuiBind<int> SelectedRaceIndex = new("selected_race_index");
    public readonly NuiBind<string> SubRaceInput = new("subrace_input");
    public readonly NuiBind<string> FullRebuildReturnLevel = new("full_rebuild_return_level");
    public readonly NuiBind<int> SelectedPendingRebuild = new("selected_pending_rebuild");
    public readonly NuiBind<string> FeatSearchText = new("feat_search_text");

    // Buttons
    public NuiButtonImage SelectCharacterButton = null!;
    public NuiButtonImage AddFeatButton = null!;
    public NuiButtonImage RemoveFeatButton = null!;
    public NuiButtonImage SearchFeatButton = null!;
    public NuiButtonImage InitiateRebuildButton = null!;
    public NuiButton ViewAllFeatsButton = null!;
    public NuiButtonImage RaceOptionsButton = null!;

    public string Title => "Character Rebuild Tool";
    public bool ListInDmTools => true;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public RebuildToolView(NwPlayer player)
    {
        // Resolve dependencies from the service container
        IRebuildRepository repository = AnvilCore.GetService<IRebuildRepository>()!;
        FeatCache featCache = AnvilCore.GetService<FeatCache>()!;

        Presenter = new RebuildToolPresenter(this, player, repository, featCache);
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
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiButton("View All Feats")
                        {
                            Id = "btn_view_all_feats",
                            Width = 130f,
                            Height = 30f,
                            Tooltip = "Display all feats the character has"
                        }.Assign(out ViewAllFeatsButton)
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
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiColumn
                        {
                            Children =
                            [
                                new NuiButtonImage("app_roll")
                                {
                                    Id = "btn_initiate_rebuild",
                                    Width = 50f,
                                    Height = 50f,
                                    Tooltip = "Initiate Rebuild"
                                }.Assign(out InitiateRebuildButton),
                                new NuiLabel("Rebuild")
                                {
                                    Height = 25f,
                                    HorizontalAlign = NuiHAlign.Center,
                                    ForegroundColor = new Color(30, 20, 12)
                                },
                                new NuiSpacer { Height = 10f },
                                new NuiButtonImage("app_save")
                                {
                                    Id = "btn_race_options",
                                    Width = 50f,
                                    Height = 50f,
                                    Tooltip = "Race Options"
                                }.Assign(out RaceOptionsButton),
                                new NuiLabel("Race")
                                {
                                    Height = 25f,
                                    HorizontalAlign = NuiHAlign.Center,
                                    ForegroundColor = new Color(30, 20, 12)
                                }
                            ]
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
                        }.Assign(out RemoveFeatButton),
                        new NuiSpacer { Width = 10f },
                        new NuiButtonImage("isk_search")
                        {
                            Id = "btn_search_feat",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "Search for feats in feat.2da"
                        }.Assign(out SearchFeatButton)
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

    public NuiWindow BuildRebuildModal()
    {
        const float modalW = 370f;
        const float modalH = 430f;

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
                    Children =
                    [
                        new NuiSpacer { Width = 80f },
                        new NuiLabel("Choose a Rebuild Type:")
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },

                new NuiSpacer { Height = 20f },

                // Rebuild type buttons
                new NuiRow
                {
                    Height = 50f,
                    Children =
                    [
                        new NuiSpacer { Width = 10f },
                        new NuiButton("Full Rebuild")
                        {
                            Id = "btn_full_rebuild",
                            Width = 150f,
                            Height = 40f
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiButton("Partial Rebuild")
                        {
                            Id = "btn_partial_rebuild",
                            Width = 150f,
                            Height = 40f
                        },
                        new NuiSpacer()
                    ]
                },

                new NuiSpacer { Height = 20f },

                // Level input for partial rebuild
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiSpacer { Width = 70f },
                        new NuiLabel("Delevel To (1-29):")
                        {
                            Width = 130f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiTextEdit("", new NuiBind<string>("rebuild_level"), 2, false)
                        {
                            Width = 80f,
                            Tooltip = "Input a number to delevel to a specific level"
                        }
                    ]
                },

                new NuiSpacer { Height = 10f },

                // Return to Level input
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiSpacer { Width = 35f },
                        new NuiLabel("Return to Level (2-30):")
                        {
                            Width = 165f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiTextEdit("", new NuiBind<string>("return_to_level"), 2, false)
                        {
                            Width = 80f,
                            Tooltip = "Leave empty to return all XP"
                        }
                    ]
                },

                new NuiSpacer { Height = 10f },

                // Return All XP button
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiSpacer {Width = 100f},
                        new NuiButton("Return XP")
                        {
                            Id = "btn_return_all_xp",
                            Width = 150f,
                            Height = 35f,
                            Tooltip = "Return an amount of removed XP to the player"
                        },
                    ]
                },

                new NuiSpacer { Height = 20f },

                // Cancel button
                new NuiRow
                {
                    Height = 50f,
                    Children =
                    [
                        new NuiSpacer{Width = 100f},
                        new NuiButtonImage("ui_btn_cancel")
                        {
                            Id = "btn_rebuild_cancel",
                            Width = 150f,
                            Height = 35f,
                            Tooltip = "Cancel"
                        },
                    ]
                }
            ]
        };

        return new NuiWindow(layout, "Character Rebuild")
        {
            Geometry = new NuiRect(450f, 250f, modalW, modalH),
            Resizable = false,
            Closable = true
        };
    }

    public NuiWindow BuildRaceOptionsModal(List<NuiComboEntry> raceEntries)
    {
        const float modalW = 390f;
        const float modalH = 420f;

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
                        new NuiLabel("Race Options")
                        {
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },

                new NuiSpacer { Height = 10f },

                // Current Race Display
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("Current Race:")
                        {
                            Width = 110f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiLabel(CurrentRaceInfo)
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },

                new NuiSpacer { Height = 20f },

                // Race Selection Dropdown
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("Select New Race:")
                        {
                            Width = 130f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiCombo
                        {
                            Width = 200f,
                            Selected = SelectedRaceIndex,
                            Entries = new NuiValue<List<NuiComboEntry>>(raceEntries)
                        }
                    ]
                },

                new NuiSpacer { Height = 20f },

                // Optional Subrace Input
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("Subrace (Optional):")
                        {
                            Width = 130f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiTextEdit("", SubRaceInput, 32, false)
                        {
                            Width = 200f,
                            Tooltip = "Leave empty to not change subrace"
                        }
                    ]
                },

                new NuiSpacer { Height = 5f },

                // Clear Subrace button
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiSpacer { Width = 155f },
                        new NuiButton("Clear Subrace")
                        {
                            Id = "btn_clear_subrace",
                            Width = 200f,
                            Height = 35f,
                            Tooltip = "Clear the character's subrace field"
                        }
                    ]
                },

                new NuiSpacer { Height = 15f },

                // Save and Cancel buttons
                new NuiRow
                {
                    Height = 50f,
                    Children =
                    [
                        new NuiSpacer { Width = 60f },
                        new NuiButtonImage("ui_btn_save")
                        {
                            Id = "btn_race_save",
                            Width = 128f,
                            Height = 32f,
                            Tooltip = "Save race change"
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiButtonImage("ui_btn_cancel")
                        {
                            Id = "btn_race_cancel",
                            Width = 128f,
                            Height = 32f,
                            Tooltip = "Cancel"
                        }
                    ]
                }
            ]
        };

        return new NuiWindow(layout, "Race Options")
        {
            Geometry = new NuiRect(450f, 250f, modalW, modalH),
            Resizable = false,
            Closable = true
        };
    }

    public NuiWindow BuildFullRebuildModal()
    {
        const float modalW = 340f;
        const float modalH = 550f;

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
                        new NuiSpacer { Width = 80f },
                        new NuiLabel("Full Character Rebuild")
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },

                new NuiSpacer { Height = 10f },

                // Start Rebuild button
                new NuiRow
                {
                    Height = 45f,
                    Children =
                    [
                        new NuiSpacer { Width = 80f },
                        new NuiButton("Start Rebuild")
                        {
                            Id = "btn_start_full_rebuild",
                            Width = 150f,
                            Height = 40f,
                            Tooltip = "Begin the full rebuild process"
                        }
                    ]
                },

                new NuiSpacer { Height = 5f },

                // Return Inventory button
                new NuiRow
                {
                    Height = 45f,
                    Children =
                    [
                        new NuiSpacer { Width = 80f },
                        new NuiButton("Return Inventory")
                        {
                            Id = "btn_return_inventory",
                            Width = 150f,
                            Height = 40f,
                            Tooltip = "Restore items and gold to the new character"
                        }
                    ]
                },

                new NuiSpacer { Height = 5f },

                // Return XP section
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiSpacer { Width = 75f },
                        new NuiLabel("Return to Level (2-30):")
                        {
                            Width = 165f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },
                new NuiRow
                {
                    Height = 45f,
                    Children =
                    {
                        new NuiSpacer { Width = 125f },
                        new NuiTextEdit("", FullRebuildReturnLevel, 2, false)
                        {
                            Width = 60f,
                            Tooltip = "Leave empty to return all XP"
                        }
                    }
                },
                new NuiRow
                {
                    Height = 45f,
                    Children =
                    [
                        new NuiSpacer { Width = 80f },
                        new NuiButton("Return XP")
                        {
                            Id = "btn_full_rebuild_return_xp",
                            Width = 150f,
                            Height = 40f,
                            Tooltip = "Return XP to the character"
                        }
                    ]
                },

                new NuiSpacer { Height = 5f },

                // Finish button
                new NuiRow
                {
                    Height = 45f,
                    Children =
                    [
                        new NuiSpacer { Width = 80f },
                        new NuiButton("Finish")
                        {
                            Id = "btn_finish_full_rebuild",
                            Width = 150f,
                            Height = 40f,
                            Tooltip = "Complete and finalize the rebuild (cannot be undone)"
                        }
                    ]
                },

                new NuiSpacer { Height = 5f },

                // Find Rebuild button
                new NuiRow
                {
                    Height = 45f,
                    Children =
                    [
                        new NuiSpacer { Width = 80f },
                        new NuiButton("Find Rebuild")
                        {
                            Id = "btn_find_rebuild",
                            Width = 150f,
                            Height = 40f,
                            Tooltip = "Find a pending rebuild"
                        }
                    ]
                },

                new NuiSpacer { Height = 5f },

                // Cancel button
                new NuiRow
                {
                    Height = 45f,
                    Children =
                    [
                        new NuiSpacer { Width = 80f },
                        new NuiButtonImage("ui_btn_cancel")
                        {
                            Id = "btn_full_rebuild_cancel",
                            Width = 150f,
                            Height = 35f,
                            Tooltip = "Close window"
                        }
                    ]
                }
            ]
        };

        return new NuiWindow(layout, "Full Rebuild")
        {
            Geometry = new NuiRect(400f, 200f, modalW, modalH),
            Resizable = false,
            Closable = true
        };
    }

    public NuiWindow BuildFindRebuildModal(List<NuiComboEntry> rebuildEntries)
    {
        const float modalW = 400f;
        const float modalH = 300f;

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
                        new NuiLabel("Find Pending Rebuild")
                        {
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },

                new NuiSpacer { Height = 20f },

                // Rebuild Selection Dropdown
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("Character:")
                        {
                            Width = 100f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiCombo
                        {
                            Width = 250f,
                            Selected = SelectedPendingRebuild,
                            Entries = new NuiValue<List<NuiComboEntry>>(rebuildEntries)
                        }
                    ]
                },

                new NuiSpacer { Height = 40f },

                // Select and Cancel buttons
                new NuiRow
                {
                    Height = 50f,
                    Children =
                    [
                        new NuiSpacer { Width = 60f },
                        new NuiButton("Select")
                        {
                            Id = "btn_select_pending_rebuild",
                            Width = 120f,
                            Height = 35f,
                            Tooltip = "Load this rebuild"
                        },
                        new NuiSpacer { Width = 20f },
                        new NuiButtonImage("ui_btn_cancel")
                        {
                            Id = "btn_find_rebuild_cancel",
                            Width = 128f,
                            Height = 32f,
                            Tooltip = "Cancel"
                        }
                    ]
                }
            ]
        };

        return new NuiWindow(layout, "Find Rebuild")
        {
            Geometry = new NuiRect(450f, 300f, modalW, modalH),
            Resizable = true,
            Closable = true
        };
    }

    public NuiWindow BuildFeatSearchModal(List<(int id, string name)> feats)
    {
        const float modalW = 600f;
        const float modalH = 700f;

        // Build rows for each feat
        List<NuiElement> featRows = new();

        foreach (var feat in feats)
        {
            featRows.Add(new NuiRow
            {
                Height = 30f,
                Children =
                [
                    new NuiSpacer { Width = 10f },
                    new NuiLabel($"{feat.id}")
                    {
                        Width = 60f,
                        VerticalAlign = NuiVAlign.Middle,
                        ForegroundColor = new Color(30, 20, 12)
                    },
                    new NuiLabel(feat.name)
                    {
                        Width = 450f,
                        VerticalAlign = NuiVAlign.Middle,
                        ForegroundColor = new Color(30, 20, 12)
                    },
                    new NuiButtonImage("ui_btn_sm_plus")
                    {
                        Id = $"btn_add_feat_{feat.id}",
                        Width = 25f,
                        Height = 25f,
                        Tooltip = $"Add {feat.name} to character"
                    }
                ]
            });
        }

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
                        new NuiLabel("Feat Search - All Feats")
                        {
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },

                new NuiSpacer { Height = 10f },

                // Search field
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("Search:")
                        {
                            Width = 70f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiTextEdit("Type feat name...", FeatSearchText, 50, false)
                        {
                            Width = 350f,
                            Tooltip = "Enter part of a feat name to search"
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButton("Search")
                        {
                            Id = "btn_feat_search",
                            Width = 100f,
                            Height = 35f,
                            Tooltip = "Search for feats"
                        }
                    ]
                },

                new NuiSpacer { Height = 10f },

                // Column headers
                new NuiRow
                {
                    Height = 30f,
                    Children =
                    [
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("ID")
                        {
                            Width = 60f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiLabel("Feat Name")
                        {
                            Width = 450f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiLabel("Add")
                        {
                            Width = 35f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },

                // Feat list
                new NuiColumn
                {
                    Height = 520f,
                    Children = featRows
                },

                new NuiSpacer { Height = 10f },

                // Close button
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiSpacer { Width = 225f },
                        new NuiButtonImage("ui_btn_cancel")
                        {
                            Id = "btn_feat_search_close",
                            Width = 128f,
                            Height = 32f,
                            Tooltip = "Close"
                        }
                    ]
                }
            ]
        };

        return new NuiWindow(layout, "Feat Search")
        {
            Geometry = new NuiRect(350f, 100f, modalW, modalH),
            Resizable = true,
            Closable = true
        };
    }
}









