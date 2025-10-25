using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.ItemEditor;

public sealed class ItemEditorView : ScryView<ItemEditorPresenter>, IDmWindow
{
    private const float WindowW = 800f;
    private const float WindowH = 720f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 8f;
    private const float HeaderLeftPad = (WindowW - HeaderW) * 0.5f;

    public override ItemEditorPresenter Presenter { get; protected set; }

    // Core binds (unchanged)
    public readonly NuiBind<string> Name = new("item_name");
    public readonly NuiBind<string> Description = new("item_desc");
    public readonly NuiBind<string> Tag = new("item_tag");

    public readonly NuiBind<bool> ValidObjectSelected = new("item_valid");
    public readonly NuiBind<bool> IconControlsVisible = new("item_icon_visible");
    public readonly NuiBind<string> IconInfo = new("item_icon_info");
    public readonly NuiBind<string> DescPlaceholder = new("item_desc_placeholder");

    private const int VarRows = 8;

    public readonly NuiBind<bool>[] VarVisible =
        Enumerable.Range(0, VarRows).Select(i => new NuiBind<bool>($"var_vis_{i}")).ToArray();

    public readonly NuiBind<string>[] VarKey =
        Enumerable.Range(0, VarRows).Select(i => new NuiBind<string>($"var_key_{i}")).ToArray();

    public readonly NuiBind<string>[] VarType =
        Enumerable.Range(0, VarRows).Select(i => new NuiBind<string>($"var_type_{i}")).ToArray();

    public readonly NuiBind<string>[] VarValue =
        Enumerable.Range(0, VarRows).Select(i => new NuiBind<string>($"var_val_{i}")).ToArray();


    // Add-variable inputs (unchanged)
    public readonly NuiBind<string> VariableName = new("item_var_name");
    public readonly NuiBind<int> VariableType = new("item_var_type");
    public readonly NuiBind<string> VariableValue = new("item_var_value");

    // Manual variables column content (view drives by presenter data -> SetLayout)
    // We keep these for compatibility if your presenter still sets arrays; the view won't rely on them directly.
    public readonly NuiBind<int> VariableCount = new("var_row_count");
    public readonly NuiBind<string> VariableNames = new("var_key");
    public readonly NuiBind<string> VariableValues = new("var_value");
    public readonly NuiBind<string> VariableTypes = new("var_type");

    // Clickables (now NuiButtonImage, but we preserve the same IDs where applicable)
    public NuiButtonImage SelectItemButton = null!;
    public NuiButtonImage SaveButton = null!;
    public NuiButtonImage AddVariableButton = null!;
    public NuiButtonImage DeleteVariableButton = null!; // used only for id name; actual rows will carry btn_del_var_{i}

    public NuiButtonImage IconPlus1 = null!;
    public NuiButtonImage IconMinus1 = null!;
    public NuiButtonImage IconPlus10 = null!;
    public NuiButtonImage IconMinus10 = null!;

    // Edit modals (Name / Description)
    // We’ll open these as separate small windows from the presenter; these binds hold the working buffer.
    public readonly NuiBind<string> EditNameBuffer = new("edit_name_buf");
    public readonly NuiBind<string> EditDescBuffer = new("edit_desc_buf");

    // Presenter aliases (unchanged)
    public NuiBind<string> NewVariableName => VariableName;
    public NuiBind<string> NewVariableValue => VariableValue;
    public NuiBind<int> NewVariableType => VariableType;

    public string Title => "Item Editor";
    public bool ListInDmTools => true;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public ItemEditorView(NwPlayer player)
    {
        Presenter = new ItemEditorPresenter(this, player);
    }

    // ——————————————————————————————————————————
    // Helpers
    // ——————————————————————————————————————————

    private NuiElement Divider(float thickness = 1f, byte alpha = 48)
    {
        // Transparent row with a faint horizontal line
        return new NuiRow
        {
            Height = thickness + 4f, // a bit of breathing room
            DrawList = new()
            {
                // line across the full window width (tweak as desired)
                new NuiDrawListLine(new Color(0,0,0, alpha), false, thickness + 2f, new NuiVector(0.0f, 100.0f),
                    new NuiVector(0.0f, 400.0f))
            },
        };
    }


    private static NuiElement ImageButton(string id, string tooltip, out NuiButtonImage logicalButton,
        float width, float height, string plateResRef)
    {
        var btn = new NuiButtonImage(plateResRef)
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
        var btn = new NuiButtonImage(plateResRef)
        {
            Id = id,
            Width = width,
            Height = height,
            Tooltip = ""
        }.Assign(out logicalButton);

        // We show the text BELOW the button (transparent label), instead of draw-list overlay
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


    private NuiElement BuildIconRow()
    {
        return new NuiRow
        {
            Children =
            {
                new NuiLabel(IconInfo)
                    { Width = 260f, HorizontalAlign = NuiHAlign.Center, VerticalAlign = NuiVAlign.Middle, ForegroundColor = new Color(30, 20, 12) },
                ImageButton("btn_icon_p1", "+1", out IconPlus1, 35f, 35f, "ui_btn_sm_plus1"),
                ImageButton("btn_icon_m1", "-1", out IconMinus1, 35f, 35f, "ui_btn_sm_min1"),
                ImageButton("btn_icon_p10", "+10", out IconPlus10, 35f, 35f, "ui_btn_sm_plus10"),
                ImageButton("btn_icon_m10", "-10", out IconMinus10, 35f, 35f, "ui_btn_sm_min10"),
            }
        };
    }

    private NuiElement BuildHeaderOverlay()
    {
        return new NuiRow
        {
            Width = 0f, Height = 0f, Children = new List<NuiElement>(),
            DrawList = new()
            {
                new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))
            }
        };
    }

    public NuiElement BuildVariablesSection()
    {
        var rows = new List<NuiElement>(VarRows);
        for (int i = 0; i < VarRows; i++)
        {
            rows.Add(new NuiRow
            {
                Visible = VarVisible[i],
                Children =
                {
                    new NuiLabel(VarKey[i]) { Width = 240f, ForegroundColor = new Color(30, 20, 12) },
                    new NuiLabel(VarType[i]) { Width = 120f, ForegroundColor = new Color(30, 20, 12) },
                    new NuiLabel(VarValue[i]) { Width = 318f, ForegroundColor = new Color(30, 20, 12) },
                    new NuiButtonImage("ui_btn_sm_x")
                        { Id = $"btn_del_var_{i}", Width = 35f, Height = 35f, Tooltip = "Delete Variable" },
                }
            });
        }

        var addVarRow = BuildAddVariableRow();

        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel("Local Variables")
                            { Height = 20f, HorizontalAlign = NuiHAlign.Center, ForegroundColor = new Color(30, 20, 12) }
                    }
                },
                addVarRow,
                new NuiColumn { Children = rows },
                new NuiSpacer { Height = 4f }
            }
        };
    }

    private NuiElement BuildAddVariableRow()
    {
        var varTypeOptions = new List<NuiComboEntry>
        {
            new NuiComboEntry("Int", 0),
            new NuiComboEntry("Float", 1),
            new NuiComboEntry("String", 2),
            new NuiComboEntry("Location", 3),
            new NuiComboEntry("Object", 4),
        };

        return new NuiRow
        {
            Children =
            {
                new NuiTextEdit("Variable Name", VariableName, 64, false)
                    { Width = 220f, Enabled = ValidObjectSelected },
                new NuiCombo
                    { Entries = varTypeOptions, Selected = VariableType, Width = 140f, Enabled = ValidObjectSelected },
                new NuiTextEdit("Value", VariableValue, 1024, false)
                    { Width = 320, Enabled = ValidObjectSelected },
                ImageButton("btn_add_var", "Add", out AddVariableButton, 35f, 35f, "ui_btn_sm_plus")
            }
        };
    }

    private NuiElement BuildBasicProps()
    {
        const float labelW = 100f;
        const float valueW = 260f;

        var nameRow = new NuiRow
        {
            Children =
            {
                new NuiLabel("Name:") { Width = labelW, Height = 20f, VerticalAlign = NuiVAlign.Middle, ForegroundColor = new Color(30, 20, 12) },
                new NuiLabel(Name) { Width = valueW, Height = 20f, VerticalAlign = NuiVAlign.Middle, ForegroundColor = new Color(30, 20, 12) },
                ImageButton("btn_edit_name", "Edit Name", out _, 35f, 35f, "ui_btn_sm_edit")
            }
        };

        var descRow = new NuiRow
        {
            Children =
            {
                new NuiLabel("Description:") { Width = labelW, Height = 20f, VerticalAlign = NuiVAlign.Middle, ForegroundColor = new Color(30, 20, 12) },
                // #2 placeholder (wired below)
                new NuiLabel(DescPlaceholder) { Width = valueW, Height = 20f, VerticalAlign = NuiVAlign.Middle, ForegroundColor = new Color(30, 20, 12) },
                ImageButton("btn_edit_desc", "Edit Description", out _, 35f, 35f, "ui_btn_sm_edit")
            }
        };

        var tagRow = new NuiRow
        {
            Children =
            {
                new NuiLabel("Tag:") { Width = labelW, Height = 20f, VerticalAlign = NuiVAlign.Middle, ForegroundColor = new Color(30, 20, 12) },
                new NuiLabel(Tag) { Width = valueW, Height = 20f, VerticalAlign = NuiVAlign.Middle, ForegroundColor = new Color(30, 20, 12) },
                ImageButton("btn_edit_tag", "Edit Tag", out _, 35f, 35f, "ui_btn_sm_edit")
            }
        };

        return new NuiColumn { Children = { nameRow, descRow, tagRow } };
    }


    private NuiElement BuildIconGroup()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiLabel("Icon / Simple Model") { Height = 20f, HorizontalAlign = NuiHAlign.Center, ForegroundColor = new Color(30, 20, 12) },
                BuildIconRow()
            }
        };
    }

    public override NuiLayout RootLayout()
    {
        // Background parchment (draw-only)
        var bgLayer = new NuiRow
        {
            Width = 0f, Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = new() { new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH)) }
        };

        var headerOverlay = BuildHeaderOverlay();
        var headerSpacer = new NuiSpacer { Height = HeaderH + HeaderTopPad + 6f };

        var selectRow = new NuiRow
        {
            Children = { ImagePlatedLabeledButton("btn_select_item", "", out SelectItemButton, "ui_btn_item") }
        };

        var saveRow = new NuiRow
        {
            Children = { ImagePlatedLabeledButton("btn_save", "", out SaveButton, "ui_btn_save") }
        };
        SaveButton.Enabled = ValidObjectSelected;

        // Variables section starts empty; presenter will rebuild the layout with rows populated
        var variablesEmpty = BuildVariablesSection(); // no args now


        return new NuiColumn
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            {
                bgLayer,
                headerOverlay,
                headerSpacer,
                selectRow,
                new NuiSpacer { Height = 4f },
                Divider(),
                new NuiSpacer { Height = 4f },
                BuildBasicProps(),
                new NuiSpacer { Height = 4f },
                Divider(),
                new NuiSpacer { Height = 4f },
                BuildIconGroup(),
                new NuiSpacer { Height = 8f },
                variablesEmpty,
                new NuiSpacer { Height = 5f },
                Divider(),
                new NuiSpacer { Height = 5f },
                saveRow
            }
        };
    }

    // ——————————————————————————————
    // Small modal builders (presenter opens/handles)
    // ——————————————————————————————
    public NuiWindow BuildEditNameModal()
    {
        var layout = new NuiColumn
        {
            Width = 380f,
            Height = 180f,
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
                new NuiLabel("Edit Name") { Height = 18f, HorizontalAlign = NuiHAlign.Center, ForegroundColor = new Color(30, 20, 12)},
                new NuiTextEdit("Name", EditNameBuffer, 100, false) { Height = 32f },
                new NuiRow
                {
                    Children =
                    {
                        ImagePlatedLabeledButton("btn_modal_ok_name", "", out _, "ui_btn_save"),
                        new NuiSpacer { Width = 20f },
                        ImagePlatedLabeledButton("btn_modal_cancel_name", "", out _, "ui_btn_cancel"),
                    }
                }
            }
        };

        return new NuiWindow(layout, "Edit Name")
        {
            Geometry = new NuiRect(400f, 300f, 380f, 180f),
            Resizable = false
        };
    }

    public NuiWindow BuildEditDescModal()
    {
        var layout = new NuiColumn
        {
            Width = 380f, Height = 350f,
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
                new NuiLabel("Edit Description") { Height = 18f, HorizontalAlign = NuiHAlign.Center, ForegroundColor = new Color(30, 20, 12)},
                new NuiTextEdit("Description", EditDescBuffer, 5000, true) { Height = 160f },
                new NuiRow
                {
                    Children =
                    {
                        ImagePlatedLabeledButton("btn_modal_ok_desc", "", out _, "ui_btn_save"),
                        new NuiSpacer { Width = 20f },
                        ImagePlatedLabeledButton("btn_modal_cancel_desc", "", out _, "ui_btn_cancel"),
                    }
                }
            }
        };

        return new NuiWindow(layout, "Edit Description")
        {
            Geometry = new NuiRect(360f, 260f, 380f, 350f),
            Resizable = true
        };
    }

    public readonly NuiBind<string> EditTagBuffer = new("edit_tag_buf");

    public NuiWindow BuildEditTagModal()
    {
        var layout = new NuiColumn
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
                new NuiLabel("Edit Tag") { Height = 18f, HorizontalAlign = NuiHAlign.Center, ForegroundColor = new Color(30, 20, 12) },
                new NuiTextEdit("Tag", EditTagBuffer, 64, false) { Height = 32f },
                new NuiRow
                {
                    Children =
                    {
                        ImagePlatedLabeledButton("btn_modal_ok_tag", "", out _, "ui_btn_save"),
                        new NuiSpacer { Width = 20f },
                        ImagePlatedLabeledButton("btn_modal_cancel_tag", "", out _, "ui_btn_cancel"),
                    }
                }
            }
        };

        return new NuiWindow(layout, "Edit Tag")
        {
            Geometry = new NuiRect(420f, 320f, 380f, 180f),
            Resizable = false
        };
    }
}
