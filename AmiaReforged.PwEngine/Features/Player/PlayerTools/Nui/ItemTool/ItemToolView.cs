using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.ItemTool;

public sealed class ItemToolView : ScryView<ItemToolPresenter>, IToolWindow
{
    // Binds
    public readonly NuiBind<bool>  ValidObjectSelected = new("ind_valid");
    public readonly NuiBind<string> Name        = new("ind_name");
    public readonly NuiBind<string> Description = new("ind_desc");
    public readonly NuiBind<bool>  IconControlsVisible = new("ind_icon_visible");
    public readonly NuiBind<string> IconInfo   = new("ind_icon_info");

    // Buttons
    public NuiButton SelectItemButton = null!;
    public NuiButton SaveButton       = null!;
    public NuiButton DiscardButton    = null!;
    public NuiButton IconPlus1        = null!;
    public NuiButton IconMinus1       = null!;
    public NuiButton IconPlus10       = null!;
    public NuiButton IconMinus10      = null!;

    public ItemToolView(NwPlayer player)
    {
        Presenter = new ItemToolPresenter(this, player);
        CategoryTag = "Items";
    }

    // IToolWindow
    public override ItemToolPresenter Presenter { get; protected set; }
    public string Id => "playertools.itemtool";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Item Modifier";
    public string CategoryTag { get; }

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public override NuiLayout RootLayout()
    {
        NuiRow iconRow = new NuiRow
        {
            Children =
            [
                new NuiLabel(IconInfo)
                {
                    Width = 120f,
                    HorizontalAlign = NuiHAlign.Center,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiButton("+1")
                {
                    Width = 50f,
                    Height = 50f,
                    Id = "ind_icon_p1",
                    Enabled = IconControlsVisible
                }.Assign(out IconPlus1),
                new NuiButton("-1")
                {
                    Width = 50f,
                    Height = 50f,
                    Id = "ind_icon_m1",
                    Enabled = IconControlsVisible
                }.Assign(out IconMinus1),
                new NuiButton("+10")
                {
                    Width = 50f,
                    Height = 50f,
                    Id = "ind_icon_p10",
                    Enabled = IconControlsVisible
                }.Assign(out IconPlus10),
                new NuiButton("-10")
                {
                    Width = 50f,
                    Height = 50f,
                    Id = "ind_icon_m10",
                    Enabled = IconControlsVisible
                }.Assign(out IconMinus10),
            ]
        };

        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Width = 400f,
                    Children =
                    [
                        new NuiButton("Select Item")
                        {
                            Id = "ind_select"
                        }.Assign(out SelectItemButton),
                    ]
                },

                new NuiGroup
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
                                [
                                    new NuiLabel("Name:")
                                    {
                                        Width = 40f,
                                        Height = 15f,
                                        VerticalAlign = NuiVAlign.Middle
                                    },
                                    new NuiTextEdit("Item Name", Name, 100, false)
                                    {
                                        Enabled = ValidObjectSelected,
                                        Width = 420f
                                    }
                                ]
                            },
                            new NuiLabel("Description:")
                            {
                                Height = 15f,
                                VerticalAlign = NuiVAlign.Middle
                            },
                            new NuiTextEdit("Item Description", Description, 5000, true)
                            {
                                Height = 120f,
                                Enabled = ValidObjectSelected
                            }
                        }
                    }
                },

                new NuiSpacer { Height = 6f },

                // Icon controls (always present; buttons disabled when not allowed)
                new NuiGroup
                {
                    Width = 500,
                    Height = 100,
                    Border = true,
                    Element = new NuiColumn
                    {
                        Children =
                        {
                            new NuiLabel("Icon / Simple Model")
                            {
                                Height = 15f,
                                HorizontalAlign = NuiHAlign.Center
                            },
                            iconRow
                        }
                    }
                },

                new NuiSpacer { Height = 8f },

                new NuiRow
                {
                    Children =
                    [
                        new NuiButton("Save")
                        {
                            Id = "ind_save",
                            Enabled = ValidObjectSelected,
                            Width  = 100f
                        }.Assign(out SaveButton),

                        new NuiButton("Discard")
                        {
                            Id = "ind_discard",
                            Width = 100f
                        }.Assign(out DiscardButton),
                    ]
                }
            }
        };
    }
}
