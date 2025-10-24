using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.ItemTool;

public sealed class ItemToolView : ScryView<ItemToolPresenter>, IToolWindow
{
    private const float WindowW = 600f;
    private const float WindowH = 535f;
    private const float HeaderW = 480f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 6f;

    public override ItemToolPresenter Presenter { get; protected set; }

    public readonly NuiBind<bool> ValidObjectSelected = new("ind_valid");
    public readonly NuiBind<string> Name = new("ind_name");
    public readonly NuiBind<string> Description = new("ind_desc");
    public readonly NuiBind<bool> IconControlsVisible = new("ind_icon_visible");
    public readonly NuiBind<string> IconInfo = new("ind_icon_info");

    public NuiButton SelectItemButton = null!;
    public NuiButton SaveButton = null!;
    public NuiButton DiscardButton = null!;
    public NuiButton IconPlus1 = null!;
    public NuiButton IconMinus1 = null!;
    public NuiButton IconPlus10 = null!;
    public NuiButton IconMinus10 = null!;

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

    private static NuiElement ImagePlatedButton(string id, string label, out NuiButton logicalButton, float width = 256f, float height = 64f, bool enabled = true)
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
                    new NuiRow { Children = { new NuiSpacer(), textButton, new NuiSpacer() } }
                }
            }
        };
    }

    public override NuiLayout RootLayout()
    {
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
                new NuiDrawListImage("ui_header", new NuiRect((WindowW - HeaderW) * 0.5f, HeaderTopPad, HeaderW, HeaderH))
            }
        };

        var headerSpacer = new NuiSpacer { Height = HeaderH + HeaderTopPad + 6f };
        var spacer6 = new NuiSpacer { Height = 6f };
        var spacer8 = new NuiSpacer { Height = 8f };

        var iconRow = new NuiRow
        {
            Children =
            {
                new NuiLabel(IconInfo){Width=250f,HorizontalAlign=NuiHAlign.Center,VerticalAlign=NuiVAlign.Middle},
                new NuiButton("+1"){Id="ind_icon_p1",Width=50f,Height=50f,Enabled=IconControlsVisible}.Assign(out IconPlus1),
                new NuiButton("-1"){Id="ind_icon_m1",Width=50f,Height=50f,Enabled=IconControlsVisible}.Assign(out IconMinus1),
                new NuiButton("+10"){Id="ind_icon_p10",Width=50f,Height=50f,Enabled=IconControlsVisible}.Assign(out IconPlus10),
                new NuiButton("-10"){Id="ind_icon_m10",Width=50f,Height=50f,Enabled=IconControlsVisible}.Assign(out IconMinus10)
            }
        };

        var selectRow = new NuiRow { Width = 500f, Children = { ImagePlatedButton("ind_select", "Select Item", out SelectItemButton) } };

        var basicProps = new NuiGroup
        {
            Width = 500f,
            Height = 250f,
            Border = true,
            Element = new NuiColumn
            {
                Children =
                {
                    new NuiRow
                    {
                        Children =
                        {
                            new NuiLabel("Name:"){Width=50f,Height=18f,VerticalAlign=NuiVAlign.Middle},
                            new NuiTextEdit("Item Name",Name,100,false){Enabled=ValidObjectSelected,Width=430f}
                        }
                    },
                    new NuiLabel("Description:"){Height=16f,VerticalAlign=NuiVAlign.Middle},
                    new NuiTextEdit("Item Description",Description,5000,true){Height=120f,Enabled=ValidObjectSelected}
                }
            }
        };

        var iconGroup = new NuiGroup
        {
            Width=500f,Height=100f,Border=true,
            Element=new NuiColumn
            {
                Children={ new NuiLabel("Icon / Simple Model"){Height=16f,HorizontalAlign=NuiHAlign.Center}, iconRow }
            }
        };

        var bottomRow = new NuiRow
        {
            Children =
            {
                ImagePlatedButton("ind_save","Save",out SaveButton),
                ImagePlatedButton("ind_discard","Discard",out DiscardButton)
            }
        };
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
                spacer6,
                basicProps,
                spacer8,
                iconGroup,
                spacer8,
                bottomRow
            }
        };
    }
}
