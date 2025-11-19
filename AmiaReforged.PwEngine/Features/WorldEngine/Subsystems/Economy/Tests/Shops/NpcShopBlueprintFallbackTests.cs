using System.Text.Json;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Shops;

public class NpcShopBlueprintFallbackTests
{
    [Test]
    public void ProductFallsBackToBlueprintFields_WhenOmittedInShopProduct()
    {
        InMemoryItemDefinitionRepository repo = new();
        ItemBlueprint blueprint = new(
            ResRef: "furniturespawn", // NWN physical template
            ItemTag: "blueprint_furniture_table", // Domain identifier
            Name: "Packed Table",
            Description: "A nice little table",
            Materials: new[] { MaterialEnum.Gem },
            JobSystemType: Harvesting.JobSystemItemType.ResourceGem,
            BaseItemType: 74,
            Appearance: new AppearanceData(0, 12, null),
            LocalVariables: new[]
            {
                new JsonLocalVariableDefinition("plc_name", JsonLocalVariableType.String, JsonDocument.Parse("\"Packed Table\"").RootElement.Clone()),
                new JsonLocalVariableDefinition("plc_appearance", JsonLocalVariableType.Int, JsonDocument.Parse("12").RootElement.Clone())
            });
        repo.AddItemDefinition(blueprint);

        ShopProductRecord productRecord = new()
        {
            Id = 10,
            ResRef = "blueprint_furniture_table", // References blueprint by ItemTag (domain ID)
            DisplayName = string.Empty, // force fallback
            Description = null,         // force fallback
            Price = 125,
            CurrentStock = 5,
            MaxStock = 20,
            RestockAmount = 3,
            SortOrder = 0,
            BaseItemType = null, // force fallback
            LocalVariablesJson = null,
            AppearanceJson = null
        };

        ShopRecord record = new()
        {
            Id = 77,
            Tag = "test_fallback_shop",
            DisplayName = "Fallback Test Shop",
            ShopkeeperTag = "keeper",
            RestockMinMinutes = 1,
            RestockMaxMinutes = 2,
            Products = new List<ShopProductRecord> { productRecord }
        };

        productRecord.ShopId = record.Id;
        productRecord.Shop = record;

        NpcShop shop = new(record, itemDefinitions: repo);
        NpcShopProduct product = shop.Products.Single();

        Assert.That(product.DisplayName, Is.EqualTo("Packed Table"));
        Assert.That(product.Description, Is.EqualTo("A nice little table"));
        Assert.That(product.BaseItemType, Is.EqualTo(74));
        Assert.That(product.Appearance?.SimpleModelNumber, Is.EqualTo(12));
        Assert.That(product.LocalVariables.Single(v => v.Name == "plc_name").StringValue, Is.EqualTo("Packed Table"));
        Assert.That(product.LocalVariables.Single(v => v.Name == "plc_appearance").IntValue, Is.EqualTo(12));
    }
}

