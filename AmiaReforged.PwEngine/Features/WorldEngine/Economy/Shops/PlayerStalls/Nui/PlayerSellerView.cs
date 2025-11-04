using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls.Nui;

public sealed class PlayerSellerView : ScryView<PlayerSellerPresenter>
{
    public readonly NuiBind<string> StallTitle = new("player_stall_seller_title");
    public readonly NuiBind<string> StallDescription = new("player_stall_seller_description");
    public readonly NuiBind<bool> StallDescriptionVisible = new("player_stall_seller_description_visible");
    public readonly NuiBind<string> StallNotice = new("player_stall_seller_notice");
    public readonly NuiBind<bool> StallNoticeVisible = new("player_stall_seller_notice_visible");
    public readonly NuiBind<string> SellerName = new("player_stall_seller_name");

    public readonly NuiBind<bool> RentToggleVisible = new("player_stall_seller_rent_toggle_visible");
    public readonly NuiBind<bool> RentToggleEnabled = new("player_stall_seller_rent_toggle_enabled");
    public readonly NuiBind<string> RentToggleLabel = new("player_stall_seller_rent_toggle_label");
    public readonly NuiBind<string> RentToggleStatus = new("player_stall_seller_rent_toggle_status");
    public readonly NuiBind<string> RentToggleTooltip = new("player_stall_seller_rent_toggle_tooltip");

    public readonly NuiBind<string> FeedbackText = new("player_stall_seller_feedback_text");
    public readonly NuiBind<bool> FeedbackVisible = new("player_stall_seller_feedback_visible");
    public readonly NuiBind<Color> FeedbackColor = new("player_stall_seller_feedback_color");

    public readonly NuiBind<int> ProductCount = new("player_stall_seller_product_count");
    public readonly NuiBind<string> ProductEntries = new("player_stall_seller_product_entries");
    public readonly NuiBind<string> ProductTooltips = new("player_stall_seller_product_tooltips");
    public readonly NuiBind<bool> ProductManageEnabled = new("player_stall_seller_product_manage_enabled");

    public readonly NuiBind<bool> DetailVisible = new("player_stall_seller_detail_visible");
    public readonly NuiBind<string> SelectedProductName = new("player_stall_seller_selected_name");
    public readonly NuiBind<string> SelectedProductQuantity = new("player_stall_seller_selected_quantity");
    public readonly NuiBind<string> SelectedProductStatus = new("player_stall_seller_selected_status");
    public readonly NuiBind<string> SelectedProductPrice = new("player_stall_seller_selected_price");
    public readonly NuiBind<string> SelectedProductDescription = new("player_stall_seller_selected_description");
    public readonly NuiBind<bool> SelectedProductDescriptionVisible = new("player_stall_seller_selected_description_visible");

    public readonly NuiBind<string> PriceInput = new("player_stall_seller_price_input");
    public readonly NuiBind<bool> PriceInputEnabled = new("player_stall_seller_price_input_enabled");
    public readonly NuiBind<bool> PriceSaveEnabled = new("player_stall_seller_price_save_enabled");

    public NuiButton ManageButton = null!;
    public NuiButton UpdatePriceButton = null!;
    public NuiButton RentToggleButton = null!;
    public NuiButton CloseButton = null!;

    public PlayerSellerView(NwPlayer player, PlayerStallSellerWindowConfig config)
    {
        Presenter = new PlayerSellerPresenter(this, player, config);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override PlayerSellerPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> productTemplate =
        [
            new(new NuiLabel(ProductEntries)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle,
                Tooltip = ProductTooltips
            })
            {
                Width = 360f
            },
            new(new NuiButton("Manage")
            {
                Id = "player_stall_manage",
                Height = 26f,
                Width = 90f,
                Enabled = ProductManageEnabled
            }.Assign(out ManageButton))
            {
                Width = 100f,
                VariableSize = false
            }
        ];

        NuiColumn detailColumn = new()
        {
            Children =
            [
                new NuiLabel(SelectedProductName)
                {
                    Height = 24f,
                    Width = 200f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiLabel(SelectedProductStatus)
                {
                    Height = 20f,
                    Width = 200f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiLabel(SelectedProductQuantity)
                {
                    Height = 20f,
                    Width = 200f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiLabel(SelectedProductPrice)
                {
                    Height = 20f,
                    Width = 200f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiSpacer { Height = 6f },
                new NuiLabel(SelectedProductDescription)
                {
                    Visible = SelectedProductDescriptionVisible,
                    Height = 60f,
                    Width = 200f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Top
                },
                new NuiRow
                {
                    Visible = RentToggleVisible,
                    Children =
                    [
                        new NuiLabel(RentToggleStatus)
                        {
                            Height = 22f,
                            Width = 320f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiSpacer { Width = 12f },
                        new NuiButton(RentToggleLabel)
                        {
                            Id = "player_stall_toggle_rent",
                            Height = 30f,
                            Width = 200f,
                            Enabled = RentToggleEnabled,
                            Tooltip = RentToggleTooltip
                        }.Assign(out RentToggleButton)
                    ]
                },
                new NuiSpacer { Height = 8f },
                new NuiRow
                {
                    Children =
                    [
                        new NuiLabel("New Price")
                        {
                            Width = 100f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiTextEdit(string.Empty, PriceInput, 9, false)
                        {
                            Width = 100f,
                            Enabled = PriceInputEnabled
                        }
                    ]
                },
                new NuiSpacer { Height = 8f },
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiButton("Save Price")
                        {
                            Id = "player_stall_save_price",
                            Height = 30f,
                            Width = 140f,
                            Enabled = PriceSaveEnabled
                        }.Assign(out UpdatePriceButton)
                    ]
                }
            ]
        };

        NuiColumn root = new()
        {
            Width = 600f,
            Children =
            [
                new NuiLabel(StallTitle)
                {
                    Height = 26f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiLabel(SellerName)
                {
                    Height = 22f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiLabel(StallDescription)
                {
                    Visible = StallDescriptionVisible,
                    Height = 24f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiLabel(StallNotice)
                {
                    Visible = StallNoticeVisible,
                    Height = 22f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = ColorConstants.Orange
                },
                new NuiSpacer { Height = 8f },
                new NuiRow
                {
                    Children =
                    [
                        new NuiColumn
                        {
                            Width = 360f,
                            Children =
                            [
                                new NuiLabel("Inventory")
                                {
                                    Height = 20f,
                                    HorizontalAlign = NuiHAlign.Left,
                                    VerticalAlign = NuiVAlign.Middle
                                },
                                new NuiList(productTemplate, ProductCount)
                                {
                                    Width = 340f,
                                    RowHeight = 26f,
                                    Height = 280f
                                }
                            ]
                        },
                        new NuiSpacer { Width = 12f },
                        new NuiGroup
                        {
                            Visible = DetailVisible,
                            Border = true,
                            Width = 220f,
                            Element = detailColumn
                        }
                    ]
                },
                new NuiSpacer { Height = 10f },
                new NuiLabel(FeedbackText)
                {
                    Visible = FeedbackVisible,
                    ForegroundColor = FeedbackColor,
                    Height = 22f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiButton("Close")
                        {
                            Id = "player_stall_seller_close",
                            Height = 32f,
                            Width = 140f
                        }.Assign(out CloseButton)
                    ]
                }
            ]
        };

        return root;
    }
}
