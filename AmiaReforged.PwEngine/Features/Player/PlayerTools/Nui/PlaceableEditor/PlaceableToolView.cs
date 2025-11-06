using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.PlaceableEditor;

public sealed class PlaceableToolView : ScryView<PlaceableToolPresenter>, IToolWindow
{
    private const float WindowWidth = 520f;
    private const float ContentWidth = WindowWidth - 16f;
    private const float SectionSpacing = 6f;

    public NuiButton RecoverButton = null!;

    public PlaceableToolView(NwPlayer player)
    {
        Presenter = new PlaceableToolPresenter(this, player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override PlaceableToolPresenter Presenter { get; protected set; }

    public string Id => "player.placeable.editor";
    public string Title => "Placeable Editor";
    public string CategoryTag => "World";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;

    public NuiButton RefreshButton = null!;
    public NuiButton SelectExistingButton = null!;
    public NuiButton SpawnButton = null!;
    public NuiButton SaveButton = null!;
    public NuiButton DiscardButton = null!;

    public readonly NuiBind<int> BlueprintCount = new("player_plc_bp_count");
    public readonly NuiBind<string> BlueprintNames = new("player_plc_bp_names");
    public readonly NuiBind<string> BlueprintResRefs = new("player_plc_bp_resrefs");

    public readonly NuiBind<bool> SelectionAvailable = new("player_plc_selected_available");
    public readonly NuiBind<string> SelectedName = new("player_plc_selected_name");
    public readonly NuiBind<string> SelectedLocation = new("player_plc_selected_location");

    public readonly NuiBind<string> StatusMessage = new("player_plc_status_message");

    public readonly NuiBind<float> PositionX = new("player_plc_pos_x");
    public readonly NuiBind<float> PositionY = new("player_plc_pos_y");
    public readonly NuiBind<float> PositionZ = new("player_plc_pos_z");
    public readonly NuiBind<string> PositionXString = new("player_plc_pos_x_str");
    public readonly NuiBind<string> PositionYString = new("player_plc_pos_y_str");
    public readonly NuiBind<string> PositionZString = new("player_plc_pos_z_str");

    public readonly NuiBind<float> TransformX = new("player_plc_trans_x");
    public readonly NuiBind<float> TransformY = new("player_plc_trans_y");
    public readonly NuiBind<float> TransformZ = new("player_plc_trans_z");
    public readonly NuiBind<string> TransformXString = new("player_plc_trans_x_str");
    public readonly NuiBind<string> TransformYString = new("player_plc_trans_y_str");
    public readonly NuiBind<string> TransformZString = new("player_plc_trans_z_str");

    public readonly NuiBind<float> RotationX = new("player_plc_rot_x");
    public readonly NuiBind<float> RotationY = new("player_plc_rot_y");
    public readonly NuiBind<float> RotationZ = new("player_plc_rot_z");
    public readonly NuiBind<string> RotationXString = new("player_plc_rot_x_str");
    public readonly NuiBind<string> RotationYString = new("player_plc_rot_y_str");
    public readonly NuiBind<string> RotationZString = new("player_plc_rot_z_str");

    public readonly NuiBind<float> Scale = new("player_plc_scale");
    public readonly NuiBind<string> ScaleString = new("player_plc_scale_str");

    public readonly NuiBind<float> Orientation = new("player_plc_orientation");
    public readonly NuiBind<string> OrientationString = new("player_plc_orientation_str");

    public override NuiLayout RootLayout()
    {
        return new NuiColumn
        {
            Width = WindowWidth,
            Children =
            {
                new NuiGroup
                {
                    Border = true,
                    Width = WindowWidth,
                    Padding = 6f,
                    Element = BuildContent()
                }
            }
        };
    }

    private NuiColumn BuildContent()
    {
        return new NuiColumn
        {
            Width = ContentWidth,
            Children =
            {
                BuildToolbarRow(),
                new NuiSpacer { Height = SectionSpacing },
                BuildBlueprintList(),
                new NuiSpacer { Height = SectionSpacing },
                BuildSelectionSummary(),
                new NuiSpacer { Height = SectionSpacing },
                BuildTransformSection(),
                new NuiSpacer { Height = SectionSpacing },
                BuildActionRow(),
                new NuiSpacer { Height = SectionSpacing },
                new NuiButton("Recover Selected")
                {
                    Id = "btn_recover",
                    Height = 32f,
                    Enabled = SelectionAvailable
                }.Assign(out RecoverButton),
                new NuiSpacer { Height = SectionSpacing },
                new NuiLabel(StatusMessage)
                {
                    Height = 18f,
                    ForegroundColor = ColorConstants.Orange,
                    HorizontalAlign = NuiHAlign.Center
                }
            }
        };
    }

    private NuiRow BuildToolbarRow()
    {
        return new NuiRow
        {
            Height = 40f,
            Children =
            {
                new NuiButton("Refresh Blueprints")
                {
                    Id = "btn_refresh",
                    Width = (ContentWidth / 2f) - 4f
                }.Assign(out RefreshButton),
                new NuiSpacer { Width = 8f },
                new NuiButton("Select Existing")
                {
                    Id = "btn_select",
                    Width = (ContentWidth / 2f) - 4f
                }.Assign(out SelectExistingButton)
            }
        };
    }

    private NuiList BuildBlueprintList()
    {
        List<NuiListTemplateCell> rowTemplate =
        [
            new(new NuiLabel(BlueprintNames)
            {
                VerticalAlign = NuiVAlign.Middle,
                HorizontalAlign = NuiHAlign.Left
            })
            {
                VariableSize = true
            },
            new(new NuiButton("Target Spawn")
            {
                Id = "btn_spawn",
                Height = 32f
            }.Assign(out SpawnButton))
            {
                VariableSize = false,
                Width = 110f
            }
        ];

        return new NuiList(rowTemplate, BlueprintCount)
        {
            RowHeight = 36f,
            Width = ContentWidth,
            Height = 180f
        };
    }

    private NuiGroup BuildSelectionSummary()
    {
        return new NuiGroup
        {
            Border = true,
            Height = 100f,
            Width = ContentWidth,
            Enabled = SelectionAvailable,
            Element = new NuiColumn
            {
                Children =
                {
                    new NuiLabel("Selected Placeable")
                    {
                        Height = 18f,
                        ForegroundColor = ColorConstants.White,
                        HorizontalAlign = NuiHAlign.Center
                    },
                    new NuiLabel(SelectedName)
                    {
                        Height = 18f,
                        HorizontalAlign = NuiHAlign.Center
                    },
                    new NuiLabel(SelectedLocation)
                    {
                        Height = 18f,
                        HorizontalAlign = NuiHAlign.Center
                    }
                }
            }
        };
    }

    private NuiGroup BuildTransformSection()
    {
        return new NuiGroup
        {
            Border = true,
            Height = 380f,
            Width = ContentWidth,
            Element = new NuiColumn
            {
                Children =
                {
                    new NuiLabel("Position")
                    {
                        Height = 18f,
                        HorizontalAlign = NuiHAlign.Center
                    },
                    BuildVectorRow("X", PositionXString, PositionX, -100f, 100f, "player_plc_pos_x_slider"),
                    BuildVectorRow("Y", PositionYString, PositionY, -100f, 100f, "player_plc_pos_y_slider"),
                    BuildVectorRow("Z", PositionZString, PositionZ, -100f, 100f, "player_plc_pos_z_slider"),
                    new NuiSpacer { Height = SectionSpacing },
                    new NuiLabel("Orientation")
                    {
                        Height = 18f,
                        HorizontalAlign = NuiHAlign.Center
                    },
                    BuildOrientationRow(),
                    new NuiSpacer { Height = SectionSpacing },
                    new NuiLabel("Visual Translation")
                    {
                        Height = 18f,
                        HorizontalAlign = NuiHAlign.Center
                    },
                    BuildVectorRow("X", TransformXString, TransformX, -10f, 10f, "player_plc_trans_x_slider"),
                    BuildVectorRow("Y", TransformYString, TransformY, -10f, 10f, "player_plc_trans_y_slider"),
                    BuildVectorRow("Z", TransformZString, TransformZ, -10f, 10f, "player_plc_trans_z_slider"),
                    new NuiSpacer { Height = SectionSpacing },
                    new NuiLabel("Visual Rotation")
                    {
                        Height = 18f,
                        HorizontalAlign = NuiHAlign.Center
                    },
                    BuildVectorRow("X", RotationXString, RotationX, -360f, 360f, "player_plc_rot_x_slider"),
                    BuildVectorRow("Y", RotationYString, RotationY, -360f, 360f, "player_plc_rot_y_slider"),
                    BuildVectorRow("Z", RotationZString, RotationZ, -360f, 360f, "player_plc_rot_z_slider"),
                    new NuiSpacer { Height = SectionSpacing },
                    BuildScaleRow()
                }
            }
        };
    }

    private NuiRow BuildScaleRow()
    {
        return new NuiRow
        {
            Height = 40f,
            Children =
            {
                new NuiLabel("Scale")
                {
                    Width = 40f,
                    VerticalAlign = NuiVAlign.Middle,
                    HorizontalAlign = NuiHAlign.Center
                },
                new NuiTextEdit("0", ScaleString, 10, false)
                {
                    Width = 100f,
                    Enabled = SelectionAvailable
                },
                new NuiSliderFloat(Scale, 0.001f, 3.0f)
                {
                    Width = ContentWidth - 160f,
                    Enabled = SelectionAvailable,
                    Id = "player_plc_scale_slider"
                }
            }
        };
    }

    private NuiRow BuildOrientationRow()
    {
        return new NuiRow
        {
            Height = 40f,
            Children =
            {
                new NuiLabel("deg")
                {
                    Width = 40f,
                    VerticalAlign = NuiVAlign.Middle,
                    HorizontalAlign = NuiHAlign.Center
                },
                new NuiTextEdit("0", OrientationString, 10, false)
                {
                    Width = 100f,
                    Enabled = SelectionAvailable
                },
                new NuiSliderFloat(Orientation, 0, 360)
                {
                    Width = ContentWidth - 160f,
                    Enabled = SelectionAvailable,
                    Id = "player_plc_orientation_slider"
                }
            }
        };
    }

    private NuiRow BuildActionRow()
    {
        return new NuiRow
        {
            Height = 42f,
            Children =
            {
                new NuiButton("Save Changes")
                {
                    Id = "btn_save",
                    Height = 32f,
                    Width = (ContentWidth / 2f) - 4f
                }.Assign(out SaveButton),
                new NuiSpacer { Width = 8f },
                new NuiButton("Discard Changes")
                {
                    Id = "btn_discard",
                    Height = 32f,
                    Width = (ContentWidth / 2f) - 4f
                }.Assign(out DiscardButton)
            }
        };
    }

    private NuiRow BuildVectorRow(string label, NuiBind<string> stringBind, NuiBind<float> floatBind,
        float minimum, float maximum, string sliderId)
    {
        return new NuiRow
        {
            Height = 38f,
            Children =
            {
                new NuiLabel(label)
                {
                    Width = 24f,
                    VerticalAlign = NuiVAlign.Middle,
                    HorizontalAlign = NuiHAlign.Center
                },
                new NuiTextEdit("0", stringBind, 16, false)
                {
                    Width = 100f,
                    Enabled = SelectionAvailable
                },
                new NuiSliderFloat(floatBind, minimum, maximum)
                {
                    Width = ContentWidth - 144f,
                    Enabled = SelectionAvailable,
                    Id = sliderId
                }
            }
        };
    }

    public bool ShouldListForPlayer(NwPlayer player)
    {
        NwArea? area = player.ControlledCreature?.Area;
        if (area == null)
        {
            return false;
        }

        PlaceablePersistenceMode mode = area.GetPlaceablePersistenceMode();
        return mode != PlaceablePersistenceMode.None;
    }

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;
}
