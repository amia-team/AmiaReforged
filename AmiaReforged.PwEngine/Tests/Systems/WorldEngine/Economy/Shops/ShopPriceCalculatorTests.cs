using System;
using System.Collections.Generic;
using System.Linq;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Shops;

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

    private static NpcShop BuildShop(int basePrice, out NpcShopProduct product)
    {
        NpcShopProductDefinition productDefinition = new(
            "test_item",
            basePrice,
            2,
            5,
            1,
            null,
            null);

        NpcShopDefinition shopDefinition = new(
            "test_shop",
            "Test Shop",
            "test_keeper",
            null,
            new NpcShopRestockDefinition(10, 20),
            new[] { productDefinition });

        NpcShop shop = new(shopDefinition);
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
