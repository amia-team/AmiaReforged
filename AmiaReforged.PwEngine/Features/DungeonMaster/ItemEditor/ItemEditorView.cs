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

    public override ItemEditorPresenter Presenter { get; protected set; }

    public readonly NuiBind<string> Name = new("item_name");
    public readonly NuiBind<string> Description = new("item_desc");
    public readonly NuiBind<string> Tag = new("item_tag");

    public readonly NuiBind<bool> ValidObjectSelected = new("item_valid");
    public readonly NuiBind<bool> IconControlsVisible = new("item_icon_visible");
    public readonly NuiBind<string> IconInfo = new("item_icon_info");

    public readonly NuiBind<string> VariableName = new("item_var_name");
    public readonly NuiBind<int> VariableType = new("item_var_type");
    public readonly NuiBind<string> VariableValue = new("item_var_value");

    public NuiButton SelectItemButton = null!;
    public NuiButton SaveButton = null!;
    public NuiButton AddVariableButton = null!;
    public NuiButton DeleteVariableButton = null!;

    public NuiButton IconPlus1 = null!;
    public NuiButton IconMinus1 = null!;
    public NuiButton IconPlus10 = null!;
    public NuiButton IconMinus10 = null!;

    // Presenter expects these aliases
    public NuiBind<string> NewVariableName => VariableName;
    public NuiBind<string> NewVariableValue => VariableValue;
    public NuiBind<int> NewVariableType => VariableType;

    public NuiBind<int> VariableCount { get; } = new("var_row_count");
    public NuiBind<string> VariableNames { get; } = new("var_key");
    public NuiBind<string> VariableValues { get; } = new("var_value");
    public NuiBind<string> VariableTypes { get; } = new("var_type");

    public string Title => "Item Editor";
    public bool ListInDmTools => true;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public ItemEditorView(NwPlayer player)
    {
        Presenter = new ItemEditorPresenter(this, player);
    }

    public override NuiLayout RootLayout()
    {
        // --- Draw-only layers ---
        var bgLayer = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = new()
            {
                new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))
            }
        };

        var headerOverlay = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = new()
            {
                new NuiDrawListImage(
                    "ui_header",
                    new NuiRect((WindowW - HeaderW) * 0.5f, HeaderTopPad, HeaderW, HeaderH))
            }
        };

        var headerSpacer = new NuiSpacer { Height = HeaderH + HeaderTopPad + 6f };
        var spacer8 = new NuiSpacer { Height = 8f };
        var spacer10 = new NuiSpacer { Height = 10f };

        // --- Image-plated button helper ---
        NuiElement ImagePlatedButton(string id, string label, out NuiButton logicalButton, float width = 256f, float height = 64f, bool enabled = true)
        {
            var textButton = new NuiButton(label)
            {
                Id = id,
                Height = 35f,
                Width = width - 56f,
                Enabled = enabled
            }.Assign(out logicalButton);

            return new NuiGroup
            {
                Width = width,
                Height = height,
                Border = false,
                Element = new NuiColumn
                {
                    DrawList = new() { new NuiDrawListImage("ui_button_round", new NuiRect(0f, 0f, width, height)) },
                    Children =
                    {
                        new NuiRow
                        {
                            Children = { new NuiSpacer(), textButton, new NuiSpacer() }
                        }
                    }
                }
            };
        }

        // --- Controls ---
        var selectRow = new NuiRow { Children = { ImagePlatedButton("btn_select_item", "Select Item", out SelectItemButton) } };

        var iconRow = new NuiRow
        {
            Children =
            {
                new NuiLabel(IconInfo){Width =260f,HorizontalAlign=NuiHAlign.Center,VerticalAlign=NuiVAlign.Middle},
                new NuiButton("+1"){Id="btn_icon_p1",Width=50f,Height=50f,Enabled=IconControlsVisible}.Assign(out IconPlus1),
                new NuiButton("-1"){Id="btn_icon_m1",Width=50f,Height=50f,Enabled=IconControlsVisible}.Assign(out IconMinus1),
                new NuiButton("+10"){Id="btn_icon_p10",Width=50f,Height=50f,Enabled=IconControlsVisible}.Assign(out IconPlus10),
                new NuiButton("-10"){Id="btn_icon_m10",Width=50f,Height=50f,Enabled=IconControlsVisible}.Assign(out IconMinus10)
            }
        };

        var basicProps = new NuiGroup
        {
            Width = 760f,
            Height = 220f,
            Border = true,
            Element = new NuiColumn
            {
                Children =
                {
                    new NuiLabel("Basic Properties"){Height=20f,HorizontalAlign=NuiHAlign.Center},
                    new NuiRow
                    {
                        Children =
                        {
                            new NuiLabel("Name:"){Width=60f,Height=20f,VerticalAlign=NuiVAlign.Middle},
                            new NuiTextEdit("Item Name", Name,100,false){Width=660f,Enabled=ValidObjectSelected}
                        }
                    },
                    new NuiLabel("Description:"){Height=20f},
                    new NuiTextEdit("Item Description",Description,5000,true){Height=120f,Enabled=ValidObjectSelected},
                    new NuiRow
                    {
                        Children =
                        {
                            new NuiLabel("Tag:"){Width=60f,Height=20f,VerticalAlign=NuiVAlign.Middle},
                            new NuiTextEdit("Item Tag",Tag,64,false){Width=660f,Enabled=ValidObjectSelected}
                        }
                    }
                }
            }
        };

        var varTypeOptions = new List<NuiComboEntry>
        {
            new NuiComboEntry("Int", 0),
            new NuiComboEntry("Float", 1),
            new NuiComboEntry("String", 2),
            new NuiComboEntry("Location", 3),
            new NuiComboEntry("Object", 4)
        };

        var variablesGroup = new NuiGroup
        {
            Width = 760f,
            Height = 340f,
            Border = true,
            Element = new NuiColumn
            {
                Children =
                {
                    new NuiLabel("Local Variables"){Height=20f,HorizontalAlign=NuiHAlign.Center},
                    new NuiRow
                    {
                        Children =
                        {
                            new NuiTextEdit("Variable Name",VariableName,64,false){Width=220f,Enabled=ValidObjectSelected},
                            new NuiCombo{Entries=varTypeOptions,Selected=VariableType,Width=140f,Enabled=ValidObjectSelected},
                            new NuiTextEdit("Value",VariableValue,1024,false){Width=320f,Enabled=ValidObjectSelected},
                            new NuiButton("Add"){Id="btn_add_var",Width=60f,Enabled=ValidObjectSelected}.Assign(out AddVariableButton)
                        }
                    },
                    new NuiList(
                        new List<NuiListTemplateCell>
                        {
                            new(new NuiLabel(VariableNames)){Width=240f},
                            new(new NuiLabel(VariableTypes)){Width=120f},
                            new(new NuiLabel(VariableValues)){Width=320f},
                            new(new NuiButton("Delete"){Id="btn_del_var"}.Assign(out DeleteVariableButton)){Width=70f}
                        },
                        VariableCount
                    ){RowHeight=28f,Height=250f}
                }
            }
        };

        var saveRow = new NuiRow { Children = { ImagePlatedButton("btn_save", "Save Changes", out SaveButton) } };
        SaveButton.Enabled = ValidObjectSelected;

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
                spacer8,
                basicProps,
                spacer8,
                new NuiGroup
                {
                    Width=760f,Height=100f,Border=true,
                    Element=new NuiColumn
                    {
                        Children={ new NuiLabel("Icon / Simple Model"){Height=20f,HorizontalAlign=NuiHAlign.Center}, iconRow }
                    }
                },
                spacer8,
                variablesGroup,
                spacer10,
                saveRow
            }
        };
    }
}
