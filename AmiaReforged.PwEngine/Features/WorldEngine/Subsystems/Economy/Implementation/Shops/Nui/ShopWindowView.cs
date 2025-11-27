using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.Nui;

public sealed class ShopWindowView : ScryView<ShopWindowPresenter>
{
    public const float WindowW = 630f;
    public const float WindowH = 640f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 0f;
    private const float HeaderLeftPad = 5f;

    public readonly NuiBind<string> StoreTitle = new("shop_title");
    public readonly NuiBind<string> StoreDescription = new("shop_desc");
    public readonly NuiBind<int> ProductCount = new("shop_product_count");
    public readonly NuiBind<string> ProductEntries = new("shop_product_entries");
    public readonly NuiBind<string> ProductTooltips = new("shop_product_tooltips");
    public readonly NuiBind<bool> ProductPurchasable = new("shop_product_purchasable");
    public readonly NuiBind<int> InventoryCount = new("shop_inventory_count");
    public readonly NuiBind<string> InventoryEntries = new("shop_inventory_entries");
    public readonly NuiBind<string> InventoryItemIds = new("shop_inventory_ids");
    public readonly NuiBind<bool> InventorySellable = new("shop_inventory_sellable");
    public readonly NuiBind<string> IdentifyButtonLabel = new("shop_identify_label");
    public readonly NuiBind<bool> IdentifyButtonEnabled = new("shop_identify_enabled");
    public readonly NuiBind<string> Search = new("shop_search");

    public NuiButtonImage ClearSearchButton = null!;
    public NuiButton CloseButton = null!;
    public NuiButton BuyButton = null!;
    public NuiButton SellButton = null!;
    public NuiButton IdentifyAllButton = null!;

    public ShopWindowView(NwPlayer player, NpcShop shop)
    {
        Presenter = new ShopWindowPresenter(this, player, shop);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override ShopWindowPresenter Presenter { get; protected set; }

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

        NuiSpacer headerSpacer = new NuiSpacer { Height = 85f };
        NuiSpacer spacer6 = new NuiSpacer { Height = 6f };
        NuiSpacer spacer8 = new NuiSpacer { Height = 8f };

        List<NuiListTemplateCell> productTemplate =
        [
            new(new NuiLabel(ProductEntries)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle,
                Tooltip = ProductTooltips
            })
            {
                Width = 350f
            },
            new(new NuiButton("Buy")
            {
                Id = "shop_buy",
                Enabled = ProductPurchasable,
                Height = 24f,
                Width = 90f
            }.Assign(out BuyButton))
            {
                Width = 100f,
                VariableSize = false
            }
        ];

        List<NuiListTemplateCell> inventoryTemplate =
        [
            new(new NuiLabel(InventoryEntries)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle
            })
            {
                Width = 350f
            },
            new(new NuiButton("Sell")
            {
                Id = "shop_sell",
                Enabled = InventorySellable,
                Height = 24f,
                Width = 90f
            }.Assign(out SellButton))
            {
                Width = 100f,
                VariableSize = false
            }
        ];

        const float listWidth = 500f;

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
                        new NuiLabel(StoreTitle)
                        {
                            Width = 500f,
                            Height = 26f,
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
                        new NuiLabel(StoreDescription)
                        {
                            Width = 500f,
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
                        new NuiTextEdit("Filter goods...", Search, 64, false)
                        {
                            Width = 440f,
                            Height = 26f
                        },
                        new NuiButtonImage("ir_abort")
                        {
                            Id = "shop_clear_search",
                            Aspect = 1f,
                            Width = 26f,
                            Height = 26f,
                            Tooltip = "Clear search"
                        }.Assign(out ClearSearchButton)
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
                            Width = 500f,
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
                            Width = listWidth,
                            RowHeight = 28f,
                            Height = 220f
                        }
                    ]
                },
                spacer6,
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiLabel("Items You May Sell")
                        {
                            Width = 500f,
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
                        new NuiList(inventoryTemplate, InventoryCount)
                        {
                            Width = listWidth,
                            RowHeight = 24f,
                            Height = 160f
                        }
                    ]
                },
                spacer6,
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiButton(IdentifyButtonLabel)
                        {
                            Id = "shop_identify_all",
                            Height = 30f,
                            Width = 160f,
                            Enabled = IdentifyButtonEnabled
                        }.Assign(out IdentifyAllButton)
                    ]
                },
                spacer8,
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer { Width = 20f },
                        new NuiButton("Leave Counter")
                        {
                            Id = "shop_close",
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
