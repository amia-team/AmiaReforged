using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Tests;

/// <summary>
/// Tests for optimized POI query operations - O(1) direct lookups vs O(M) scans.
/// Validates the new index-based query methods added for performance optimization.
/// </summary>
[TestFixture]
public class PoiQueryOptimizationTests
{
    [Test]
    public void TryGetPointOfInterestByResRef_ReturnsTrue_WhenPoiExists()
    {
        // Arrange
        PlaceOfInterest bank = new("bank_cordor", "cordor_bank", "Bank of Cordor", PoiType.Bank, "The main bank");
        PlaceOfInterest shop = new("shop_blacksmith", "blacksmith_shop", "Blacksmith", PoiType.Shop);

        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [bank, shop])));

        // Act
        bool foundBank = repo.TryGetPointOfInterestByResRef("bank_cordor", out PlaceOfInterest retrievedPoi);

        // Assert
        Assert.That(foundBank, Is.True);
        Assert.That(retrievedPoi.ResRef, Is.EqualTo("bank_cordor"));
        Assert.That(retrievedPoi.Name, Is.EqualTo("Bank of Cordor"));
        Assert.That(retrievedPoi.Type, Is.EqualTo(PoiType.Bank));
        Assert.That(retrievedPoi.Description, Is.EqualTo("The main bank"));
    }

    [Test]
    public void TryGetPointOfInterestByResRef_ReturnsFalse_WhenPoiDoesNotExist()
    {
        // Arrange
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [])));

        // Act
        bool found = repo.TryGetPointOfInterestByResRef("nonexistent_poi", out PlaceOfInterest poi);

        // Assert
        Assert.That(found, Is.False);
        Assert.That(poi, Is.EqualTo(default(PlaceOfInterest)));
    }

    [Test]
    public void TryGetPointOfInterestByResRef_IsCaseInsensitive()
    {
        // Arrange
        PlaceOfInterest bank = new("bank_cordor", "cordor_bank", "Bank of Cordor", PoiType.Bank);
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [bank])));

        // Act & Assert
        Assert.That(repo.TryGetPointOfInterestByResRef("BANK_CORDOR", out _), Is.True);
        Assert.That(repo.TryGetPointOfInterestByResRef("Bank_Cordor", out _), Is.True);
        Assert.That(repo.TryGetPointOfInterestByResRef("bank_cordor", out _), Is.True);
    }

    [Test]
    public void GetPointsOfInterestByTag_ReturnsAllPoisWithTag()
    {
        // Arrange
        PlaceOfInterest bank1 = new("bank_cordor", "cordor_bank", "Bank of Cordor", PoiType.Bank);
        PlaceOfInterest bank2 = new("bank_branch", "cordor_bank", "Bank Branch", PoiType.Bank);
        PlaceOfInterest shop = new("shop_blacksmith", "blacksmith_shop", "Blacksmith", PoiType.Shop);

        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [bank1, bank2, shop])));

        // Act
        IReadOnlyList<PlaceOfInterest> cordorBanks = repo.GetPointsOfInterestByTag("cordor_bank");

        // Assert
        Assert.That(cordorBanks, Has.Count.EqualTo(2));
        Assert.That(cordorBanks.Any(p => p.ResRef == "bank_cordor"), Is.True);
        Assert.That(cordorBanks.Any(p => p.ResRef == "bank_branch"), Is.True);
        Assert.That(cordorBanks.Any(p => p.ResRef == "shop_blacksmith"), Is.False);
    }

    [Test]
    public void GetPointsOfInterestByTag_ReturnsEmpty_WhenNoMatchingTag()
    {
        // Arrange
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [])));

        // Act
        IReadOnlyList<PlaceOfInterest> pois = repo.GetPointsOfInterestByTag("nonexistent_tag");

        // Assert
        Assert.That(pois, Is.Empty);
    }

    [Test]
    public void GetPointsOfInterestByType_ReturnsAllPoisOfType()
    {
        // Arrange
        PlaceOfInterest bank1 = new("bank_cordor", "cordor_bank", "Bank of Cordor", PoiType.Bank);
        PlaceOfInterest bank2 = new("bank_ulatos", "ulatos_bank", "Bank of Ulatos", PoiType.Bank);
        PlaceOfInterest shop1 = new("shop_blacksmith", "blacksmith_shop", "Blacksmith", PoiType.Shop);
        PlaceOfInterest shop2 = new("shop_tailor", "tailor_shop", "Tailor", PoiType.Shop);

        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [bank1, shop1])));
        repo.Add(CreateRegion("ulatos", "Ulatos",
            CreateArea("ulatos_market", 2, [bank2, shop2])));

        // Act
        IReadOnlyList<PlaceOfInterest> allBanks = repo.GetPointsOfInterestByType(PoiType.Bank);
        IReadOnlyList<PlaceOfInterest> allShops = repo.GetPointsOfInterestByType(PoiType.Shop);

        // Assert
        Assert.That(allBanks, Has.Count.EqualTo(2));
        Assert.That(allBanks.Any(p => p.ResRef == "bank_cordor"), Is.True);
        Assert.That(allBanks.Any(p => p.ResRef == "bank_ulatos"), Is.True);

        Assert.That(allShops, Has.Count.EqualTo(2));
        Assert.That(allShops.Any(p => p.ResRef == "shop_blacksmith"), Is.True);
        Assert.That(allShops.Any(p => p.ResRef == "shop_tailor"), Is.True);
    }

    [Test]
    public void GetPointsOfInterestByType_ReturnsEmpty_WhenNoMatchingType()
    {
        // Arrange
        PlaceOfInterest bank = new("bank_cordor", "cordor_bank", "Bank of Cordor", PoiType.Bank);
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [bank])));

        // Act
        IReadOnlyList<PlaceOfInterest> temples = repo.GetPointsOfInterestByType(PoiType.Temple);

        // Assert
        Assert.That(temples, Is.Empty);
    }

    [Test]
    public void GetPointsOfInterestByType_IgnoresUndefinedType()
    {
        // Arrange
        InMemoryRegionRepository repo = new();

        // Act
        IReadOnlyList<PlaceOfInterest> undefined = repo.GetPointsOfInterestByType(PoiType.Undefined);

        // Assert
        Assert.That(undefined, Is.Empty);
    }

    [Test]
    public void GetPointsOfInterestForArea_ReturnsAllPoisInArea()
    {
        // Arrange
        PlaceOfInterest bank = new("bank_cordor", "cordor_bank", "Bank of Cordor", PoiType.Bank);
        PlaceOfInterest shop = new("shop_blacksmith", "blacksmith_shop", "Blacksmith", PoiType.Shop);
        PlaceOfInterest guild = new("guild_warriors", "warriors_guild", "Warriors Guild", PoiType.Guild);

        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [bank, shop]),
            CreateArea("cordor_guild", 1, [guild])));

        // Act
        IReadOnlyList<PlaceOfInterest> districtPois = repo.GetPointsOfInterestForArea(new AreaTag("cordor_district"));
        IReadOnlyList<PlaceOfInterest> guildPois = repo.GetPointsOfInterestForArea(new AreaTag("cordor_guild"));

        // Assert
        Assert.That(districtPois, Has.Count.EqualTo(2));
        Assert.That(districtPois.Any(p => p.ResRef == "bank_cordor"), Is.True);
        Assert.That(districtPois.Any(p => p.ResRef == "shop_blacksmith"), Is.True);

        Assert.That(guildPois, Has.Count.EqualTo(1));
        Assert.That(guildPois.Any(p => p.ResRef == "guild_warriors"), Is.True);
    }

    [Test]
    public void GetPoiLocationInfo_ReturnsCompleteLocationContext()
    {
        // Arrange
        PlaceOfInterest bank = new("bank_cordor", "cordor_bank", "Bank of Cordor", PoiType.Bank);
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 42, [bank])));

        // Act
        PoiLocationInfo? location = repo.GetPoiLocationInfo("bank_cordor");

        // Assert
        Assert.That(location, Is.Not.Null);
        Assert.That(location!.Poi.ResRef, Is.EqualTo("bank_cordor"));
        Assert.That(location.SettlementId?.Value, Is.EqualTo(42));
        Assert.That(location.RegionTag.Value, Is.EqualTo("cordor"));
        Assert.That(location.AreaTag.Value, Is.EqualTo("cordor_district"));
        Assert.That(location.HasSettlement, Is.True);
    }

    [Test]
    public void GetPoiLocationInfo_ReturnsNull_WhenPoiDoesNotExist()
    {
        // Arrange
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [])));

        // Act
        PoiLocationInfo? location = repo.GetPoiLocationInfo("nonexistent_poi");

        // Assert
        Assert.That(location, Is.Null);
    }

    [Test]
    public void GetPoiLocationInfo_HandlesPoiWithoutSettlement()
    {
        // Arrange
        PlaceOfInterest dungeon = new("dungeon_ruins", "ancient_ruins", "Ancient Ruins", PoiType.Dungeon);
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("wilderness", "Wilderness",
            CreateAreaWithoutSettlement("wild_forest", [dungeon])));

        // Act
        PoiLocationInfo? location = repo.GetPoiLocationInfo("dungeon_ruins");

        // Assert
        Assert.That(location, Is.Not.Null);
        Assert.That(location!.Poi.ResRef, Is.EqualTo("dungeon_ruins"));
        Assert.That(location.SettlementId, Is.Null);
        Assert.That(location.RegionTag.Value, Is.EqualTo("wilderness"));
        Assert.That(location.AreaTag.Value, Is.EqualTo("wild_forest"));
        Assert.That(location.HasSettlement, Is.False);
    }

    [Test]
    public void PoiIndexes_AreRebuilt_WhenRegionUpdated()
    {
        // Arrange
        PlaceOfInterest oldBank = new("bank_cordor", "cordor_bank", "Old Bank", PoiType.Bank);
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [oldBank])));

        // Verify initial state
        Assert.That(repo.TryGetPointOfInterestByResRef("bank_cordor", out PlaceOfInterest poi), Is.True);
        Assert.That(poi.Name, Is.EqualTo("Old Bank"));

        // Act - Update region with different POI
        PlaceOfInterest newBank = new("bank_cordor", "cordor_bank", "New Bank", PoiType.Bank);
        repo.Update(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [newBank])));

        // Assert - Index reflects updated data
        Assert.That(repo.TryGetPointOfInterestByResRef("bank_cordor", out poi), Is.True);
        Assert.That(poi.Name, Is.EqualTo("New Bank"));
    }

    [Test]
    public void PoiIndexes_AreCleaned_WhenPoiRemoved()
    {
        // Arrange
        PlaceOfInterest bank = new("bank_cordor", "cordor_bank", "Bank of Cordor", PoiType.Bank);
        PlaceOfInterest shop = new("shop_blacksmith", "blacksmith_shop", "Blacksmith", PoiType.Shop);
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [bank, shop])));

        // Verify initial state
        Assert.That(repo.GetPointsOfInterestByType(PoiType.Bank), Has.Count.EqualTo(1));

        // Act - Update region without the bank
        repo.Update(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [shop])));

        // Assert - Bank no longer in indexes
        Assert.That(repo.TryGetPointOfInterestByResRef("bank_cordor", out _), Is.False);
        Assert.That(repo.GetPointsOfInterestByType(PoiType.Bank), Is.Empty);
        Assert.That(repo.GetPointsOfInterestByTag("cordor_bank"), Is.Empty);
    }

    [Test]
    public void Clear_RemovesAllPoiIndexes()
    {
        // Arrange
        PlaceOfInterest bank = new("bank_cordor", "cordor_bank", "Bank of Cordor", PoiType.Bank);
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [bank])));

        // Verify initial state
        Assert.That(repo.TryGetPointOfInterestByResRef("bank_cordor", out _), Is.True);

        // Act
        repo.Clear();

        // Assert
        Assert.That(repo.TryGetPointOfInterestByResRef("bank_cordor", out _), Is.False);
        Assert.That(repo.GetPointsOfInterestByType(PoiType.Bank), Is.Empty);
        Assert.That(repo.GetPoiLocationInfo("bank_cordor"), Is.Null);
    }

    // Test helpers following DDD factory pattern

    private static RegionDefinition CreateRegion(string tag, string name, params AreaDefinition[] areas)
    {
        return new RegionDefinition
        {
            Tag = new RegionTag(tag),
            Name = name,
            Areas = areas.ToList()
        };
    }

    private static AreaDefinition CreateArea(string resRef, int settlementId, List<PlaceOfInterest> pois)
    {
        return new AreaDefinition(
            new AreaTag(resRef),
            new List<string>(),
            new EnvironmentData(Climate.Temperate, EconomyQuality.Average, new QualityRange()),
            pois,
            SettlementId.Parse(settlementId));
    }

    private static AreaDefinition CreateAreaWithoutSettlement(string resRef, List<PlaceOfInterest> pois)
    {
        return new AreaDefinition(
            new AreaTag(resRef),
            new List<string>(),
            new EnvironmentData(Climate.Temperate, EconomyQuality.Average, new QualityRange()),
            pois,
            LinkedSettlement: null);
    }
}

