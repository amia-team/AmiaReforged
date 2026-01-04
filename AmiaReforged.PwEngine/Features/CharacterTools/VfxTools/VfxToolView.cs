using AmiaReforged.PwEngine.Features.DungeonMaster;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.CharacterTools.VfxTools;

public sealed class VfxToolView : ScryView<VfxToolPresenter>, IDmWindow
{
    private readonly bool _isDm;

    // Window sizing - compact for player, larger for DM
    public float WindowW => _isDm ? 385f : 380f;
    public float WindowH => _isDm ? 450f : 230f;

    public override VfxToolPresenter Presenter { get; protected set; } = null!;

    // IDmWindow implementation
    public string Title => "VFX Tool";
    public bool ListInDmTools => true;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    // General control binds
    public readonly NuiBind<bool> AlwaysEnabled = new("vfx_always_enabled");
    public readonly NuiBind<bool> IsDmBind = new("vfx_is_dm");

    // VFX selection binds
    public readonly NuiBind<string> VfxIdInput = new("vfx_id_input");
    public readonly NuiBind<string> CurrentVfxLabel = new("vfx_current_label");
    public readonly NuiBind<string> TargetNameLabel = new("vfx_target_name");

    // DM-specific binds
    public readonly NuiBind<string> PermanentVfxInput = new("vfx_permanent_input");
    public readonly NuiBind<string> DurationInput = new("vfx_duration_input");
    public readonly NuiBind<List<NuiComboEntry>> ActiveVfxList = new("vfx_active_list");
    public readonly NuiBind<int> SelectedVfxIndex = new("vfx_selected_index");

    // Button references
    public NuiButtonImage PreviousButton = null!;
    public NuiButtonImage NextButton = null!;
    public NuiButtonImage ApplyButton = null!;
    public NuiButton AddPermanentButton = null!;
    public NuiButton RemoveVfxButton = null!;
    public NuiButtonImage ChooseTargetButton = null!;
    public NuiButtonImage RefreshVfxListButton = null!;

    // Constructor for DM Tools - sets DM's controlled creature as initial target
    public VfxToolView(NwPlayer player)
    {
        _isDm = player.IsDM;
        NwGameObject? initialTarget = player.ControlledCreature;
        Presenter = new VfxToolPresenter(this, player, _isDm, initialTarget);
    }

    // Constructor for item activation or placeable use
    public VfxToolView(NwPlayer player, bool isDm, NwGameObject? selectedTarget)
    {
        _isDm = isDm;
        Presenter = new VfxToolPresenter(this, player, isDm, selectedTarget);
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

        return new NuiColumn
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            {
                bgLayer,
                new NuiSpacer { Height = 10f },
                BuildMainContent()
            }
        };
    }

    private NuiElement BuildMainContent()
    {
        NuiColumn mainColumn = new NuiColumn
        {
            Children =
            {
                // Title
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 125f },
                        new NuiLabel("VFX Tool")
                        {
                            Height = 25f,
                            Width = 100f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },

                // Target name
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 80f },
                        new NuiLabel(TargetNameLabel)
                        {
                            Height = 20f,
                            Width = 200f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(50, 40, 30)
                        }
                    }
                },
                new NuiSpacer { Height = 10f },

                // VFX browser with arrows and ID input
                BuildVfxBrowser(),
                new NuiSpacer { Height = 10f }
            }
        };

        // Add DM-specific controls
        if (_isDm)
        {
            mainColumn.Children.Add(BuildDmControls());
        }

        return mainColumn;
    }

    private NuiElement BuildVfxBrowser()
    {
        return new NuiColumn
        {
            Children =
            {
                // Current VFX label
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 30f },
                        new NuiLabel(CurrentVfxLabel)
                        {
                            Height = 25f,
                            Width = 300f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },

                // Navigation and input row
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 65f },
                        new NuiButtonImage("ui_btn_sm_min1")
                        {
                            Id = "btn_previous",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "Previous VFX"
                        }.Assign(out PreviousButton),
                        new NuiSpacer { Width = 5f },
                        new NuiTextEdit(label: "", value: VfxIdInput, maxLength: 5, multiLine: false)
                        {
                            Width = 100f,
                            Height = 35f,
                            Tooltip = "Enter VFX ID. Find the full list in the Requests forum."
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButtonImage("ui_btn_sm_check")
                        {
                            Id = "btn_apply",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "Apply VFX"
                        }.Assign(out ApplyButton),
                        new NuiSpacer { Width = 5f },
                        new NuiButtonImage("ui_btn_sm_plus1")
                        {
                            Id = "btn_next",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "Next VFX"
                        }.Assign(out NextButton)
                    }
                }
            }
        };
    }

    private NuiElement BuildDmControls()
    {
        return new NuiColumn
        {
            Margin = 10f,
            Children =
            {
                // Separator with chooser button
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 115f },
                        new NuiLabel("DM Controls")
                        {
                            Height = 35f,
                            Width = 100f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButtonImage("nui_pick")
                        {
                            Id = "btn_choose_target",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "Choose a new target"
                        }.Assign(out ChooseTargetButton)
                    }
                },

                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 25f },
                        // Active VFX list
                        new NuiLabel("Active VFX:")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },

                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 25f },
                        new NuiCombo
                        {
                            Height = 30f,
                            Width = 250f,
                            Entries = ActiveVfxList,
                            Selected = SelectedVfxIndex
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButtonImage("cc_turn_right")
                        {
                            Id = "btn_refresh_vfx",
                            Width = 30f,
                            Height = 30f,
                            Tooltip = "Refresh List"
                        }.Assign(out RefreshVfxListButton)
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 90f },
                        new NuiButton("Remove Selected")
                        {
                            Id = "btn_remove_vfx",
                            Height = 30f,
                            Tooltip = "Remove selected VFX"
                        }.Assign(out RemoveVfxButton)
                    }
                },

                new NuiSpacer { Height = 10f },

                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 55f },
                        // Add permanent VFX
                        new NuiLabel("Add VFX  Duration")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },

                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 55f },
                        new NuiTextEdit(label: "", value: PermanentVfxInput, maxLength: 5, multiLine: false)
                        {
                            Width = 70f,
                            Height = 30f,
                            Tooltip = "Enter VFX ID"
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiTextEdit(label: "", value: DurationInput, maxLength: 5, multiLine: false)
                        {
                            Width = 70f,
                            Height = 30f,
                            Tooltip = "Optional: Duration in seconds (leave empty for permanent)"
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiButton("Add")
                        {
                            Id = "btn_add_permanent",
                            Width = 65f,
                            Height = 30f,
                            Tooltip = "Add VFX (permanent if no duration, temporary if duration set)"
                        }.Assign(out AddPermanentButton)
                    }
                }
            }
        };
    }
}

