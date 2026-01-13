using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Nui;

public sealed class PlayerSellerView : ScryView<PlayerSellerPresenter>
{
    private const float WindowW = 850f;
    private const float WindowH = 770f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 0f;
    private const float HeaderLeftPad = 100f;
    private const float ContentWidth = WindowW - 40f;

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

    public readonly NuiBind<bool> HoldEarningsVisible = new("player_stall_seller_hold_earnings_visible");
    public readonly NuiBind<bool> HoldEarningsEnabled = new("player_stall_seller_hold_earnings_enabled");
    public readonly NuiBind<bool> HoldEarningsChecked = new("player_stall_seller_hold_earnings_checked");
    public readonly NuiBind<string> HoldEarningsLabel = new("player_stall_seller_hold_earnings_label");
    public readonly NuiBind<string> HoldEarningsTooltip = new("player_stall_seller_hold_earnings_tooltip");

    public readonly NuiBind<bool> EarningsRowVisible = new("player_stall_seller_earnings_visible");
    public readonly NuiBind<string> GrossProfitsText = new("player_stall_seller_gross_profits");
    public readonly NuiBind<string> AvailableFundsText = new("player_stall_seller_available_funds");
    public readonly NuiBind<string> EarningsBalanceText = new("player_stall_seller_earnings_balance");
    public readonly NuiBind<string> EarningsTooltip = new("player_stall_seller_earnings_tooltip");
    public readonly NuiBind<string> EarningsWithdrawInput = new("player_stall_seller_earnings_withdraw_input");
    public readonly NuiBind<bool> EarningsInputEnabled = new("player_stall_seller_earnings_input_enabled");
    public readonly NuiBind<bool> EarningsWithdrawEnabled = new("player_stall_seller_earnings_withdraw_enabled");
    public readonly NuiBind<bool> EarningsWithdrawAllEnabled = new("player_stall_seller_earnings_withdraw_all_enabled");

    public readonly NuiBind<string> DepositInput = new("player_stall_seller_deposit_input");
    public readonly NuiBind<bool> DepositEnabled = new("player_stall_seller_deposit_enabled");
    public readonly NuiBind<string> DepositTooltip = new("player_stall_seller_deposit_tooltip");

    public readonly NuiBind<int> LedgerCount = new("player_stall_seller_ledger_count");
    public readonly NuiBind<string> LedgerTimestampEntries = new("player_stall_seller_ledger_timestamps");
    public readonly NuiBind<string> LedgerAmountEntries = new("player_stall_seller_ledger_amounts");
    public readonly NuiBind<string> LedgerDescriptionEntries = new("player_stall_seller_ledger_descriptions");
    public readonly NuiBind<string> LedgerTooltipEntries = new("player_stall_seller_ledger_tooltips");

    public readonly NuiBind<string> FeedbackText = new("player_stall_seller_feedback_text");
    public readonly NuiBind<bool> FeedbackVisible = new("player_stall_seller_feedback_visible");
    public readonly NuiBind<Color> FeedbackColor = new("player_stall_seller_feedback_color");

    public readonly NuiBind<int> ProductCount = new("player_stall_seller_product_count");
    public readonly NuiBind<string> ProductEntries = new("player_stall_seller_product_entries");
    public readonly NuiBind<string> ProductTooltips = new("player_stall_seller_product_tooltips");
    public readonly NuiBind<bool> ProductManageEnabled = new("player_stall_seller_product_manage_enabled");
    public readonly NuiBind<bool> ProductEmptyVisible = new("player_stall_seller_product_empty_visible");

    public readonly NuiBind<bool> DetailVisible = new("player_stall_seller_detail_visible");
    public readonly NuiBind<bool> DetailPlaceholderVisible = new("player_stall_seller_detail_placeholder_visible");
    public readonly NuiBind<string> SelectedProductName = new("player_stall_seller_selected_name");
    public readonly NuiBind<string> SelectedProductQuantity = new("player_stall_seller_selected_quantity");
    public readonly NuiBind<string> SelectedProductStatus = new("player_stall_seller_selected_status");
    public readonly NuiBind<string> SelectedProductPrice = new("player_stall_seller_selected_price");
    public readonly NuiBind<string> SelectedProductDescription = new("player_stall_seller_selected_description");
    public readonly NuiBind<bool> SelectedProductDescriptionVisible = new("player_stall_seller_selected_description_visible");

    public readonly NuiBind<string> PriceInput = new("player_stall_seller_price_input");
    public readonly NuiBind<bool> PriceInputEnabled = new("player_stall_seller_price_input_enabled");
    public readonly NuiBind<bool> PriceSaveEnabled = new("player_stall_seller_price_save_enabled");
    public readonly NuiBind<bool> ProductRetrieveEnabled = new("player_stall_seller_reclaim_enabled");

    public readonly NuiBind<int> InventoryCount = new("player_stall_seller_inventory_count");
    public readonly NuiBind<string> InventoryEntries = new("player_stall_seller_inventory_entries");
    public readonly NuiBind<string> InventoryTooltips = new("player_stall_seller_inventory_tooltips");
    public readonly NuiBind<bool> InventorySelectEnabled = new("player_stall_seller_inventory_select_enabled");
    public readonly NuiBind<bool> InventoryEmptyVisible = new("player_stall_seller_inventory_empty_visible");

    public readonly NuiBind<bool> InventoryDetailVisible = new("player_stall_seller_inventory_detail_visible");
    public readonly NuiBind<bool> InventoryDetailPlaceholderVisible = new("player_stall_seller_inventory_detail_placeholder_visible");
    public readonly NuiBind<string> InventorySelectedName = new("player_stall_seller_inventory_selected_name");
    public readonly NuiBind<string> InventorySelectedResRef = new("player_stall_seller_inventory_selected_resref");
    public readonly NuiBind<string> InventorySelectedQuantity = new("player_stall_seller_inventory_selected_quantity");
    public readonly NuiBind<string> InventoryPriceInput = new("player_stall_seller_inventory_price_input");
    public readonly NuiBind<bool> InventoryPriceEnabled = new("player_stall_seller_inventory_price_enabled");
    public readonly NuiBind<bool> InventoryListEnabled = new("player_stall_seller_inventory_list_enabled");

    // Member management bindings
    public readonly NuiBind<int> MemberCount = new("player_stall_seller_member_count");
    public readonly NuiBind<string> MemberNames = new("player_stall_seller_member_names");
    public readonly NuiBind<string> MemberTooltips = new("player_stall_seller_member_tooltips");
    public readonly NuiBind<bool> MemberRemoveEnabled = new("player_stall_seller_member_remove_enabled");
    public readonly NuiBind<bool> MemberSectionVisible = new("player_stall_seller_member_section_visible");
    public readonly NuiBind<bool> TargetMemberVisible = new("player_stall_seller_target_member_visible");
    public readonly NuiBind<bool> TargetMemberEnabled = new("player_stall_seller_target_member_enabled");
    public readonly NuiBind<string> MemberStatusMessage = new("player_stall_seller_member_status_message");
    public readonly NuiBind<bool> MemberStatusVisible = new("player_stall_seller_member_status_visible");

    public readonly NuiBind<bool> CloseStallVisible = new("player_stall_seller_close_stall_visible");
    public readonly NuiBind<bool> CloseStallEnabled = new("player_stall_seller_close_stall_enabled");

    // Stall naming bindings
    public readonly NuiBind<string> StallNameInput = new("player_stall_seller_stall_name_input");
    public readonly NuiBind<bool> StallNameInputEnabled = new("player_stall_seller_stall_name_input_enabled");
    public readonly NuiBind<bool> StallNameSaveEnabled = new("player_stall_seller_stall_name_save_enabled");
    public readonly NuiBind<bool> StallNameRowVisible = new("player_stall_seller_stall_name_row_visible");

    public NuiButton ManageButton = null!;
    public NuiButton UpdatePriceButton = null!;
    public NuiButton RentToggleButton = null!;
    public NuiButton InventorySelectButton = null!;
    public NuiButton InventoryListButton = null!;
    public NuiButton RetrieveProductButton = null!;
    public NuiButtonImage CloseButton = null!;
    public NuiButton WithdrawProfitsButton = null!;
    public NuiButton WithdrawAllProfitsButton = null!;
    public NuiButton DepositButton = null!;
    public NuiButtonImage ViewDescriptionButton = null!;
    public NuiButton RemoveMemberButton = null!;
    public NuiButton TargetMemberButton = null!;
    public NuiButton CloseStallButton = null!;
    public NuiButton SaveStallNameButton = null!;

    public const string HoldEarningsToggleId = "player_stall_hold_earnings_toggle";

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
            new(new NuiButton(ProductEntries)
            {
                Id = "player_stall_manage",
                Height = 26f,
                Enabled = ProductManageEnabled,
                Tooltip = ProductTooltips
            }.Assign(out ManageButton))
            {
                Width = 360f,
                VariableSize = false
            }
        ];

        List<NuiListTemplateCell> inventoryTemplate =
        [
            new(new NuiLabel(InventoryEntries)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle,
                Tooltip = InventorySelectedName
            })
            {
                Width = 200f
            },
            new(new NuiButton("Select")
            {
                Id = "player_stall_inventory_select",
                Height = 26f,
                Width = 90f,
                Enabled = InventorySelectEnabled
            }.Assign(out InventorySelectButton))
            {
                Width = 100f,
                VariableSize = false
            }
        ];

        List<NuiListTemplateCell> ledgerTemplate =
        [
            new(new NuiLabel(LedgerTimestampEntries)
            {
                Height = 20f,
                Width = 120f,
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle,
                Tooltip = LedgerTooltipEntries
            }),
            new(new NuiLabel(LedgerAmountEntries)
            {
                Height = 20f,
                Width = 80f,
                HorizontalAlign = NuiHAlign.Right,
                VerticalAlign = NuiVAlign.Middle,
                Tooltip = LedgerTooltipEntries
            }),
            new(new NuiLabel(LedgerDescriptionEntries)
            {
                Height = 20f,
                Width = 280f,
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle,
                Tooltip = LedgerTooltipEntries
            })
        ];

        List<NuiListTemplateCell> memberTemplate =
        [
            new(new NuiLabel(MemberNames)
            {
                Height = 22f,
                Width = 200f,
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle,
                Tooltip = MemberTooltips
            }),
            new(new NuiButton("Remove")
            {
                Id = "player_stall_remove_member",
                Height = 22f,
                Width = 70f,
                Enabled = MemberRemoveEnabled,
                Tooltip = new NuiBind<string>("player_stall_remove_member_tooltip")
            }.Assign(out RemoveMemberButton))
            {
                Width = 80f,
                VariableSize = false
            }
        ];

        NuiColumn productDetailColumn = new()
        {
            Visible = DetailVisible,
            Children =
            [
                new NuiLabel(SelectedProductName)
                {
                    Height = 24f,
                    Width = 260f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiRow
                {
                    Children =
                    [
                        new NuiButtonImage("cc_scale")
                        {
                            Id = "player_stall_view_full_description",
                            Visible = SelectedProductDescriptionVisible,
                            Width = 30f,
                            Height = 30f,
                            Tooltip = "View Full Description"
                        }.Assign(out ViewDescriptionButton),
                        new NuiLabel("View Description")
                        {
                            Height = 30f,
                            Width = 120f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },
                new NuiLabel(SelectedProductStatus)
                {
                    Height = 20f,
                    Width = 260f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiLabel(SelectedProductQuantity)
                {
                    Height = 20f,
                    Width = 260f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiLabel(SelectedProductPrice)
                {
                    Height = 20f,
                    Width = 260f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiSpacer { Height = 6f },
                new NuiRow
                {
                    Children =
                    [
                        new NuiLabel("New Price:")
                        {
                            Width = 90f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiTextEdit(string.Empty, PriceInput, 9, false)
                        {
                            Width = 100f,
                            Height = 30f,
                            Enabled = PriceInputEnabled
                        },
                        new NuiButton("Save")
                        {
                            Id = "player_stall_save_price",
                            Height = 30f,
                            Width = 60f,
                            Enabled = PriceSaveEnabled
                        }.Assign(out UpdatePriceButton)
                    ]
                },
                new NuiSpacer { Height = 6f },
                new NuiRow
                {
                    Children =
                    [
                        new NuiButton("Take Item Back")
                        {
                            Id = "player_stall_reclaim_product",
                            Height = 30f,
                            Width = 140f,
                            Enabled = ProductRetrieveEnabled
                        }.Assign(out RetrieveProductButton)
                    ]
                }
            ]
        };

        NuiColumn inventoryDetailColumn = new()
        {
            Width = 300f,
            Children =
            [
                new NuiSpacer { Height = 4f },
                new NuiLabel("List Item")
                {
                    Height = 20f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiLabel("Select an inventory item.")
                {
                    Visible = InventoryDetailPlaceholderVisible,
                    Height = 20f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(139, 0, 0)
                },
                new NuiSpacer
                {
                    Height = 6f,
                    Visible = InventoryDetailVisible
                },
                new NuiLabel(InventorySelectedName)
                {
                    Visible = InventoryDetailVisible,
                    Height = 20f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiLabel(InventorySelectedResRef)
                {
                    Visible = InventoryDetailVisible,
                    Height = 18f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiLabel(InventorySelectedQuantity)
                {
                    Visible = InventoryDetailVisible,
                    Height = 18f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiLabel("Set Sale Price:")
                {
                    Width = 120f,
                    Height = 18f,
                    Visible = InventoryDetailVisible,
                    HorizontalAlign = NuiHAlign.Left,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiTextEdit(string.Empty, InventoryPriceInput, 9, false)
                {
                    Width = 120f,
                    Height = 30f,
                    Visible = InventoryDetailVisible,
                    Enabled = InventoryPriceEnabled
                },
                new NuiButton("List This Item")
                {
                        Id = "player_stall_list_inventory_item",
                        Height = 30f,
                        Width = 140f,
                        Visible = InventoryDetailVisible,
                        Enabled = InventoryListEnabled
                }.Assign(out InventoryListButton)
            ]
        };

        NuiColumn detailColumn = new()
        {
            Width = 300f,
            Children =
            [
                new NuiSpacer { Height = 4f },
                new NuiLabel("Edit Listing")
                {
                    Visible = DetailVisible,
                    Height = 20f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiLabel("Select a listing to edit it.")
                {
                    Visible = DetailPlaceholderVisible,
                    Height = 20f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(139, 0, 0)
                },
                productDetailColumn
            ]
        };

        NuiColumn root = new()
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            [
                new NuiRow { Width = 0f, Height = 0f, Children = new List<NuiElement>(), DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))] },
                new NuiRow { Width = 0f, Height = 0f, Children = new List<NuiElement>(), DrawList = [new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))] },
                new NuiSpacer { Height = 85f },

                new NuiRow
                {
                    Visible = StallNoticeVisible,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(StallNotice)
                        {
                            Width = ContentWidth - 20f,
                            Height = 22f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(139, 0, 0)
                        }
                    ]
                },
                new NuiSpacer { Height = 8f },
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiColumn
                        {
                            Width = 360f,
                            Children =
                            [
                                new NuiSpacer { Height = 4f },
                                new NuiLabel("Listings")
                                {
                                    Height = 20f,
                                    HorizontalAlign = NuiHAlign.Left,
                                    VerticalAlign = NuiVAlign.Middle,
                                    ForegroundColor = new Color(30, 20, 12)
                                },
                                new NuiSpacer { Height = 6f },
                                new NuiList(productTemplate, ProductCount)
                                {
                                    Width = 340f,
                                    RowHeight = 26f,
                                    Height = 250f
                                },
                                new NuiSpacer { Height = 3f },
                                new NuiLabel("You have no active listings.")
                                {
                                    Visible = ProductEmptyVisible,
                                    Height = 20f,
                                    HorizontalAlign = NuiHAlign.Left,
                                    VerticalAlign = NuiVAlign.Middle,
                                    ForegroundColor = new Color(139, 0, 0)
                                }
                            ]
                        },
                        new NuiSpacer { Width = 16f },
                        detailColumn
                    ]
                },
                new NuiSpacer { Height = 15f },
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiColumn
                        {
                            Width = 360f,
                            Children =
                            [
                                new NuiSpacer { Height = 4f },
                                new NuiLabel("Your Items")
                                {
                                    Height = 20f,
                                    HorizontalAlign = NuiHAlign.Left,
                                    VerticalAlign = NuiVAlign.Middle,
                                    ForegroundColor = new Color(30, 20, 12)
                                },
                                new NuiSpacer { Height = 6f },
                                new NuiList(inventoryTemplate, InventoryCount)
                                {
                                    Width = 340f,
                                    RowHeight = 26f,
                                    Height = 234f
                                },
                                new NuiSpacer { Height = 6f },
                                new NuiLabel("No eligible items in your inventory.")
                                {
                                    Visible = InventoryEmptyVisible,
                                    Height = 20f,
                                    HorizontalAlign = NuiHAlign.Left,
                                    VerticalAlign = NuiVAlign.Middle,
                                    ForegroundColor = new Color(139, 0, 0)
                                }
                            ]
                        },
                        new NuiSpacer { Width = 16f },
                        inventoryDetailColumn
                    ]
                },
                new NuiRow
                {
                    Visible = EarningsRowVisible,
                    Children =
                    [
                        new NuiSpacer{ Width = 20f },
                        new NuiLabel("Profits")
                        {
                            Height = 20f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },
                new NuiRow
                {
                    Visible = EarningsRowVisible,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(EarningsBalanceText)
                        {
                            Width = 170f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiSpacer { Width = 8f },
                        new NuiTextEdit(string.Empty, EarningsWithdrawInput, 9, false)
                        {
                            Width = 90f,
                            Height = 30f,
                            Enabled = EarningsInputEnabled,
                            Tooltip = EarningsTooltip
                        },
                        new NuiSpacer { Width = 6f },
                        new NuiButton("Withdraw")
                        {
                            Id = "player_stall_withdraw_profits",
                            Height = 30f,
                            Width = 110f,
                            Enabled = EarningsWithdrawEnabled,
                            Tooltip = EarningsTooltip
                        }.Assign(out WithdrawProfitsButton),
                        new NuiSpacer { Width = 6f },
                        new NuiButton("Withdraw All")
                        {
                            Id = "player_stall_withdraw_all_profits",
                            Height = 30f,
                            Width = 130f,
                            Enabled = EarningsWithdrawAllEnabled,
                            Tooltip = EarningsTooltip
                        }.Assign(out WithdrawAllProfitsButton),
                    ]
                },
                new NuiRow
                {
                    Visible = EarningsRowVisible,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("Gross Profits:")
                        {
                            Width = 170f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiSpacer { Width = 8f },
                        new NuiLabel(GrossProfitsText)
                        {
                            Width = 90f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },
                new NuiRow
                {
                    Visible = EarningsRowVisible,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("Available Funds:")
                        {
                            Width = 170f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiSpacer { Width = 8f },
                        new NuiLabel(AvailableFundsText)
                        {
                            Width = 90f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },
                new NuiRow
                {
                    Visible = EarningsRowVisible,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("Deposit to Escrow:")
                        {
                            Width = 170f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiSpacer { Width = 8f },
                        new NuiTextEdit(string.Empty, DepositInput, 9, false)
                        {
                            Width = 90f,
                            Height = 30f,
                            Enabled = DepositEnabled,
                            Tooltip = DepositTooltip
                        },
                        new NuiSpacer { Width = 6f },
                        new NuiButton("Deposit")
                        {
                            Id = "player_stall_deposit_rent",
                            Height = 30f,
                            Width = 110f,
                            Enabled = DepositEnabled,
                            Tooltip = DepositTooltip
                        }.Assign(out DepositButton)
                    ]
                },
                new NuiSpacer { Height = 6f, Visible = HoldEarningsVisible },
                new NuiRow
                {
                    Visible = HoldEarningsVisible,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiCheck(HoldEarningsLabel, HoldEarningsChecked)
                        {
                            Id = HoldEarningsToggleId,
                            Height = 20f,
                            Width = 320f,
                            ForegroundColor = new Color(139, 0, 0),
                            Tooltip = HoldEarningsTooltip,
                            Enabled = HoldEarningsEnabled
                        }
                    ]
                },
                new NuiSpacer { Height = 12f },
                new NuiRow
                {
                    Visible = RentToggleVisible,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(RentToggleStatus)
                        {
                            Height = 22f,
                            Width = 260f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            Tooltip = HoldEarningsTooltip,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiSpacer { Width = 8f },
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
                new NuiSpacer { Height = 12f },
                // Stall name input row (owner only)
                new NuiRow
                {
                    Visible = StallNameRowVisible,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("Stall Name:")
                        {
                            Height = 30f,
                            Width = 90f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiTextEdit("Enter stall name...", StallNameInput, 255, false)
                        {
                            Width = 280f,
                            Height = 30f,
                            Enabled = StallNameInputEnabled,
                            Tooltip = "Set a custom name for your stall (max 255 characters). Leave blank to use default."
                        },
                        new NuiSpacer { Width = 8f },
                        new NuiButton("Rename")
                        {
                            Id = "player_stall_save_stall_name",
                            Height = 30f,
                            Width = 90f,
                            Enabled = StallNameSaveEnabled,
                            Tooltip = "Save the custom stall name"
                        }.Assign(out SaveStallNameButton)
                    ]
                },
                new NuiSpacer { Height = 12f },
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiColumn
                        {
                            Width = ContentWidth - 20f,
                            Children =
                            [
                                new NuiSpacer { Height = 4f },
                                new NuiLabel("Recent Ledger Entries")
                                {
                                    Height = 20f,
                                    HorizontalAlign = NuiHAlign.Left,
                                    VerticalAlign = NuiVAlign.Middle,
                                    ForegroundColor = new Color(30, 20, 12)
                                },
                                new NuiSpacer { Height = 6f },
                                new NuiList(ledgerTemplate, LedgerCount)
                                {
                                    Width = ContentWidth - 60f,
                                    Height = 170f,
                                    RowHeight = 22f
                                }
                            ]
                        }
                    ]
                },
                // Member management section (only visible to owner)
                new NuiRow
                {
                    Visible = MemberSectionVisible,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiColumn
                        {
                            Width = ContentWidth - 20f,
                            Children =
                            [
                                new NuiSpacer { Height = 4f },
                                new NuiLabel("Stall Members")
                                {
                                    Height = 20f,
                                    HorizontalAlign = NuiHAlign.Left,
                                    VerticalAlign = NuiVAlign.Middle,
                                    ForegroundColor = new Color(30, 20, 12)
                                },
                                new NuiSpacer { Height = 6f },
                                new NuiList(memberTemplate, MemberCount)
                                {
                                    Width = ContentWidth - 60f,
                                    Height = 100f,
                                    RowHeight = 26f
                                },
                                new NuiSpacer { Height = 8f },
                                new NuiRow
                                {
                                    Visible = TargetMemberVisible,
                                    Height = 30f,
                                    Children =
                                    [
                                        new NuiButton("Target Player")
                                        {
                                            Id = "player_stall_target_member",
                                            Width = 120f,
                                            Height = 26f,
                                            Enabled = TargetMemberEnabled,
                                            Tooltip = "Click to target a player character in-game to add as a stall member."
                                        }.Assign(out TargetMemberButton),
                                        new NuiSpacer { Width = 12f },
                                        new NuiLabel(MemberStatusMessage)
                                        {
                                            Visible = MemberStatusVisible,
                                            Width = 300f,
                                            Height = 26f,
                                            HorizontalAlign = NuiHAlign.Left,
                                            VerticalAlign = NuiVAlign.Middle,
                                            ForegroundColor = new Color(30, 20, 12)
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                new NuiRow
                {
                    Visible = FeedbackVisible,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(FeedbackText)
                        {
                            Width = ContentWidth - 20f,
                            ForegroundColor = FeedbackColor,
                            Height = 15f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    ]
                },
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiButtonImage("ui_btn_cancel")
                        {
                            Id = "player_stall_seller_close",
                            Height = 38f,
                            Width = 150f
                        }.Assign(out CloseButton),
                        new NuiSpacer { Width = 20f },
                        new NuiButton("Close Stall & Retrieve All")
                        {
                            Id = "player_stall_close_and_retrieve_all",
                            Height = 38f,
                            Width = 220f,
                            Visible = CloseStallVisible,
                            Enabled = CloseStallEnabled,
                            Tooltip = "Close your stall and retrieve all items. Items that don't fit will be held by the Market Reeve."
                        }.Assign(out CloseStallButton)
                    ]
                }
            ]
        };

        return root;
    }
}
