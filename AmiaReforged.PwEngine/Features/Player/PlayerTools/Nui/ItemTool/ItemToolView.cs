using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.ItemTool;

public sealed class ItemToolView : ScryView<ItemToolPresenter>, IToolWindow
{
    private const float WindowW = 650f;
    private const float WindowH = 455f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 6f;
    private const float HeaderLeftPad = 0f;

    public override ItemToolPresenter Presenter { get; protected set; }

    public readonly NuiBind<bool>   ValidObjectSelected = new("ind_valid");
    public readonly NuiBind<string> Name        = new("ind_name");
    public readonly NuiBind<string> Description = new("ind_desc");
    public readonly NuiBind<bool>   IconControlsVisible = new("ind_icon_visible");
    public readonly NuiBind<string> IconInfo            = new("ind_icon_info");
    public readonly NuiBind<string> DescPlaceholder = new("item_desc_placeholder");

    // Edit modals (same pattern as DM view)
    public readonly NuiBind<string> EditNameBuffer = new("edit_name_buf_ind");
    public readonly NuiBind<string> EditDescBuffer = new("edit_desc_buf_ind");

    public NuiButtonImage SelectItemButton = null!;
    public NuiButtonImage SaveButton       = null!;
    public NuiButtonImage CancelButton    = null!;
    public NuiButtonImage IconPlus1        = null!;
    public NuiButtonImage IconMinus1       = null!;
    public NuiButtonImage IconPlus10       = null!;
    public NuiButtonImage IconMinus10      = null!;

    // IToolWindow
    public string Title => "Item Tool";
    public string Id => "item_tool";
    public string CategoryTag => "Items";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public ItemToolView(NwPlayer player)
    {
        Presenter = new ItemToolPresenter(this, player);
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

    private static NuiElement ImagePlatedLabeledButton(string id, string label, out NuiButtonImage logicalButton, string plateResRef,
        float width = 150f, float height = 38f)
    {
        NuiButtonImage btn = new NuiButtonImage(plateResRef)
        {
            Id = id,
            Width = width,
            Height = height,
            Tooltip = ""
        }.Assign(out logicalButton);

        // Transparent label below the image button
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
        // LEFT-ALIGNED header image
        return new NuiRow
        {
            Width = 0f, Height = 0f, Children = new List<NuiElement>(),
            DrawList = new()
            {
                new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))
            }
        };
    }

    private NuiElement BuildIconRow()
    {
        return new NuiRow
        {
            Children =
            {
                new NuiLabel(IconInfo){ Width=250f, HorizontalAlign=NuiHAlign.Center, VerticalAlign=NuiVAlign.Middle, ForegroundColor = new Color(30, 20, 12) },
                ImageButton("ind_icon_p1",  "+1",  out IconPlus1,  35f, 35f, "ui_btn_sm_plus1"),
                ImageButton("ind_icon_m1",  "-1",  out IconMinus1, 35f, 35f, "ui_btn_sm_min1"),
                ImageButton("ind_icon_p10", "+10", out IconPlus10, 35f, 35f, "ui_btn_sm_plus10"),
                ImageButton("ind_icon_m10", "-10", out IconMinus10, 35f, 35f, "ui_btn_sm_min10"),
            }
        };
    }

    private NuiElement BuildBasicProps()
    {
        NuiRow nameRow = new NuiRow
        {
            Children =
            {
                new NuiLabel("Name:"){ Width=100f, Height=20f, VerticalAlign=NuiVAlign.Middle, ForegroundColor = new Color(30, 20, 12) },
                new NuiLabel(Name){ Width=260f, Height=20f, VerticalAlign=NuiVAlign.Middle, ForegroundColor = new Color(30, 20, 12) },
                ImageButton("ind_edit_name", "Edit Name", out _, 35f, 35f, "ui_btn_sm_edit")
            }
        };

        NuiRow descRow = new NuiRow
        {
            Children =
            {
                new NuiLabel("Description:"){ Width=100f, Height=20f, VerticalAlign=NuiVAlign.Middle, ForegroundColor = new Color(30, 20, 12) },
                new NuiLabel(DescPlaceholder) { Width = 260f, Height = 20f, VerticalAlign = NuiVAlign.Middle, ForegroundColor = new Color(30, 20, 12) },
                ImageButton("ind_edit_desc", "Edit Description", out _, 35f, 35f,"ui_btn_sm_edit"),
            }
        };

        // Column only; no Group ⇒ no panel background
        return new NuiColumn { Children = { nameRow, descRow } };
    }

    private NuiElement BuildIconGroup()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiLabel("Icon / Simple Model"){ Height=16f, HorizontalAlign=NuiHAlign.Center, ForegroundColor = new Color(30, 20, 12) },
                BuildIconRow()
            }
        };
    }

    public override NuiLayout RootLayout()
    {
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
        NuiSpacer spacer8 = new NuiSpacer { Height = 8f };

        NuiRow selectRow = new NuiRow
        {
            Width = 500f,
            Children = { ImagePlatedLabeledButton("ind_select", "", out SelectItemButton, "ui_btn_item") }
        };

        NuiRow bottomRow = new NuiRow
        {
            Children =
            {
                ImagePlatedLabeledButton("ind_save",    "",    out SaveButton, "ui_btn_save"),
                ImagePlatedLabeledButton("ind_cancel", "", out CancelButton, "ui_btn_cancel")
            }
        };
        SaveButton.Enabled = ValidObjectSelected;

        return new NuiColumn
        {
            Width = WindowW, Height = WindowH,
            Children =
            {
                bgLayer,
                headerOverlay,
                headerSpacer,
                selectRow,
                spacer8,
                BuildBasicProps(),
                spacer8,
                BuildIconGroup(),
                spacer8,
                bottomRow
            }
        };
    }

    // Optional: provide small modal layouts if you want to open them from the presenter
    public NuiWindow BuildEditNameModal()
    {
        NuiColumn layout = new NuiColumn
        {
            Width = 380f, Height = 180f,
            Children =
            {
                new NuiRow
                {
                    Width = 0f, Height = 0f,
                    DrawList = new()
                    {
                        new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 420f, 250f))
                    }
                },
                new NuiLabel("Edit Name"){ Height=18f, HorizontalAlign = NuiHAlign.Center, ForegroundColor = new Color(30, 20, 12) },
                new NuiTextEdit("Name", EditNameBuffer, 100, false) { Height = 32f },
                new NuiRow
                {
                    Children =
                    {
                        ImagePlatedLabeledButton("ind_modal_ok_name", "", out _ , "ui_btn_save"),
                        new NuiSpacer{Width=20f},
                        ImagePlatedLabeledButton("ind_modal_discard_name", "", out _ , "ui_btn_discard"),
                    }
                }
            }
        };

        return new NuiWindow(layout, "Edit Name")
        {
            Geometry = new NuiRect(420f, 320f, 380f, 180f),
            Resizable = false
        };
    }

    public NuiWindow BuildEditDescModal()
    {
        NuiColumn layout = new NuiColumn
        {
            Width = 380, Height = 350f,
            Children =
            {
                new NuiRow
                {
                    Width = 0f, Height = 0f,
                    DrawList = new()
                    {
                        new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 420f, 250f))
                    }
                },
                new NuiLabel("Edit Description"){ Height=18f, HorizontalAlign = NuiHAlign.Center, ForegroundColor = new Color(30, 20, 12) },
                new NuiTextEdit("Description", EditDescBuffer, 5000, true) { Height = 160f },
                new NuiRow
                {
                    Children =
                    {
                        ImagePlatedLabeledButton("ind_modal_ok_desc", "", out _ , "ui_btn_save"),
                        new NuiSpacer{Width=20f},
                        ImagePlatedLabeledButton("ind_modal_discard_desc", "", out _ , "ui_btn_discard"),
                    }
                }
            }
        };

        return new NuiWindow(layout, "Edit Description")
        {
            Geometry = new NuiRect(380f, 280f, 380f, 350f),
            Resizable = false
        };
    }
}
