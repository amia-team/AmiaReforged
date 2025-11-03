using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Shops;

[TestFixture]
public sealed class ShopLocationResolverTests
{
    [Test]
    public void Resolves_By_Shop_Tag()
    {
        SettlementId settlementId = SettlementId.Parse(101);
        RegionIndex index = BuildIndex(
            regionTag: "cordor_region",
            areaResRef: "cordor_market",
            settlementId,
            new PlaceOfInterest("cordor_furnishings_interior", "cordor_furnishings", "Cordor Furnishings", PoiType.Shop,
                "A municipal furnishings vendor."));

        ShopLocationResolver resolver = new(index);

        bool resolved = resolver.TryResolve(
            shopTag: "cordor_furnishings",
            shopDisplayName: "Legacy Furnishings",
            shopkeeperTag: "cordor_shopkeeper",
            shopkeeperResRef: "cordor_shopkeeper_resref",
            areaResRef: "cordor_furnishings_interior",
            areaTag: "market_area",
            areaName: "Cordor Market",
            out ShopLocationMetadata metadata);

        Assert.That(resolved, Is.True);
        Assert.That(metadata.ShopTag, Is.EqualTo("cordor_furnishings"));
        Assert.That(metadata.ShopDisplayName, Is.EqualTo("Cordor Furnishings"));
        Assert.That(metadata.ShopkeeperTag, Is.EqualTo("cordor_shopkeeper"));
        Assert.That(metadata.ShopkeeperResRef, Is.EqualTo("cordor_shopkeeper_resref"));
        Assert.That(metadata.AreaResRef, Is.EqualTo("cordor_furnishings_interior"));
        Assert.That(metadata.AreaTag, Is.EqualTo("market_area"));
        Assert.That(metadata.AreaName, Is.EqualTo("Cordor Market"));
        Assert.That(metadata.SettlementId.Value, Is.EqualTo(settlementId.Value));
        Assert.That(metadata.Settlement.Value, Is.EqualTo("cordor_furnishings"));
        Assert.That(metadata.RegionTag.Value, Is.EqualTo("cordor_region"));
        Assert.That(metadata.PoiTag, Is.EqualTo("cordor_furnishings"));
        Assert.That(metadata.PoiResRef, Is.EqualTo("cordor_furnishings_interior"));
        Assert.That(metadata.PoiName, Is.EqualTo("Cordor Furnishings"));
        Assert.That(metadata.PoiDescription, Is.EqualTo("A municipal furnishings vendor."));
    }

    [Test]
    public void Resolves_By_Shopkeeper_Tag_When_Shop_Tag_Differs()
    {
        SettlementId settlementId = SettlementId.Parse(205);
        RegionIndex index = BuildIndex(
            regionTag: "thay_region",
            areaResRef: "thay_market",
            settlementId,
            new PlaceOfInterest("thay_arms_interior", "thay_smith_keeper", "Thayan Smith", PoiType.Shop));

        ShopLocationResolver resolver = new(index);

        bool resolved = resolver.TryResolve(
            shopTag: "thay_arms",
            shopDisplayName: "Thay Arms",
            shopkeeperTag: "thay_smith_keeper",
            shopkeeperResRef: null,
            areaResRef: "thay_arms_interior",
            areaTag: null,
            areaName: null,
            out ShopLocationMetadata metadata);

        Assert.That(resolved, Is.True);
        Assert.That(metadata.ShopTag, Is.EqualTo("thay_arms"));
        Assert.That(metadata.PoiTag, Is.EqualTo("thay_smith_keeper"));
        Assert.That(metadata.RegionTag.Value, Is.EqualTo("thay_region"));
        Assert.That(metadata.SettlementId.Value, Is.EqualTo(settlementId.Value));
    }

    [Test]
    public void Returns_False_When_No_Shop_Poi_Matches()
    {
        SettlementId settlementId = SettlementId.Parse(77);
        RegionIndex index = BuildIndex(
            regionTag: "luskan_region",
            areaResRef: "luskan_bank",
            settlementId,
            new PlaceOfInterest("luskan_bank_interior", "luskan_bank", "Luskan Treasury", PoiType.Bank));

        ShopLocationResolver resolver = new(index);

        bool resolved = resolver.TryResolve(
            shopTag: "luskan_supplies",
            shopDisplayName: "Luskan Supplies",
            shopkeeperTag: "luskan_supplies_keeper",
            shopkeeperResRef: null,
            areaResRef: "luskan_bank_interior",
            areaTag: null,
            areaName: null,
            out ShopLocationMetadata metadata);

        Assert.That(resolved, Is.False);
        Assert.That(metadata, Is.Null);
    }

    private static RegionIndex BuildIndex(string regionTag, string areaResRef, SettlementId settlementId,
        PlaceOfInterest poi)
    {
        InMemoryRegionRepository repository = new();

        AreaDefinition area = new(
            new AreaTag(areaResRef),
            new List<string>(),
            new EnvironmentData(Climate.Temperate, EconomyQuality.Average, new QualityRange()),
            new List<PlaceOfInterest> { poi },
            settlementId);

        RegionDefinition region = new()
        {
            Tag = new RegionTag(regionTag),
            Name = regionTag,
            Areas = new List<AreaDefinition> { area }
        };

        repository.Add(region);
        return new RegionIndex(repository);
    }
}
