using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Nui;

public sealed class PlayerBuyerView : ScryView<PlayerBuyerPresenter>
{
    private const float WindowW = 950f;
    private const float WindowH = 520f;
    private const float HeaderW = 900f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 0f;
    private const float HeaderLeftPad = 5f;

    public readonly NuiBind<string> StallTitle = new("player_stall_title");
    public readonly NuiBind<string> StallDescription = new("player_stall_description");
    public readonly NuiBind<bool> StallDescriptionVisible = new("player_stall_description_visible");
    public readonly NuiBind<string> StallNotice = new("player_stall_notice");
    public readonly NuiBind<bool> StallNoticeVisible = new("player_stall_notice_visible");
    public readonly NuiBind<string> GoldText = new("player_stall_gold_text");
    public readonly NuiBind<string> FeedbackText = new("player_stall_feedback_text");
    public readonly NuiBind<bool> FeedbackVisible = new("player_stall_feedback_visible");
    public readonly NuiBind<Color> FeedbackColor = new("player_stall_feedback_color");

    public readonly NuiBind<int> ProductCount = new("player_stall_product_count");
    public readonly NuiBind<string> ProductEntries = new("player_stall_product_entries");
    public readonly NuiBind<string> ProductTooltips = new("player_stall_product_tooltips");
    public readonly NuiBind<bool> ProductSelectable = new("player_stall_product_selectable");

    // Preview bindings
    public readonly NuiBind<bool> PreviewVisible = new("player_stall_preview_visible");
    public readonly NuiBind<bool> PreviewPlaceholderVisible = new("player_stall_preview_placeholder_visible");
    public readonly NuiBind<string> PreviewItemName = new("player_stall_preview_item_name");
    public readonly NuiBind<string> PreviewItemDescription = new("player_stall_preview_item_description");
    public readonly NuiBind<bool> PreviewDescriptionVisible = new("player_stall_preview_description_visible");
    public readonly NuiBind<bool> PreviewNoDescriptionVisible = new("player_stall_preview_no_description_visible");
    public readonly NuiBind<string> PreviewItemCost = new("player_stall_preview_item_cost");
    public readonly NuiBind<bool> PreviewBuyEnabled = new("player_stall_preview_buy_enabled");

    public NuiButton SelectButton = null!;
    public NuiButton BuyFromPreviewButton = null!;
    public NuiButton LeaveButton = null!;

    public PlayerBuyerView(NwPlayer player, PlayerStallBuyerWindowConfig config)
    {
        Presenter = new PlayerBuyerPresenter(this, player, config);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override PlayerBuyerPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f, Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))]
        };

        NuiRow headerOverlay = new NuiRow
        {
            Width = 0f, Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))]
        };

        NuiSpacer headerSpacer = new NuiSpacer { Height = 90f };
        NuiSpacer spacer6 = new NuiSpacer { Height = 6f };

        List<NuiListTemplateCell> productTemplate =
        [
            new(new NuiLabel(ProductEntries)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle,
                Tooltip = ProductTooltips
            })
            {
                Width = 280f
            },
            new(new NuiButton("Select")
            {
                Id = "player_stall_select",
                Height = 26f,
                Width = 70f,
                Enabled = ProductSelectable
            }.Assign(out SelectButton))
            {
                Width = 80f,
                VariableSize = false
            }
        ];

        NuiGroup previewGroup = new()
        {
            Id = "player_stall_preview_group",
            Border = true,
            Scrollbars = NuiScrollbars.Auto,
            Layout = new NuiColumn
            {
                Children =
                [
                    new NuiSpacer { Height = 8f },
                    new NuiLabel("Item Preview")
                    {
                        Height = 25f,
                        HorizontalAlign = NuiHAlign.Center,
                        VerticalAlign = NuiVAlign.Middle,
                        ForegroundColor = new Color(30, 20, 12)
                    },
                    new NuiSpacer { Height = 8f },
                    new NuiRow
                    {
                        Visible = PreviewPlaceholderVisible,
                        Children =
                        [
                            new NuiLabel("Select an item to view details")
                            {
                                Width = 260f,
                                Height = 200f,
                                HorizontalAlign = NuiHAlign.Center,
                                VerticalAlign = NuiVAlign.Middle,
                                ForegroundColor = new Color(80, 80, 80)
                            }
                        ]
                    },
                    new NuiColumn
                    {
                        Visible = PreviewVisible,
                        Children =
                        [
                            new NuiLabel(PreviewItemName)
                            {
                                Width = 260f,
                                Height = 28f,
                                HorizontalAlign = NuiHAlign.Left,
                                VerticalAlign = NuiVAlign.Middle,
                                ForegroundColor = new Color(30, 20, 12)
                            },
                            new NuiSpacer { Height = 6f },
                            new NuiLabel(PreviewItemCost)
                            {
                                Width = 260f,
                                Height = 22f,
                                HorizontalAlign = NuiHAlign.Left,
                                VerticalAlign = NuiVAlign.Middle,
                                ForegroundColor = new Color(30, 20, 12)
                            },
                            new NuiSpacer { Height = 12f },
                            new NuiLabel("Description:")
                            {
                                Width = 260f,
                                Height = 20f,
                                HorizontalAlign = NuiHAlign.Left,
                                VerticalAlign = NuiVAlign.Middle,
                                ForegroundColor = new Color(30, 20, 12),
                                Visible = PreviewDescriptionVisible
                            },
                            new NuiText(PreviewItemDescription)
                            {
                                Width = 260f,
                                Height = 100f,
                                Scrollbars = NuiScrollbars.Auto,
                                Visible = PreviewDescriptionVisible
                            },
                            new NuiLabel("No description available.")
                            {
                                Width = 260f,
                                Height = 40f,
                                HorizontalAlign = NuiHAlign.Left,
                                VerticalAlign = NuiVAlign.Top,
                                ForegroundColor = new Color(80, 80, 80),
                                Visible = PreviewNoDescriptionVisible
                            },
                            new NuiSpacer { Height = 12f },
                            new NuiButton("Purchase Item")
                            {
                                Id = "player_stall_buy_from_preview",
                                Width = 260f,
                                Height = 35f,
                                Enabled = PreviewBuyEnabled
                            }.Assign(out BuyFromPreviewButton),
                            new NuiSpacer { Height = 8f }
                        ]
                    }
                ]
            }
        };

        NuiColumn root = new()
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            [
                bgLayer,
                headerOverlay,
                headerSpacer,

                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(StallTitle)
                        {
                            Width = 400f,
                            Height = 28f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },
                new NuiRow
                {
                    Visible = StallDescriptionVisible,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(StallDescription)
                        {
                            Width = 400f,
                            Height = 24f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },
                new NuiRow
                {
                    Visible = StallNoticeVisible,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(StallNotice)
                        {
                            Width = 400f,
                            Height = 22f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = ColorConstants.Orange
                        }
                    ]
                },
                spacer6,
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(GoldText)
                        {
                            Width = 400f,
                            Height = 22f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },
                spacer6,
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("Available Goods")
                        {
                            Width = 400f,
                            Height = 20f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    ]
                },
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiColumn
                        {
                            Width = 380f,
                            Children =
                            [
                                new NuiList(productTemplate, ProductCount)
                                {
                                    RowHeight = 26f,
                                    Height = 280f
                                }
                            ]
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiColumn
                        {
                            Width = 280f,
                            Children =
                            [
                                previewGroup
                            ]
                        },
                        new NuiSpacer { Width = 20f }
                    ]
                },
                spacer6,
                new NuiRow
                {
                    Visible = FeedbackVisible,
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel(FeedbackText)
                        {
                            Width = 400f,
                            ForegroundColor = FeedbackColor,
                            Height = 22f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    ]
                },
                spacer6,
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiButton("Leave Stall")
                        {
                            Id = "player_stall_leave",
                            Height = 32f,
                            Width = 140f
                        }.Assign(out LeaveButton)
                    ]
                }
            ]
        };

        return root;
    }
}

