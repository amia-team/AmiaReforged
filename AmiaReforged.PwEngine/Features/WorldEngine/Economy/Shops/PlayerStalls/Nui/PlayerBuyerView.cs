using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls.Nui;

public sealed class PlayerBuyerView : ScryView<PlayerBuyerPresenter>
{
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
        List<NuiListTemplateCell> productTemplate =
        [
            new(new NuiLabel(ProductEntries)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle,
                Tooltip = ProductTooltips
            })
            {
                Width = 440f
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
            Width = 560f,
            Children =
            [
                new NuiLabel(StallTitle)
                {
                    Height = 28f,
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
                new NuiLabel(GoldText)
                {
                    Height = 22f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiSpacer { Height = 6f },
                new NuiLabel("Available Goods")
                {
                    Height = 20f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiList(productTemplate, ProductCount)
                {
                    Width = 540f,
                    RowHeight = 26f,
                    Height = 280f
                },
                new NuiSpacer { Height = 8f },
                new NuiLabel(FeedbackText)
                {
                    Visible = FeedbackVisible,
                    ForegroundColor = FeedbackColor,
                    Height = 22f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiSpacer { Height = 6f },
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer(),
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
