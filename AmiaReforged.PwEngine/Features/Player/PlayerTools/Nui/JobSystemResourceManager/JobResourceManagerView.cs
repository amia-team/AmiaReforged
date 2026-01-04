using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.JobSystemResourceManager;

public sealed class JobResourceManagerView : ScryView<JobResourceManagerPresenter>, IToolWindow
{
    private const float WindowW = 660f;
    private const float WindowH = 675f;
    private const float HeaderW = 600f;
    private const float HeaderH = 80f;
    private const float HeaderTopPad = 4f;
    private const float HeaderLeftPad = 20f;

    public override JobResourceManagerPresenter Presenter { get; protected set; }

    // Resource list binds
    public readonly NuiBind<int> ResourceCount = new("resource_count");
    public readonly NuiBind<string> ResourceNames = new("resource_name");
    public readonly NuiBind<string> ResourceQuantities = new("resource_quantity");
    public readonly NuiBind<string> ResourceSources = new("resource_source");
    public readonly NuiBind<string> TransferQuantities = new("transfer_quantity");

    // Control buttons
    public NuiButtonImage RefreshButton = null!;
    public NuiButtonImage CloseButton = null!;

    // IToolWindow implementation
    public string Id => "job_resource_manager";
    public string Title => "Job Resource Manager";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public string CategoryTag => "Jobs";

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public JobResourceManagerView(NwPlayer player)
    {
        Presenter = new JobResourceManagerPresenter(this, player);
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
        string plateResRef, float width = 150f, float height = 38f)
    {
        NuiButtonImage btn = new NuiButtonImage(plateResRef)
        {
            Id = id,
            Width = width,
            Height = height,
            Tooltip = label
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

    private NuiElement BuildHeaderOverlay()
    {
        return new NuiRow
        {
            Width = 0f, Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = new()
            {
                new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))
            }
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

        NuiListTemplateCell transferQuantityCell = new NuiListTemplateCell(new NuiTextEdit("Qty", TransferQuantities, 6, false)
        {
            Width = 60f
        }) { Width = 60f };

        NuiListTemplateCell transferButtonCell = new NuiListTemplateCell(new NuiButtonImage("nui_pick")
        {
            Id = "btn_transfer",
            Width = 35f,
            Height = 35f,
            Tooltip = "Transfer Resource"
        }) { Width = 40f };

        NuiListTemplateCell inventoryButtonCell = new NuiListTemplateCell(new NuiButtonImage("app_copy")
        {
            Id = "btn_to_inventory",
            Width = 35f,
            Height = 35f,
            Tooltip = "Move to Inventory"
        }) { Width = 40f };

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
                new NuiLabel("Transfer")
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
            new[] { nameCell, quantityCell, sourceCell, transferQuantityCell, transferButtonCell, inventoryButtonCell },
            ResourceCount)
        {
            RowHeight = 35f,
            Width = 630f,
            Height = 350f,
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
                        new NuiLabel("Resource Manager")
                        {
                            Height = 20f,
                            Width = 600f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 4f },
                headerRow,
                new NuiSpacer { Height = 4f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 1f },
                        resourceList
                    }
                },
                new NuiSpacer { Height = 4f }
            }
        };
    }

    public override NuiLayout RootLayout()
    {
        // Background parchment
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f, Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = new()
            {
                new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))
            }
        };

        NuiElement headerOverlay = BuildHeaderOverlay();
        NuiSpacer headerSpacer = new NuiSpacer { Height = HeaderH + HeaderTopPad + 6f };

        NuiRow controlRow = new NuiRow
        {
            Children =
            {
                ImagePlatedLabeledButton("btn_refresh", "", out RefreshButton, "ui_btn_save"),
                new NuiSpacer { Width = 20f },
                ImagePlatedLabeledButton("btn_close", "", out CloseButton, "ui_btn_cancel")
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
                BuildResourceList(),
                new NuiSpacer { Height = 10f },
                controlRow,
                new NuiSpacer { Height = 10f }
            }
        };
    }

    /// <summary>
    /// Builds the transfer destination selection modal
    /// </summary>
    public NuiWindow BuildTransferDestinationModal()
    {
        NuiColumn layout = new NuiColumn
        {
            Width = 400f,
            Height = 300f,
            Children =
            {
                new NuiRow
                {
                    Width = 0f, Height = 0f,
                    DrawList = new()
                    {
                        new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 400f, 300f))
                    }
                },
                new NuiLabel("Select Transfer Destination")
                {
                    Height = 25f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiSpacer { Height = 15f },
                new NuiRow
                {
                    Children =
                    {
                        ImagePlatedLabeledButton("btn_transfer_self_merchant", "My Merchant Box", out _, "ui_btn_save", 180f, 38f)
                    }
                },
                new NuiSpacer { Height = 8f },
                new NuiRow
                {
                    Children =
                    {
                        ImagePlatedLabeledButton("btn_transfer_other_merchant", "Another Player", out _, "ui_btn_player", 180f, 38f)
                    }
                },
                new NuiSpacer { Height = 8f },
                new NuiRow
                {
                    Children =
                    {
                        ImagePlatedLabeledButton("btn_transfer_miniature", "Miniature Box", out _, "ui_btn_box", 180f, 38f)
                    }
                },
                new NuiSpacer { Height = 15f },
                new NuiRow
                {
                    Children =
                    {
                        ImagePlatedLabeledButton("btn_transfer_cancel", "Cancel", out _, "ui_btn_cancel", 150f, 38f)
                    }
                }
            }
        };

        return new NuiWindow(layout, "Transfer Destination")
        {
            Geometry = new NuiRect(450f, 250f, 400f, 300f),
            Resizable = true
        };
    }
}

