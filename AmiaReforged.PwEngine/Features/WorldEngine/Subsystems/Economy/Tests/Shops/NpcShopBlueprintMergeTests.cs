using System.Text.Json;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using NUnit.Framework;
using ItemBlueprint = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData.ItemDefinition;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Shops;

public class NpcShopBlueprintMergeTests
{
    [Test]
    public void BlueprintLocalsApplied_WhenShopHasNoLocals()
    {
        InMemoryItemDefinitionRepository repo = new();
        ItemBlueprint blueprint = new(
            ResRef: "bp_resref_01",
            ItemTag: "bp_tag_01",
            Name: "Blueprint Item",
            Description: "Desc",
            Materials: new[] { MaterialEnum.Gem },
            JobSystemType: JobSystemItemType.ResourceGem,
            BaseItemType: 74,
            Appearance: new AppearanceData(0, 12, null),
            LocalVariables: new[]
            {
                new JsonLocalVariableDefinition("tier", JsonLocalVariableType.Int, JsonDocument.Parse("2").RootElement.Clone()),
                new JsonLocalVariableDefinition("category", JsonLocalVariableType.String, JsonDocument.Parse("\"Furniture\"").RootElement.Clone())
            });
        repo.AddItemDefinition(blueprint);

        ShopProductRecord productRecord = new()
        {
            Id = 1,
            ResRef = "bp_resref_01",
            DisplayName = "Display",
            Price = 10,
            CurrentStock = 1,
            MaxStock = 5,
            RestockAmount = 1,
            SortOrder = 0,
        };

        NpcShop shop = CreateShop(productRecord, repo);
        NpcShopProduct product = shop.Products.Single();

        Assert.That(product.LocalVariables.Single(v => v.Name == "tier").IntValue, Is.EqualTo(2));
        Assert.That(product.LocalVariables.Single(v => v.Name == "category").StringValue, Is.EqualTo("Furniture"));
    }

    [Test]
    public void ShopLocalsOverrideBlueprintLocals_ByName()
    {
        InMemoryItemDefinitionRepository repo = new();
        ItemBlueprint blueprint = new(
            ResRef: "bp_resref_02",
            ItemTag: "bp_tag_02",
            Name: "Blueprint Item",
            Description: "Desc",
            Materials: new[] { MaterialEnum.Gem },
            JobSystemType: JobSystemItemType.ResourceGem,
            BaseItemType: 74,
            Appearance: new AppearanceData(0, 12, null),
            LocalVariables: new[]
            {
                new JsonLocalVariableDefinition("tier", JsonLocalVariableType.Int, JsonDocument.Parse("1").RootElement.Clone()),
                new JsonLocalVariableDefinition("category", JsonLocalVariableType.String, JsonDocument.Parse("\"Furniture\"").RootElement.Clone())
            });
        repo.AddItemDefinition(blueprint);

        JsonLocalVariableDefinition[] shopLocals =
        {
            new("tier", JsonLocalVariableType.Int, JsonDocument.Parse("3").RootElement.Clone()), // override
            new("extra", JsonLocalVariableType.String, JsonDocument.Parse("\"Bonus\"").RootElement.Clone())
        };

        ShopProductRecord productRecord = new()
        {
            Id = 2,
            ResRef = "bp_resref_02",
            DisplayName = "Display",
            Price = 10,
            CurrentStock = 1,
            MaxStock = 5,
            RestockAmount = 1,
            SortOrder = 0,
            LocalVariablesJson = JsonSerializer.Serialize(shopLocals)
        };

        NpcShop shop = CreateShop(productRecord, repo);
        NpcShopProduct product = shop.Products.Single();

        Assert.That(product.LocalVariables.Single(v => v.Name == "tier").IntValue, Is.EqualTo(3));
        Assert.That(product.LocalVariables.Single(v => v.Name == "category").StringValue, Is.EqualTo("Furniture"));
        Assert.That(product.LocalVariables.Single(v => v.Name == "extra").StringValue, Is.EqualTo("Bonus"));
    }

    private static NpcShop CreateShop(ShopProductRecord productRecord, IItemDefinitionRepository repo)
    {
        ShopRecord record = new()
        {
            Id = 99,
            Tag = "bp_shop",
            DisplayName = "Blueprint Shop",
            ShopkeeperTag = "keeper",
            RestockMinMinutes = 10,
            RestockMaxMinutes = 20,
            Products = new List<ShopProductRecord> { productRecord }
        };

        productRecord.ShopId = record.Id;
        productRecord.Shop = record;

        return new NpcShop(record, itemDefinitions: repo);
    }
}
