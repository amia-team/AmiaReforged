using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.Nui;

public sealed class ShopWindowView : ScryView<ShopWindowPresenter>
{
    public readonly NuiBind<string> StoreTitle = new("shop_title");
    public readonly NuiBind<string> StoreDescription = new("shop_desc");
    public readonly NuiBind<int> ProductCount = new("shop_product_count");
    public readonly NuiBind<string> ProductEntries = new("shop_product_entries");
    public readonly NuiBind<int> InventoryCount = new("shop_inventory_count");
    public readonly NuiBind<string> InventoryEntries = new("shop_inventory_entries");

    public NuiButton CloseButton = null!;

    public ShopWindowView(NwPlayer player, NpcShop shop)
    {
        Presenter = new ShopWindowPresenter(this, player, shop);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override ShopWindowPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> productTemplate =
        [
            new(new NuiLabel(ProductEntries)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle
            })
        ];

        List<NuiListTemplateCell> inventoryTemplate =
        [
            new(new NuiLabel(InventoryEntries)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle
            })
        ];

        NuiColumn root = new()
        {
            Children =
            [
                new NuiLabel(StoreTitle)
                {
                    Height = 26f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiLabel(StoreDescription)
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
                    RowHeight = 26f,
                    Height = 200f
                },
                new NuiSpacer { Height = 6f },
                new NuiLabel("Items You May Sell")
                {
                    Height = 20f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiList(inventoryTemplate, InventoryCount)
                {
                    RowHeight = 24f,
                    Height = 160f
                },
                new NuiSpacer { Height = 8f },
                new NuiButton("Leave Counter")
                {
                    Id = "shop_close",
                    Height = 32f,
                    Width = 140f
                }.Assign(out CloseButton)
            ]
        };

        return root;
    }
}
