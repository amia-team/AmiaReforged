using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.JobSystemResourceModifier;

public sealed class JobSystemResourceModifierView : ScryView<JobSystemResourceModifierPresenter>, IDmWindow
{
    private const float WindowW = 645f;
    private const float WindowH = 675f;
    private const float HeaderW = 600f;
    private const float HeaderH = 80f;
    private const float HeaderTopPad = 4f;
    private const float HeaderLeftPad = 15f;

    public override JobSystemResourceModifierPresenter Presenter { get; protected set; }

    // Binds for resource list
    public readonly NuiBind<int> ResourceCount = new("resource_count");
    public readonly NuiBind<string> ResourceNames = new("resource_names");
    public readonly NuiBind<string> ResourceQuantities = new("resource_quantities");
    public readonly NuiBind<string> ResourceSources = new("resource_sources");
    public readonly NuiBind<string> ModifyQuantities = new("modify_quantities");

    // Binds for selected player
    public readonly NuiBind<string> SelectedPlayerName = new("selected_player_name");
    public readonly NuiBind<bool> PlayerSelected = new("player_selected");

    // Buttons
    public NuiButtonImage SelectPlayerButton = null!;
    public NuiButtonImage RefreshButton = null!;
    public NuiButtonImage CloseButton = null!;

    public string Title => "Job Resource Modifier (DM)";
    public bool ListInDmTools => true;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public JobSystemResourceModifierView(NwPlayer player)
    {
        Presenter = new JobSystemResourceModifierPresenter(this, player);
    }

    private static NuiElement ImageButton(string id, string tooltip, out NuiButtonImage logicalButton,
        float width, float height, string plateResRef)
    {
        NuiButtonImage btn = new NuiButtonImage(plateResRef)
        {
            Id = id,
            Width = width,
            Height = height,
            Tooltip = tooltip
        }.Assign(out logicalButton);

        return btn;
    }

    private static NuiElement ImagePlatedLabeledButton(string id, string label, out NuiButtonImage logicalButton,
        string plateResRef, string tooltip, float width = 150f, float height = 38f)
    {
        NuiButtonImage btn = new NuiButtonImage(plateResRef)
        {
            Id = id,
            Width = width,
            Height = height,
            Tooltip = tooltip
        }.Assign(out logicalButton);

        return new NuiColumn
        {
            Children =
            {
                btn,
                new NuiLabel(label)
                {
                    Height = 18f,
                    HorizontalAlign = NuiHAlign.Center
                }
            }
        };
    }

    private NuiElement Divider(float thickness = 1f, byte alpha = 48)
    {
        return new NuiRow
        {
            Height = thickness + 4f,
            DrawList = new()
            {
                new NuiDrawListLine(new Color(0,0,0, alpha), false, thickness + 2f,
                    new NuiVector(20.0f, thickness + 2f), new NuiVector(WindowW - 20f, thickness + 2f))
            },
        };
    }

    private NuiElement BuildResourceList()
    {
        // Build the list row template for each resource
        NuiListTemplateCell nameCell = new NuiListTemplateCell(new NuiLabel(ResourceNames)
        {
            Width = 200f,
        }) { Width = 200f };

        NuiListTemplateCell quantityCell = new NuiListTemplateCell(new NuiLabel(ResourceQuantities)
        {
            Width = 80f,
            HorizontalAlign = NuiHAlign.Center,
        }) { Width = 80f };

        NuiListTemplateCell sourceCell = new NuiListTemplateCell(new NuiLabel(ResourceSources)
        {
            Width = 150f,
            HorizontalAlign = NuiHAlign.Center,
        }) { Width = 150f };

        NuiListTemplateCell modifyQuantityCell = new NuiListTemplateCell(new NuiTextEdit("Qty", ModifyQuantities, 6, false)
        {
            Width = 60f,
            Enabled = PlayerSelected
        }) { Width = 60f };

        NuiListTemplateCell removeButtonCell = new NuiListTemplateCell(new NuiButtonImage("ui_btn_sm_min")
        {
            Id = "btn_remove",
            Width = 35f,
            Height = 35f,
            Tooltip = "Remove Quantity",
            Enabled = PlayerSelected
        }) { Width = 35f };

        NuiListTemplateCell addButtonCell = new NuiListTemplateCell(new NuiButtonImage("ui_btn_sm_plus")
        {
            Id = "btn_add",
            Width = 35f,
            Height = 35f,
            Tooltip = "Add Quantity",
            Enabled = PlayerSelected
        }) { Width = 35f };

        // Column headers
        NuiRow headerRow = new NuiRow
        {
            Height = 25f,
            Children =
            {
                new NuiLabel("Resource Name")
                {
                    Width = 200f,
                    HorizontalAlign = NuiHAlign.Left,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiLabel("Qty")
                {
                    Width = 80f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiLabel("Source")
                {
                    Width = 150f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiLabel("Modify")
                {
                    Width = 60f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiSpacer { Width = 40f },
                new NuiSpacer { Width = 40f }
            }
        };

        // Scrollable list
        NuiList resourceList = new NuiList(
            new[] { nameCell, quantityCell, sourceCell, modifyQuantityCell, removeButtonCell, addButtonCell },
            ResourceCount)
        {
            RowHeight = 35f,
            Width = 620f,
            Height = 250f,
            Scrollbars = NuiScrollbars.Y,
        };

        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel("Resources")
                        {
                            Height = 20f,
                            Width = 630f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 4f },
                headerRow,
                new NuiSpacer { Height = 4f },
                resourceList,
                new NuiSpacer { Height = 4f }
            }
        };
    }

    public override NuiLayout RootLayout()
    {
        // Background parchment (draw-only)
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f, Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = new() { new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH)) }
        };

        NuiRow headerOverlay = new NuiRow
        {
            Width = 0f, Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = new()
            {
                new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))
            }
        };

        NuiSpacer headerSpacer = new NuiSpacer { Height = HeaderH + HeaderTopPad + 6f };

        // Player selection row
        NuiRow playerSelectionRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 150f },
                ImagePlatedLabeledButton("btn_select_player", "", out SelectPlayerButton, "nui_pick", "Select Player",50f, 50f),
                new NuiLabel(SelectedPlayerName)
                {
                    Width = 400f,
                    Height = 35f,
                    VerticalAlign = NuiVAlign.Middle,
                    HorizontalAlign = NuiHAlign.Left,
                    ForegroundColor = new Color(30, 20, 12)
                }
            }
        };

        NuiRow controlRow = new NuiRow
        {
            Children =
            {
                ImagePlatedLabeledButton("btn_refresh", "", out RefreshButton, "ui_btn_save", "Save and Refresh"),
                new NuiSpacer { Width = 20f },
                ImagePlatedLabeledButton("btn_close", "", out CloseButton, "ui_btn_cancel", "Close")
            }
        };

        return new NuiColumn
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            {
                bgLayer,
                headerOverlay,
                headerSpacer,
                new NuiSpacer { Height = 10f },
                playerSelectionRow,
                BuildResourceList(),
                new NuiSpacer { Height = 10f },
                controlRow,
                new NuiSpacer { Height = 10f }
            }
        };
    }
}

