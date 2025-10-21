using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.PlcEdit;

public sealed class PlcEditorView : ScryView<PlcEditorPresenter>, IDmWindow
{
    public override PlcEditorPresenter Presenter { get; protected set; }

    public readonly NuiBind<string> Name = new("name_val");
    public readonly NuiBind<string> Description = new("desc_val");

    public readonly NuiBind<string> PortraitResRef = new("port_ref");
    public readonly NuiBind<string> PortraitPreview = new("port_preview");

    public readonly NuiBind<bool> ValidObjectSelected = new("valid_obj");

    public readonly NuiBind<int> AppearanceValue = new("plc_appear");

    public readonly NuiBind<float> Scale = new("plc_scale");
    public readonly NuiBind<string> ScaleString = new("plc_scale_str");

    public readonly NuiBind<float> PositionStep = new NuiBind<float>("plc_pos_step");
    public readonly NuiBind<string> PositionStepString = new("plc_pos_step_str");

    public readonly NuiBind<float> PositionX = new("plc_pos_x");
    public readonly NuiBind<float> PositionY = new("plc_pos_y");
    public readonly NuiBind<float> PositionZ = new("plc_pos_z");
    public readonly NuiBind<string> PositionXString = new("plc_pos_x_str");
    public readonly NuiBind<string> PositionYString = new("plc_pos_y_str");
    public readonly NuiBind<string> PositionZString = new("plc_pos_z_str");

    public readonly NuiBind<float> RotationX = new("plc_rot_x");
    public readonly NuiBind<float> RotationY = new("plc_rot_y");
    public readonly NuiBind<float> RotationZ = new("plc_rot_z");
    public readonly NuiBind<string> RotationXString = new("plc_rot_x_str");
    public readonly NuiBind<string> RotationYString = new("plc_rot_y_str");
    public readonly NuiBind<string> RotationZString = new("plc_rot_z_str");

    public readonly NuiBind<float> TransformX = new("plc_trans_x");
    public readonly NuiBind<float> TransformY = new("plc_trans_y");
    public readonly NuiBind<float> TransformZ = new("plc_trans_z");
    public readonly NuiBind<string> TransformXString = new("plc_trans_x_str");
    public readonly NuiBind<string> TransformYString = new("plc_trans_y_str");
    public readonly NuiBind<string> TransformZString = new("plc_trans_z_str");

    public NuiButton SelectPlcButton = null!;

    public NuiButton Step1Button = null!;
    public NuiButton Step01Button = null!;
    public NuiButton Step001Button = null!;

    public NuiButton DecrementPositionXButton = null!;
    public NuiButton IncrementPositionXButton = null!;

    public NuiButton DecrementPositionYButton = null!;
    public NuiButton IncrementPositionYButton = null!;

    public NuiButton DecrementPositionZButton = null!;
    public NuiButton IncrementPositionZButton = null!;


    public PlcEditorView(NwPlayer player)
    {
        Presenter = new PlcEditorPresenter(this, player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        return new NuiColumn
        {
            Children =
            [
                new NuiButton("Select PLC (Nearest or Targeted")
                {
                    Id = "btn_plc_select"
                }.Assign(out SelectPlcButton),

                new NuiLabel("Basic Information")
                {
                    Height = 15f,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiRow
                {
                    Height = 300f,
                    Width = 600f,
                    Children =
                    [
                        new NuiGroup
                        {
                            Width = 300f,
                            Element = new NuiColumn
                            {
                                Children =
                                [
                                    new NuiLabel("Name:")
                                    {
                                        Height = 15f,
                                        Width = 60f,
                                        VerticalAlign = NuiVAlign.Middle
                                    },
                                    new NuiTextEdit("Enter a name...", Name, 200, false)
                                    {
                                        Width = 200f,
                                        Enabled = ValidObjectSelected
                                    },

                                    new NuiLabel("Portrait:")
                                    {
                                        Height = 15f,
                                        Width = 60f,
                                        VerticalAlign = NuiVAlign.Middle
                                    },
                                    new NuiTextEdit("PortraitResRef", PortraitResRef, 16, false)
                                    {
                                        Width = 200f,
                                        Enabled = ValidObjectSelected
                                    }
                                ]
                            }
                        },
                        new NuiGroup
                        {
                            Width = 300f,

                            Element = new NuiColumn
                            {
                                Children =
                                [
                                    new NuiLabel("Description:")
                                    {
                                        Height = 15f,
                                        VerticalAlign = NuiVAlign.Middle
                                    },

                                    new NuiTextEdit("Description . . .", Description, 5000, true)
                                    {
                                        Enabled = ValidObjectSelected,
                                        Height = 200
                                    }
                                ]
                            }
                        },
                    ]
                },

                new NuiLabel("Position")
                {
                    Height = 15f,
                    HorizontalAlign = NuiHAlign.Center,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiRow
                {
                    Height = 200f,
                    Children =
                    [
                        new NuiGroup
                        {
                            Element = new NuiColumn
                            {
                                Children =
                                [
                                    new NuiRow
                                    {
                                        Height = 60f,
                                        Children =
                                        [
                                            new NuiTextEdit("0", PositionXString, 10, false)
                                            {
                                                Tooltip = "X Value. Must be a valid decimal number",
                                                Enabled = ValidObjectSelected
                                            },
                                            new NuiSliderFloat(PositionX, -200, 200)
                                            {
                                                Width = 190f,
                                                Tooltip =
                                                    "The amount that a position/transform (X,Y,Z) will be decremented or incremented",
                                                Enabled = ValidObjectSelected,
                                                Id = "pos_x_slider"
                                            },
                                        ]
                                    },
                                    new NuiRow
                                    {
                                        Height = 60f,
                                        Children =
                                        [
                                            new NuiTextEdit("0", PositionYString, 10, false)
                                            {
                                                Tooltip = "Y Value. Must be a valid decimal number",
                                                Enabled = ValidObjectSelected
                                            },
                                            new NuiSliderFloat(PositionY, -200, 200)
                                            {
                                                Width = 190f,
                                                Tooltip =
                                                    "The amount that a position/transform (X,Y,Z) will be decremented or incremented",
                                                Enabled = ValidObjectSelected,
                                                Id = "pos_y_slider"
                                            },
                                        ]
                                    },

                                    new NuiRow
                                    {
                                        Height = 60f,
                                        Children =
                                        [
                                            new NuiTextEdit("0", PositionZString, 10, false)
                                            {
                                                Tooltip = "Z Value. Must be a valid decimal number",
                                                Enabled = ValidObjectSelected
                                            },
                                            new NuiSliderFloat(PositionZ, -200, 200)
                                            {
                                                Width = 190f,
                                                Tooltip =
                                                    "The amount that a position/transform (X,Y,Z) will be decremented or incremented",
                                                Enabled = ValidObjectSelected,
                                                Id = "pos_z_slider"
                                            },
                                        ]
                                    }
                                ]
                            }
                        }
                    ]
                },
                new NuiLabel("Transform")
                {
                    Height = 15f,
                    HorizontalAlign = NuiHAlign.Center,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiRow
                {
                    Height = 200f,
                    Children =
                    [
                        new NuiGroup
                        {
                            Element = new NuiColumn
                            {
                                Children =
                                [
                                    new NuiRow
                                    {
                                        Children =
                                        [
                                            new NuiTextEdit("0", TransformXString, 10, false)
                                            {
                                                Width = 70f,
                                                Enabled = ValidObjectSelected
                                            },
                                            new NuiSliderFloat(TransformX, -10, 10)
                                            {
                                                Width = 190f,
                                                Tooltip =
                                                    "The amount that a position/transform (X,Y,Z) will be decremented or incremented",
                                                Enabled = ValidObjectSelected,
                                                Id = "trans_x_slider"
                                            },
                                        ]
                                    },
                                    new NuiRow
                                    {
                                        Children =
                                        [
                                            new NuiTextEdit("0", TransformYString, 10, false)
                                            {
                                                Width = 70f,
                                                Enabled = ValidObjectSelected
                                            },
                                            new NuiSliderFloat(TransformY, -10, 10)
                                            {
                                                Width = 190f,
                                                Tooltip =
                                                    "The amount that a position/transform (X,Y,Z) will be decremented or incremented",
                                                Enabled = ValidObjectSelected,
                                                Id = "trans_y_slider"
                                            },
                                        ]
                                    },
                                    new NuiRow
                                    {
                                        Children =
                                        [
                                            new NuiTextEdit("0", TransformZString, 10, false)
                                            {
                                                Width = 70f,
                                                Enabled = ValidObjectSelected
                                            },
                                            new NuiSliderFloat(TransformZ, -10, 10)
                                            {
                                                Width = 190f,
                                                Tooltip =
                                                    "The amount that a position/transform (X,Y,Z) will be decremented or incremented",
                                                Enabled = ValidObjectSelected,
                                                Id = "trans_z_slider"
                                            },
                                        ]
                                    },
                                    new NuiLabel("Rotation")
                                    {
                                        Height = 15f,
                                        VerticalAlign = NuiVAlign.Middle
                                    },
                                    new NuiRow
                                    {
                                        Children =
                                        [
                                            new NuiTextEdit("0", RotationXString, 10, false)
                                            {
                                                Width = 70f,
                                                Enabled = ValidObjectSelected
                                            },
                                            new NuiSliderFloat(RotationX, -360, 360)
                                            {
                                                Width = 190f,
                                                Tooltip =
                                                    "The amount that a position/transform (X,Y,Z) will be decremented or incremented",
                                                Enabled = ValidObjectSelected,
                                                Id = "rot_x_slider"
                                            },
                                        ]
                                    },
                                    new NuiRow
                                    {
                                        Children =
                                        [
                                            new NuiTextEdit("0", RotationYString, 10, false)
                                            {
                                                Width = 70f,
                                                Enabled = ValidObjectSelected
                                            },
                                            new NuiSliderFloat(RotationY, -360, 360)
                                            {
                                                Width = 190f,
                                                Tooltip =
                                                    "The amount that a position/transform (X,Y,Z) will be decremented or incremented",
                                                Enabled = ValidObjectSelected,
                                                Id = "rot_y_slider"
                                            },
                                        ]
                                    },
                                    new NuiRow
                                    {
                                        Children =
                                        [
                                            new NuiTextEdit("0", RotationZString, 10, false)
                                            {
                                                Width = 70f,
                                                Enabled = ValidObjectSelected
                                            },
                                            new NuiSliderFloat(RotationZ, -360, 360)
                                            {
                                                Width = 190f,
                                                Tooltip =
                                                    "The amount that a position/transform (X,Y,Z) will be decremented or incremented",
                                                Enabled = ValidObjectSelected,
                                                Id = "rot_z_slider"
                                            },
                                        ]
                                    },
                                    new NuiLabel("Scale")
                                    {
                                        Height = 15f,
                                        VerticalAlign = NuiVAlign.Middle
                                    },
                                    new NuiRow
                                    {
                                        Children =
                                        [
                                            new NuiTextEdit("0", ScaleString, 10, false)
                                            {
                                                Width = 70f,
                                                Enabled = ValidObjectSelected
                                            },
                                            new NuiSliderFloat(Scale, 0, 100)
                                            {
                                                Width = 190f,
                                                Tooltip =
                                                    "The amount that a position/transform (X,Y,Z) will be decremented or incremented",
                                                Enabled = ValidObjectSelected,
                                                Id = "scale_slider"
                                            },
                                        ]
                                    },
                                ]
                            }
                        }
                    ]
                },
            ]
        };
    }

    public string Title => "PLC Editor";
    public bool ListInDmTools => true;

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;
}
