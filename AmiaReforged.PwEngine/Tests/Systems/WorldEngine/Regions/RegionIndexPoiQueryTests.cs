
using System.Collections.Generic;
using System.Linq;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Regions;

/// <summary>
/// Tests for RegionIndex facade methods that wrap optimized POI queries.
/// Validates the public API that consumers will use instead of directly accessing repository.
/// </summary>
[TestFixture]
public class RegionIndexPoiQueryTests
{
    [Test]
    public void TryGetPointOfInterest_DelegatesTo_Repository()
    {
        // Arrange
        PlaceOfInterest bank = new("bank_cordor", "cordor_bank", "Bank of Cordor", PoiType.Bank);
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [bank])));
        RegionIndex index = new(repo);

        // Act
        bool found = index.TryGetPointOfInterest("bank_cordor", out PlaceOfInterest poi);

        // Assert
        Assert.That(found, Is.True);
        Assert.That(poi.ResRef, Is.EqualTo("bank_cordor"));
        Assert.That(poi.Name, Is.EqualTo("Bank of Cordor"));
    }

    [Test]
    public void GetPointsOfInterestByTag_ReturnsAllMatchingPois()
    {
        // Arrange
        PlaceOfInterest bank1 = new("bank_main", "cordor_bank", "Main Bank", PoiType.Bank);
        PlaceOfInterest bank2 = new("bank_branch", "cordor_bank", "Branch Bank", PoiType.Bank);
        PlaceOfInterest shop = new("shop_weapons", "weapon_shop", "Weapon Shop", PoiType.Shop);

        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [bank1, bank2, shop])));
        RegionIndex index = new(repo);

        // Act
        IReadOnlyList<PlaceOfInterest> cordorBanks = index.GetPointsOfInterestByTag("cordor_bank");

        // Assert
        Assert.That(cordorBanks, Has.Count.EqualTo(2));
        Assert.That(cordorBanks.All(p => p.Tag == "cordor_bank"), Is.True);
    }

    [Test]
    public void GetPointsOfInterestByType_ReturnsAllPoisOfGivenType()
    {
        // Arrange
        PlaceOfInterest bank = new("bank_cordor", "cordor_bank", "Bank", PoiType.Bank);
        PlaceOfInterest shop1 = new("shop_blacksmith", "blacksmith", "Blacksmith", PoiType.Shop);
        PlaceOfInterest shop2 = new("shop_tailor", "tailor", "Tailor", PoiType.Shop);

        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_market", 1, [bank, shop1, shop2])));
        RegionIndex index = new(repo);

        // Act
        IReadOnlyList<PlaceOfInterest> allShops = index.GetPointsOfInterestByType(PoiType.Shop);

        // Assert
        Assert.That(allShops, Has.Count.EqualTo(2));
        Assert.That(allShops.All(p => p.Type == PoiType.Shop), Is.True);
    }

    [Test]
    public void GetPointsOfInterestForArea_ReturnsOnlyPoisInSpecifiedArea()
    {
        // Arrange
        PlaceOfInterest bank = new("bank_cordor", "cordor_bank", "Bank", PoiType.Bank);
        PlaceOfInterest shop = new("shop_blacksmith", "blacksmith", "Blacksmith", PoiType.Shop);
        PlaceOfInterest temple = new("temple_tyr", "temple_tyr", "Temple of Tyr", PoiType.Temple);

        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_market", 1, [bank, shop]),
            CreateArea("cordor_temple", 1, [temple])));
        RegionIndex index = new(repo);

        // Act
        IReadOnlyList<PlaceOfInterest> marketPois = index.GetPointsOfInterestForArea(new AreaTag("cordor_market"));

        // Assert
        Assert.That(marketPois, Has.Count.EqualTo(2));
        Assert.That(marketPois.Any(p => p.ResRef == "bank_cordor"), Is.True);
        Assert.That(marketPois.Any(p => p.ResRef == "shop_blacksmith"), Is.True);
        Assert.That(marketPois.Any(p => p.ResRef == "temple_tyr"), Is.False);
    }

    [Test]
    public void GetPoiLocationInfo_ReturnsCompleteContext_InSingleQuery()
    {
        // Arrange
        PlaceOfInterest bank = new("bank_cordor", "cordor_bank", "Bank of Cordor", PoiType.Bank, "The main bank");
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 42, [bank])));
        RegionIndex index = new(repo);

        // Act
        PoiLocationInfo? location = index.GetPoiLocationInfo("bank_cordor");

        // Assert - All context retrieved in single query
        Assert.That(location, Is.Not.Null);
        Assert.That(location!.Poi.ResRef, Is.EqualTo("bank_cordor"));
        Assert.That(location.Poi.Name, Is.EqualTo("Bank of Cordor"));
        Assert.That(location.Poi.Type, Is.EqualTo(PoiType.Bank));
        Assert.That(location.SettlementId?.Value, Is.EqualTo(42));
        Assert.That(location.RegionTag.Value, Is.EqualTo("cordor"));
        Assert.That(location.AreaTag.Value, Is.EqualTo("cordor_district"));
    }

    [Test]
    public void GetPoiLocationInfo_ReturnsNull_WhenPoiNotFound()
    {
        // Arrange
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [])));
        RegionIndex index = new(repo);

        // Act
        PoiLocationInfo? location = index.GetPoiLocationInfo("nonexistent_poi");

        // Assert
        Assert.That(location, Is.Null);
    }

    [Test]
    public void ResolveSettlement_TriesPoiFirst_ThenFallsBackToArea()
    {
        // Arrange
        PlaceOfInterest bank = new("bank_cordor", "cordor_bank", "Bank", PoiType.Bank);
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_market", 10, [bank]),
            CreateArea("cordor_residen", 20, [])));
        RegionIndex index = new(repo);

        // Act - POI ResRef resolves to its settlement
        SettlementId? poiSettlement = index.ResolveSettlement("bank_cordor");

        // Act - Area Tag resolves to its settlement
        SettlementId? areaSettlement = index.ResolveSettlement("cordor_residen");

        // Assert
        Assert.That(poiSettlement?.Value, Is.EqualTo(10));
        Assert.That(areaSettlement?.Value, Is.EqualTo(20));
    }

    [Test]
    public void ResolveSettlement_ReturnsNull_WhenNeitherPoiNorAreaFound()
    {
        // Arrange
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [])));
        RegionIndex index = new(repo);

        // Act
        SettlementId? settlement = index.ResolveSettlement("nonexistent");

        // Assert
        Assert.That(settlement, Is.Null);
    }

    [Test]
    public void ResolveSettlement_HandlesInvalidAreaTagGracefully()
    {
        // Arrange
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_district", 1, [])));
        RegionIndex index = new(repo);

        // Act - Empty string is invalid area tag
        SettlementId? settlement = index.ResolveSettlement("");

        // Assert - Should return null, not throw
        Assert.That(settlement, Is.Null);
    }

    [Test]
    public void PoiLocationInfo_HasSettlement_ReflectsSettlementPresence()
    {
        // Arrange
        PlaceOfInterest cityBank = new("bank_cordor", "city_bank", "City Bank", PoiType.Bank);
        PlaceOfInterest wildDungeon = new("dungeon_ruins", "ruins", "Ancient Ruins", PoiType.Dungeon);

        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_city", 1, [cityBank])));
        repo.Add(CreateRegion("wilderness", "Wilderness",
            CreateAreaWithoutSettlement("wild_forest", [wildDungeon])));
        RegionIndex index = new(repo);

        // Act
        PoiLocationInfo? cityLocation = index.GetPoiLocationInfo("bank_cordor");
        PoiLocationInfo? wildLocation = index.GetPoiLocationInfo("dungeon_ruins");

        // Assert
        Assert.That(cityLocation?.HasSettlement, Is.True);
        Assert.That(wildLocation?.HasSettlement, Is.False);
    }

    [Test]
    public void PoiLocationInfo_LocationDescription_ProvidesReadableLocation()
    {
        // Arrange
        PlaceOfInterest bank = new("bank_cordor", "cordor_bank", "Bank of Cordor", PoiType.Bank);
        InMemoryRegionRepository repo = new();
        repo.Add(CreateRegion("cordor", "Cordor",
            CreateArea("cordor_market", 42, [bank])));
        RegionIndex index = new(repo);

        // Act
        PoiLocationInfo? location = index.GetPoiLocationInfo("bank_cordor");

        // Assert
        Assert.That(location?.LocationDescription, Does.Contain("Bank of Cordor"));
        Assert.That(location?.LocationDescription, Does.Contain("cordor_market"));
        Assert.That(location?.LocationDescription, Does.Contain("cordor"));
        Assert.That(location?.LocationDescription, Does.Contain("42"));
    }

    // Test helpers

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

