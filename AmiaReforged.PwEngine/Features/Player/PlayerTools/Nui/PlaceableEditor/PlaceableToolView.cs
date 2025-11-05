using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.PlaceableEditor;

public sealed class PlaceableToolView : ScryView<PlaceableToolPresenter>, IToolWindow
{
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

    public readonly NuiBind<int> BlueprintCount = new("plc_bp_count");
    public readonly NuiBind<string> BlueprintNames = new("plc_bp_names");
    public readonly NuiBind<string> BlueprintResRefs = new("plc_bp_resrefs");

    public readonly NuiBind<bool> SelectionAvailable = new("plc_selected_available");
    public readonly NuiBind<string> SelectedName = new("plc_selected_name");
    public readonly NuiBind<string> SelectedLocation = new("plc_selected_location");

    public readonly NuiBind<string> StatusMessage = new("plc_status_message");

    public readonly NuiBind<float> PositionX = new("plc_pos_x");
    public readonly NuiBind<float> PositionY = new("plc_pos_y");
    public readonly NuiBind<float> PositionZ = new("plc_pos_z");
    public readonly NuiBind<string> PositionXString = new("plc_pos_x_str");
    public readonly NuiBind<string> PositionYString = new("plc_pos_y_str");
    public readonly NuiBind<string> PositionZString = new("plc_pos_z_str");

    public readonly NuiBind<float> TransformX = new("plc_trans_x");
    public readonly NuiBind<float> TransformY = new("plc_trans_y");
    public readonly NuiBind<float> TransformZ = new("plc_trans_z");
    public readonly NuiBind<string> TransformXString = new("plc_trans_x_str");
    public readonly NuiBind<string> TransformYString = new("plc_trans_y_str");
    public readonly NuiBind<string> TransformZString = new("plc_trans_z_str");

    public readonly NuiBind<float> RotationX = new("plc_rot_x");
    public readonly NuiBind<float> RotationY = new("plc_rot_y");
    public readonly NuiBind<float> RotationZ = new("plc_rot_z");
    public readonly NuiBind<string> RotationXString = new("plc_rot_x_str");
    public readonly NuiBind<string> RotationYString = new("plc_rot_y_str");
    public readonly NuiBind<string> RotationZString = new("plc_rot_z_str");

    public readonly NuiBind<float> Scale = new("plc_scale");
    public readonly NuiBind<string> ScaleString = new("plc_scale_str");

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> rowTemplate =
        [
            new(new NuiLabel(BlueprintNames)
            {
                VerticalAlign = NuiVAlign.Middle
            })
            {
                VariableSize = true
            },
            new(new NuiLabel(BlueprintResRefs)
            {
                VerticalAlign = NuiVAlign.Middle
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

        return new NuiColumn
        {
            Children =
            [
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiButton("Refresh Blueprints")
                        {
                            Id = "btn_refresh"
                        }.Assign(out RefreshButton),
                        new NuiButton("Select Existing")
                        {
                            Id = "btn_select"
                        }.Assign(out SelectExistingButton)
                    ]
                },
                new NuiList(rowTemplate, BlueprintCount)
                {
                    RowHeight = 36f,
                    Width = 0f,
                    Height = 180f
                },
                new NuiGroup
                {
                    Border = true,
                    Height = 100f,
                    Enabled = SelectionAvailable,
                    Element = new NuiColumn
                    {
                        Children =
                        [
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
                        ]
                    }
                },
                new NuiGroup
                {
                    Border = true,
                    Height = 320f,
                    Enabled = SelectionAvailable,
                    Element = new NuiColumn
                    {
                        Children =
                        [
                            new NuiLabel("Position")
                            {
                                Height = 18f,
                                HorizontalAlign = NuiHAlign.Center
                            },
                            BuildVectorRow("X", PositionXString, PositionX, -100f, 100f, "pos_x_slider"),
                            BuildVectorRow("Y", PositionYString, PositionY, -100f, 100f, "pos_y_slider"),
                            BuildVectorRow("Z", PositionZString, PositionZ, -100f, 100f, "pos_z_slider"),
                            new NuiSpacer
                            {
                                Height = 6f
                            },
                            new NuiLabel("Visual Translation")
                            {
                                Height = 18f,
                                HorizontalAlign = NuiHAlign.Center
                            },
                            BuildVectorRow("X", TransformXString, TransformX, -10f, 10f, "trans_x_slider"),
                            BuildVectorRow("Y", TransformYString, TransformY, -10f, 10f, "trans_y_slider"),
                            BuildVectorRow("Z", TransformZString, TransformZ, -10f, 10f, "trans_z_slider"),
                            new NuiSpacer
                            {
                                Height = 6f
                            },
                            new NuiLabel("Visual Rotation")
                            {
                                Height = 18f,
                                HorizontalAlign = NuiHAlign.Center
                            },
                            BuildVectorRow("X", RotationXString, RotationX, -360f, 360f, "rot_x_slider"),
                            BuildVectorRow("Y", RotationYString, RotationY, -360f, 360f, "rot_y_slider"),
                            BuildVectorRow("Z", RotationZString, RotationZ, -360f, 360f, "rot_z_slider"),
                            new NuiSpacer
                            {
                                Height = 6f
                            },
                            new NuiRow
                            {
                                Height = 40f,
                                Children =
                                [
                                    new NuiLabel("Scale")
                                    {
                                        Width = 40f,
                                        VerticalAlign = NuiVAlign.Middle,
                                        HorizontalAlign = NuiHAlign.Center
                                    },
                                    new NuiTextEdit("0", ScaleString, 10, false)
                                    {
                                        Width = 80f,
                                        Enabled = SelectionAvailable
                                    },
                                    new NuiSliderFloat(Scale, 0f, 10f)
                                    {
                                        Width = 220f,
                                        Enabled = SelectionAvailable,
                                        Id = "scale_slider"
                                    }
                                ]
                            }
                        ]
                    }
                },
                new NuiRow
                {
                    Height = 42f,
                    Enabled = SelectionAvailable,
                    Children =
                    [
                        new NuiButton("Save Changes")
                        {
                            Id = "btn_save",
                            Height = 32f,
                            Width = 140f
                        }.Assign(out SaveButton),
                        new NuiButton("Discard Changes")
                        {
                            Id = "btn_discard",
                            Height = 32f,
                            Width = 140f
                        }.Assign(out DiscardButton)
                    ]
                },
                new NuiButton("Recover Selected")
                {
                    Id = "btn_recover",
                    Height = 32f,
                    Enabled = SelectionAvailable
                }.Assign(out RecoverButton),
                new NuiLabel(StatusMessage)
                {
                    Height = 18f,
                    ForegroundColor = ColorConstants.Orange
                }
            ]
        };
    }

    private static NuiRow BuildVectorRow(string label, NuiBind<string> stringBind, NuiBind<float> floatBind,
        float minimum, float maximum, string sliderId)
    {
        return new NuiRow
        {
            Height = 38f,
            Children =
            [
                new NuiLabel(label)
                {
                    Width = 24f,
                    VerticalAlign = NuiVAlign.Middle,
                    HorizontalAlign = NuiHAlign.Center
                },
                new NuiTextEdit("0", stringBind, 16, false)
                {
                    Width = 80f
                },
                new NuiSliderFloat(floatBind, minimum, maximum)
                {
                    Width = 220f,
                    Id = sliderId
                }
            ]
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
