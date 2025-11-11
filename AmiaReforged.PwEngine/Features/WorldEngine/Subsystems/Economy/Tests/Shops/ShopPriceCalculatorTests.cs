using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Shops;

[TestFixture]
public class ShopPriceCalculatorTests
{
    [Test]
    public void CalculatePrice_WithNoModifiers_ReturnsBasePrice()
    {
        NpcShop shop = BuildShop(basePrice: 100, out NpcShopProduct product);
        ShopPriceCalculator calculator = new(Array.Empty<IShopPriceModifier>());

        int price = calculator.CalculatePrice(shop, product, buyer: null);

        Assert.That(price, Is.EqualTo(100));
    }

    [Test]
    public void CalculatePrice_AppliesModifiersInSequence()
    {
        NpcShop shop = BuildShop(basePrice: 200, out NpcShopProduct product);

        IShopPriceModifier[] modifiers =
        {
            new AddModifier(50),
            new MultiplyModifier(2),
            new AddModifier(-30)
        };

        ShopPriceCalculator calculator = new(modifiers);

        int price = calculator.CalculatePrice(shop, product, buyer: null);

    Assert.That(price, Is.EqualTo(470));
    }

    [Test]
    public void CalculatePrice_WhenModifierThrows_FallsBackToPreviousPrice()
    {
        NpcShop shop = BuildShop(basePrice: 150, out NpcShopProduct product);

        IShopPriceModifier[] modifiers =
        {
            new AddModifier(25),
            new ThrowingModifier(),
            new MultiplyModifier(3)
        };

        ShopPriceCalculator calculator = new(modifiers);

        int price = calculator.CalculatePrice(shop, product, buyer: null);

        Assert.That(price, Is.EqualTo(525));
    }

    [Test]
    public void CalculatePrice_ClampPreventsNegativeResults()
    {
        NpcShop shop = BuildShop(basePrice: 60, out NpcShopProduct product);

        IShopPriceModifier[] modifiers =
        {
            new AddModifier(-120),
            new AddModifier(-10)
        };

        ShopPriceCalculator calculator = new(modifiers);

        int price = calculator.CalculatePrice(shop, product, buyer: null);

        Assert.That(price, Is.EqualTo(0));
    }

    [Test]
    public void CalculatePrice_AppliesMarkupFromShop()
    {
        NpcShop shop = BuildShop(basePrice: 100, out NpcShopProduct product, markupPercent: 25);
        ShopMarkupPriceModifier markupModifier = new();
        ShopPriceCalculator calculator = new(new IShopPriceModifier[] { markupModifier });

        int price = calculator.CalculatePrice(shop, product, buyer: null);

        Assert.That(price, Is.EqualTo(125));
    }

    private static NpcShop BuildShop(int basePrice, out NpcShopProduct product, int markupPercent = 0)
    {
        ShopProductRecord productRecord = new()
        {
            Id = 100,
            ResRef = "test_item",
            DisplayName = "Test Item",
            Description = "A mock item for price calculation tests.",
            Price = basePrice,
            CurrentStock = 2,
            MaxStock = 5,
            RestockAmount = 1,
            SortOrder = 0
        };

        ShopRecord record = new()
        {
            Id = 50,
            Tag = "test_shop",
            DisplayName = "Test Shop",
            ShopkeeperTag = "test_keeper",
            RestockMinMinutes = 10,
            RestockMaxMinutes = 20,
            MarkupPercent = markupPercent,
            Products = new List<ShopProductRecord> { productRecord }
        };

        productRecord.ShopId = record.Id;
        productRecord.Shop = record;

        NpcShop shop = new(record);
        product = shop.Products.Single();
        return shop;
    }

    private sealed class AddModifier : IShopPriceModifier
    {
        private readonly int _delta;

        public AddModifier(int delta)
        {
            _delta = delta;
        }

        public int ModifyPrice(int currentPrice, ShopPriceContext context)
        {
            return currentPrice + _delta;
        }
    }

    private sealed class MultiplyModifier : IShopPriceModifier
    {
        private readonly int _factor;

        public MultiplyModifier(int factor)
        {
            _factor = factor;
        }

        public int ModifyPrice(int currentPrice, ShopPriceContext context)
        {
            return currentPrice * _factor;
        }
    }

    private sealed class ThrowingModifier : IShopPriceModifier
    {
        public int ModifyPrice(int currentPrice, ShopPriceContext context)
        {
            throw new InvalidOperationException("Test modifier failure");
        }
    }
}
