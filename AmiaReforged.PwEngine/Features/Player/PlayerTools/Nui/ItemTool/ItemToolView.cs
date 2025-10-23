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
        var iconRow = new NuiRow
        {
            Height = 36f,
            Children =
            [
                new NuiLabel(IconInfo)
                {
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiSpacer(), // flex so buttons don't overflow
                new NuiButton("+1")
                {
                    Id = "ind_icon_p1",
                    Height = 32f,
                    Enabled = IconControlsVisible
                }.Assign(out IconPlus1),
                new NuiButton("-1")
                {
                    Id = "ind_icon_m1",
                    Height = 32f,
                    Enabled = IconControlsVisible
                }.Assign(out IconMinus1),
                new NuiButton("+10")
                {
                    Id = "ind_icon_p10",
                    Height = 32f,
                    Enabled = IconControlsVisible
                }.Assign(out IconPlus10),
                new NuiButton("-10")
                {
                    Id = "ind_icon_m10",
                    Height = 32f,
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
                    Height = 35f,
                    Children =
                    [
                        new NuiButton("Select Item")
                        {
                            Id = "ind_select",
                            Height = 35f
                        }.Assign(out SelectItemButton),
                    ]
                },

                new NuiGroup
                {
                    Border = true,
                    Element = new NuiColumn
                    {
                        Children =
                        {
                            new NuiRow
                            {
                                Height = 36f,
                                Children =
                                [
                                    new NuiLabel("Name:")
                                    {
                                        Width = 80f,
                                        VerticalAlign = NuiVAlign.Middle
                                    },
                                    new NuiTextEdit("Item Name", Name, 100, false)
                                    {
                                        Enabled = ValidObjectSelected
                                    }
                                ]
                            },
                            new NuiLabel("Description:")
                            {
                                Height = 18f,
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

                // Icon controls (shown only for allowed base types)
                new NuiGroup
                {
                    Border = true,
                    Enabled = IconControlsVisible,
                    Element = new NuiColumn
                    {
                        Children =
                        {
                            new NuiLabel("Icon / Simple Model")
                            {
                                Height = 18f,
                                HorizontalAlign = NuiHAlign.Center
                            },
                            iconRow
                        }
                    }
                },

                new NuiSpacer { Height = 8f },

                new NuiRow
                {
                    Height = 36f,
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
