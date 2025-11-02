using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Shops;

[TestFixture]
public class NpcShopProductTests
{
    [Test]
    public void LocalVariablesAndAppearanceAreProjectedFromDefinition()
    {
        JsonLocalVariableDefinition[] locals =
        {
            new("category", JsonLocalVariableType.String, JsonDocument.Parse("\"Furniture\"").RootElement.Clone()),
            new("tier", JsonLocalVariableType.Int, JsonDocument.Parse("3").RootElement.Clone()),
        };

        NpcShopProductDefinition productDefinition = new(
            "furn_chair_oak",
            250,
            2,
            6,
            1,
            locals,
            new SimpleModelAppearanceDefinition(0, 12));

        NpcShopDefinition shopDefinition = new(
            "test_shop",
            "Test Shop",
            "test_keeper",
            null,
            new NpcShopRestockDefinition(30, 60),
            new[] { productDefinition });

        NpcShop shop = new(shopDefinition);
        NpcShopProduct product = shop.Products.Single();

        Assert.Multiple(() =>
        {
            Assert.That(product.LocalVariables, Has.Count.EqualTo(2));
            Assert.That(product.LocalVariables.Single(v => v.Name == "category").StringValue, Is.EqualTo("Furniture"));
            Assert.That(product.LocalVariables.Single(v => v.Name == "tier").IntValue, Is.EqualTo(3));
            Assert.That(product.Appearance, Is.Not.Null);
            Assert.That(product.Appearance!.SimpleModelNumber, Is.EqualTo(12));
        });
    }

    [Test]
    public void InvalidLocalVariableThrows()
    {
        JsonLocalVariableDefinition[] locals =
        {
            new("tier", JsonLocalVariableType.Int, JsonDocument.Parse("\"not-a-number\"").RootElement.Clone()),
        };

        NpcShopProductDefinition productDefinition = new(
            "furn_table_round",
            500,
            1,
            3,
            1,
            locals,
            null);

        NpcShopDefinition shopDefinition = new(
            "test_shop_invalid",
            "Test Shop",
            "test_keeper",
            null,
            new NpcShopRestockDefinition(30, 60),
            new[] { productDefinition });

        Assert.That(() => new NpcShop(shopDefinition), Throws.ArgumentException);
    }

    [Test]
    public void LocalVariableWritesToWriterUsingDeclaredType()
    {
        JsonLocalVariableDefinition stringDef = new(
            "category",
            JsonLocalVariableType.String,
            JsonDocument.Parse("\"Furniture\"").RootElement.Clone());

        JsonLocalVariableDefinition jsonDef = new(
            "metadata",
            JsonLocalVariableType.Json,
            JsonDocument.Parse("{\"Color\":\"Oak\"}").RootElement.Clone());

        JsonLocalVariableDefinition intDef = new(
            "tier",
            JsonLocalVariableType.Int,
            JsonDocument.Parse("2").RootElement.Clone());

        NpcShopLocalVariable stringVar = NpcShopLocalVariable.FromDefinition(stringDef);
        NpcShopLocalVariable jsonVar = NpcShopLocalVariable.FromDefinition(jsonDef);
        NpcShopLocalVariable intVar = NpcShopLocalVariable.FromDefinition(intDef);

        FakeLocalWriter writer = new();

        stringVar.WriteTo(writer);
        jsonVar.WriteTo(writer);
        intVar.WriteTo(writer);

        Assert.Multiple(() =>
        {
            Assert.That(writer.Strings, Does.ContainKey("category"));
            Assert.That(writer.Strings["category"], Is.EqualTo("Furniture"));
            Assert.That(writer.Jsons, Does.ContainKey("metadata"));
            Assert.That(writer.Jsons["metadata"], Is.EqualTo("{\"Color\":\"Oak\"}"));
            Assert.That(writer.Ints, Does.ContainKey("tier"));
            Assert.That(writer.Ints["tier"], Is.EqualTo(2));
        });
    }

    private sealed class FakeLocalWriter : IItemLocalVariableWriter
    {
        public Dictionary<string, int> Ints { get; } = new();
        public Dictionary<string, string> Strings { get; } = new();
        public Dictionary<string, string> Jsons { get; } = new();

        public void SetInt(string name, int value)
        {
            Ints[name] = value;
        }

        public void SetString(string name, string value)
        {
            Strings[name] = value;
        }

        public void SetJson(string name, string json)
        {
            Jsons[name] = json;
        }
    }

    [Test]
    public void ReturnToStockRestoresQuantityWithoutExceedingMax()
    {
        NpcShopProduct product = new(
            "furn_shelf",
            150,
            initialStock: 1,
            maxStock: 2,
            restockAmount: 1);

        Assert.That(product.TryConsume(1), Is.True);
        Assert.That(product.CurrentStock, Is.EqualTo(0));

        product.ReturnToStock(1);
        Assert.That(product.CurrentStock, Is.EqualTo(1));

        product.ReturnToStock(5);
        Assert.That(product.CurrentStock, Is.EqualTo(2));
    }
}
