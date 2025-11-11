using System.Text.Json;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Shops;

[TestFixture]
public class NpcShopProductTests
{
    [Test]
    public void LocalVariablesAndAppearanceAreProjectedFromRecord()
    {
        JsonLocalVariableDefinition[] locals =
        {
            new("category", JsonLocalVariableType.String, JsonDocument.Parse("\"Furniture\"").RootElement.Clone()),
            new("tier", JsonLocalVariableType.Int, JsonDocument.Parse("3").RootElement.Clone()),
        };

        ShopProductRecord productRecord = new()
        {
            Id = 10,
            ResRef = "furn_chair_oak",
            DisplayName = "Oak Chair",
            Description = "A sturdy oak chair finished in warm stain.",
            Price = 250,
            CurrentStock = 2,
            MaxStock = 6,
            RestockAmount = 1,
            SortOrder = 0,
            LocalVariablesJson = JsonSerializer.Serialize(locals),
            AppearanceJson = JsonSerializer.Serialize(new SimpleModelAppearanceDefinition(0, 12)),
        };

        NpcShop shop = CreateShop(productRecord);
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

        ShopProductRecord productRecord = new()
        {
            Id = 11,
            ResRef = "furn_table_round",
            DisplayName = "Round Table",
            Price = 500,
            CurrentStock = 1,
            MaxStock = 3,
            RestockAmount = 1,
            SortOrder = 0,
            LocalVariablesJson = JsonSerializer.Serialize(locals),
        };

        Assert.That(() => CreateShop(productRecord), Throws.ArgumentException);
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
            id: 20,
            resRef: "furn_shelf",
            displayName: "Oak Shelf",
            description: "A tall shelf made from dark oak.",
            price: 150,
            currentStock: 1,
            maxStock: 2,
            restockAmount: 1,
            isPlayerManaged: false,
            sortOrder: 0,
            baseItemType: null);

        Assert.That(product.TryConsume(1), Is.True);
        Assert.That(product.CurrentStock, Is.EqualTo(0));

        product.ReturnToStock(1);
        Assert.That(product.CurrentStock, Is.EqualTo(1));

        product.ReturnToStock(5);
        Assert.That(product.CurrentStock, Is.EqualTo(2));
    }

    private static NpcShop CreateShop(ShopProductRecord productRecord)
    {
        ShopRecord record = new()
        {
            Id = 1,
            Tag = "test_shop",
            DisplayName = "Test Shop",
            ShopkeeperTag = "test_keeper",
            RestockMinMinutes = 30,
            RestockMaxMinutes = 60,
            Products = new List<ShopProductRecord> { productRecord }
        };

        productRecord.ShopId = record.Id;
        productRecord.Shop = record;

        return new NpcShop(record);
    }
}
