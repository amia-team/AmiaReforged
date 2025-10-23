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

    // sound binds
    public readonly NuiBind<string> DayMusicStr = new("day_music_str");

    public readonly NuiBind<string> NightMusicStr = new("night_music_str");

    public readonly NuiBind<string> BattleMusicStr = new("battle_music_str");

    // fog binds
    public readonly NuiBind<string> FogClipDistance = new("fog_clip_distance");
    public readonly NuiBind<string> DayFogColor = new("day_fog_color");
    public readonly NuiBind<string> DayDiffuse = new("day_diffuse");
    public readonly NuiBind<string> DayFogDensity = new("day_fog_density");

    public readonly NuiBind<string> NightFogColor = new("night_fog_color");
    public readonly NuiBind<string> NightDiffuse = new("night_diffuse");
    public readonly NuiBind<string> NightFogDensity = new("night_fog_color");

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
            new(new NuiLabel(AreaNames)),
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
            new(new NuiButtonImage("dm_goto")
            {
                Id = "btn_load_var",
                Aspect = 1f,
                Tooltip = "Load this area"
            })
            {
                Width = 30f,
                VariableSize = false
            }
        ];

        return new NuiColumn()
        {
            Children =
            [
                new NuiRow
                {
                    Children =
                    [
                        new NuiColumn()
                        {
                            Children =
                            [
                                new NuiLabel("Search:")
                                {
                                    Height = 15f
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
                            Element = new NuiColumn
                            {
                                Children =
                                [
                                    new NuiLabel("Saved Instances")
                                    {
                                        Height = 15f
                                    },
                                    new NuiList(savedInstances, SavedVariantCounts)
                                    {
                                        Width = 300f,
                                        Height = 200f
                                    },
                                    new NuiLabel("Sound Settings")
                                    {
                                        Height = 15f
                                    },
                                    new NuiGroup()
                                    {
                                        Height = 200f,
                                        Element = new NuiColumn()
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
                                                            Width = 90f
                                                        },
                                                        new NuiTextEdit("0", DayMusicStr, 5, false)
                                                        {
                                                            Width = 40f,
                                                            Tooltip = "Int value from ambientmusic.2da"
                                                        },
                                                    ]
                                                },
                                                new NuiRow()
                                                {
                                                    Children =
                                                    [
                                                        new NuiLabel("Night Music:")
                                                        {
                                                            Height = 15f,
                                                            Width = 90f
                                                        },
                                                        new NuiTextEdit("0", NightMusicStr, 5, false)
                                                        {
                                                            Width = 40f,
                                                            Tooltip = "Int value from ambientmusic.2da"
                                                        },
                                                    ]
                                                },
                                                new NuiRow()
                                                {
                                                    Children =
                                                    [
                                                        new NuiLabel("Battle Music:")
                                                        {
                                                            Height = 15f,
                                                            Width = 90f
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
                                    new NuiGroup()
                                    {
                                        Height = 200f,
                                        Element = new NuiColumn()
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
                                                            Width = 90f
                                                        },
                                                        new NuiTextEdit("0", FogClipDistance, 5, false)
                                                        {
                                                            Width = 40f,
                                                        },
                                                    ]
                                                },
                                                new NuiRow()
                                                {
                                                    Children =
                                                    [
                                                        new NuiLabel("Day Color:")
                                                        {
                                                            Height = 15f,
                                                            Width = 90f,
                                                        },
                                                        new NuiTextEdit("0", DayFogColor, 7, false)
                                                        {
                                                            Width = 80f,
                                                        },
                                                    ]
                                                },
                                                new NuiRow()
                                                {
                                                    Children =
                                                    [
                                                        new NuiLabel("Day Diffuse:")
                                                        {
                                                            Height = 15f,
                                                            Width = 90f,
                                                        },
                                                        new NuiTextEdit("0", DayDiffuse, 7, false)
                                                        {
                                                            Width = 80f,
                                                        },
                                                    ]
                                                },
                                                new NuiRow()
                                                {
                                                    Children =
                                                    [
                                                        new NuiLabel("Day Density:")
                                                        {
                                                            Height = 15f,
                                                            Width = 90f,
                                                        },
                                                        new NuiTextEdit("0", DayFogDensity, 3, false)
                                                        {
                                                            Width = 80f,
                                                        },
                                                    ]
                                                },

                                                new NuiRow()
                                                {
                                                    Children =
                                                    [
                                                        new NuiLabel("Night Color:")
                                                        {
                                                            Height = 15f,
                                                            Width = 90f,
                                                        },
                                                        new NuiTextEdit("0", NightFogColor, 7, false)
                                                        {
                                                            Width = 80f,
                                                        },
                                                    ]
                                                },
                                                new NuiRow()
                                                {
                                                    Children =
                                                    [
                                                        new NuiLabel("Night Diffuse:")
                                                        {
                                                            Height = 15f,
                                                            Width = 90f,
                                                        },
                                                        new NuiTextEdit("0", NightDiffuse, 7, false)
                                                        {
                                                            Width = 80f,
                                                        },
                                                    ]
                                                },
                                                new NuiRow()
                                                {
                                                    Children =
                                                    [
                                                        new NuiLabel("Night Density:")
                                                        {
                                                            Height = 15f,
                                                            Width = 90f,
                                                        },
                                                        new NuiTextEdit("0", NightFogDensity, 3, false)
                                                        {
                                                            Width = 80f,
                                                        },
                                                    ]
                                                }
                                            ]
                                        }
                                    }
                                ]
                            }
                        }
                    ]
                },
                new NuiRow()
                {
                    Children =
                    [
                        new NuiSpacer(),
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
