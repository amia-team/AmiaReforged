using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.AreaEdit;

public sealed class AreaEditorView : ScryView<AreaEditorPresenter>, IDmWindow
{
    public override AreaEditorPresenter Presenter { get; protected set; }

    public string Title => "Area Editor";
    public bool ListInDmTools => true;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    // Binds
    public readonly NuiBind<string> AreaNames = new("area_names");
    public readonly NuiBind<int> AreaCount = new("area_count");

    public readonly NuiBind<string> SavedVariantNames = new("saved_variant_names");
    public readonly NuiBind<int> SavedVariantCounts = new("saved_variant_count");
    public NuiBind<string> SearchBind { get; } = new("dm_search");

    public NuiButton SaveSettingsButton = null!;
    public NuiButton PickCurrentAreaButton = null!;
    public NuiButton ReloadCurrentAreaButton = null!;
    public NuiButton SaveNewInstanceButton = null!;


    // sound binds
    public readonly NuiBind<string> DayMusicStr = new("day_music_str");

    public readonly NuiBind<string> NightMusicStr = new("night_music_str");

    public readonly NuiBind<string> BattleMusicStr = new("battle_music_str");

    // fog binds
    public readonly NuiBind<string> FogClipDistance = new("fog_clip_distance");

    public readonly NuiBind<string> DayFogR = new("day_fog_r");
    public readonly NuiBind<string> DayFogG = new("day_fog_g");
    public readonly NuiBind<string> DayFogB = new("day_fog_b");
    public readonly NuiBind<string> DayFogA = new("day_fog_a");

    public readonly NuiBind<string> DayDiffuseR = new("day_diffuse_r");
    public readonly NuiBind<string> DayDiffuseG = new("day_diffuse_g");
    public readonly NuiBind<string> DayDiffuseB = new("day_diffuse_b");
    public readonly NuiBind<string> DayDiffuseA = new("day_diffuse_a");

    public readonly NuiBind<string> DayFogDensity = new("day_fog_density");

    public readonly NuiBind<string> NightFogR = new("night_fog_r");
    public readonly NuiBind<string> NightFogG = new("night_fog_g");
    public readonly NuiBind<string> NightFogB = new("night_fog_b");
    public readonly NuiBind<string> NightFogA = new("night_fog_a");

    public readonly NuiBind<string> NightDiffuseR = new("night_diffuse_r");
    public readonly NuiBind<string> NightDiffuseG = new("night_diffuse_g");
    public readonly NuiBind<string> NightDiffuseB = new("night_diffuse_b");
    public readonly NuiBind<string> NightDiffuseA = new("night_diffuse_a");

    public readonly NuiBind<bool> CanSaveArea = new("can_save_area");

    public readonly NuiBind<string> NightFogDensity = new("night_fog_color");

    public readonly NuiBind<string> NewAreaName = new("new_area_name");

    public AreaEditorView(NwPlayer player)
    {
        Presenter = new AreaEditorPresenter(this, player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }


    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> moduleAreas =
        [
            new(new NuiLabel(AreaNames)
            {
                Tooltip = AreaNames,
            }),
            new(new NuiButtonImage("dm_examine")
            {
                Id = "btn_pick_row",
                Aspect = 1f,
                Tooltip = "Pick this area"
            })
            {
                Width = 30f,
                VariableSize = false
            }
        ];

        List<NuiListTemplateCell> savedInstances =
        [
            new(new NuiLabel(SavedVariantNames)),
            new(new NuiButtonImage("ir_abort")
            {
                Id = "btn_delete_var",
                Aspect = 1f,
                Tooltip = "Delete this instance"
            })
            {
                Width = 30f,
                VariableSize = false
            },
            new(new NuiButtonImage("dm_goto")
            {
                Id = "btn_load_var",
                Aspect = 1f,
                Tooltip = "Load this instance (spawns new area)"
            })
            {
                Width = 30f,
                VariableSize = false
            }
        ];

        return new NuiColumn
        {
            Children =
            [
                new NuiRow
                {
                    Children =
                    [
                        new NuiColumn
                        {
                            Children =
                            [
                                new NuiLabel("Search:")
                                {
                                    Height = 15f,
                                    VerticalAlign = NuiVAlign.Middle,
                                },
                                new NuiTextEdit("type to filter...", SearchBind, 64, false)
                                {
                                    Width = 260f
                                },
                                new NuiList(moduleAreas, AreaCount),
                            ]
                        },
                        new NuiGroup
                        {
                            Element = new NuiRow
                            {
                                Children =
                                [
                                    // Column 1
                                    new NuiColumn()
                                    {
                                        Children =
                                        [
                                            new NuiRow()
                                            {
                                                Children =
                                                [
                                                    new NuiTextEdit("Type a name...", NewAreaName, 255, false)
                                                    {
                                                        Height = 30f,
                                                        Enabled = CanSaveArea
                                                    },
                                                    new NuiSpacer(),
                                                    new NuiButton("Save New Instance")
                                                    {
                                                        Id = "btn_save_instance",
                                                        Height = 30f,
                                                        Enabled = CanSaveArea,
                                                        DisabledTooltip = "You cannot create copies of a copy."
                                                    }.Assign(out SaveNewInstanceButton)
                                                ]
                                            },

                                            new NuiList(savedInstances, SavedVariantCounts)
                                            {
                                                Width = 300f,
                                                Height = 200f
                                            },
                                            new NuiLabel("Sound Settings")
                                            {
                                                Height = 15f,
                                                VerticalAlign = NuiVAlign.Middle,
                                            },

                                            // Music
                                            new NuiGroup
                                            {
                                                Height = 200f,
                                                Element = new NuiColumn
                                                {
                                                    Children =
                                                    [
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Day Music:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", DayMusicStr, 5, false)
                                                                {
                                                                    Width = 40f,
                                                                    Tooltip = "Int value from ambientmusic.2da"
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Night Music:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", NightMusicStr, 5, false)
                                                                {
                                                                    Width = 40f,
                                                                    Tooltip = "Int value from ambientmusic.2da"
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Battle Music:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", BattleMusicStr, 5, false)
                                                                {
                                                                    Width = 40f,
                                                                    Tooltip = "Int value from ambientmusic.2da"
                                                                },
                                                            ]
                                                        }
                                                    ]
                                                }
                                            },
                                            new NuiGroup
                                            {
                                                Height = 200f,
                                                Element = new NuiColumn
                                                {
                                                    Children =
                                                    [
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Fog Distance:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", FogClipDistance, 5, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },

                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Day Color R:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", DayFogR, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Day Color G:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", DayFogG, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Day Color G:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", DayFogB, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Day Color A:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", DayFogA, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },

                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Day Diffuse R:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", DayDiffuseR, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Day Diffuse G:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", DayDiffuseG, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Day Diffuse B:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", DayDiffuseB, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Day Diffuse A:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", DayDiffuseA, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Day Density:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", DayFogDensity, 3, false)
                                                                {
                                                                    Width = 80f,
                                                                },
                                                            ]
                                                        },

                                                        // ewe
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Night Color R:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", NightFogR, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Night Color G:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", NightFogG, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Night Color G:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", NightFogB, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Night Color A:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", NightFogA, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },

                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Night Diffuse R:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", NightDiffuseR, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Night Diffuse G:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", NightDiffuseG, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Night Diffuse B:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", NightDiffuseB, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Night Diffuse A:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", NightDiffuseA, 3, false)
                                                                {
                                                                    Width = 40f,
                                                                },
                                                            ]
                                                        },
                                                        new NuiRow
                                                        {
                                                            Children =
                                                            [
                                                                new NuiLabel("Night Density:")
                                                                {
                                                                    Height = 15f,
                                                                    Width = 90f,
                                                                    VerticalAlign = NuiVAlign.Middle,
                                                                },
                                                                new NuiTextEdit("0", NightFogDensity, 3, false)
                                                                {
                                                                    Width = 80f,
                                                                },
                                                            ]
                                                        }
                                                    ]
                                                }
                                            },
                                        ]
                                    },
                                    // Column 2
                                    new NuiColumn
                                    {
                                        Children =
                                        [
                                        ]
                                    }
                                ]
                            }
                        }
                    ]
                },
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiButton("Reload Current Area")
                        {
                            Id = "btn_reload_are"
                        }.Assign(out ReloadCurrentAreaButton),
                        new NuiButton("Pick Current Area")
                        {
                            Id = "btn_pick_curr"
                        }.Assign(out PickCurrentAreaButton),
                        new NuiButton("Save Settings")
                        {
                            Id = "btn_save_set"
                        }.Assign(out SaveSettingsButton)
                    ]
                }
            ]
        };
    }
}
