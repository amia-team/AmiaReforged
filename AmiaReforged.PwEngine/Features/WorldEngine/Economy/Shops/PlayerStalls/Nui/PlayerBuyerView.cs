using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls.Nui;

public sealed class PlayerBuyerView : ScryView<PlayerBuyerPresenter>
{
    private const float WindowW = 630f;
    private const float WindowH = 520f;
    private const float HeaderW = 600f;
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
    public readonly NuiBind<bool> ProductPurchasable = new("player_stall_product_enabled");

    public NuiButton BuyButton = null!;
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
                Width = 400f
            },
            new(new NuiButton("Buy")
            {
                Id = "player_stall_buy",
                Height = 26f,
                Width = 90f,
                Enabled = ProductPurchasable
            }.Assign(out BuyButton))
            {
                Width = 100f,
                VariableSize = false
            }
        ];

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
                        new NuiList(productTemplate, ProductCount)
                        {
                            Width = 400f,
                            RowHeight = 26f,
                            Height = 280f
                        }
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
