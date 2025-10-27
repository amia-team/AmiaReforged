using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Taxation;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy;

[TestFixture]
public class RegionPolicyResolverTests
{
    [Test]
    public void TryGetRegionTagForCoinhouseTag_Returns_RegionTag_When_Found()
    {
        Mock<ICoinhouseRepository> coinhouses = new Mock<ICoinhouseRepository>(MockBehavior.Strict);
        CoinHouse ch = new() { Tag = "ch1", Settlement = 42, EngineId = Guid.NewGuid() };
        coinhouses.Setup(c => c.GetByTag(new CoinhouseTag("ch1"))).Returns(ch);

        InMemoryRegionRepository repo = new();
        repo.Add(new RegionDefinition { Tag = new RegionTag("rX"), Name = "Region X", Areas = new(), Settlements = new(){SettlementId.Parse(42)} });
        RegionIndex index = new(repo);

        RegionPolicyResolver resolver = new(coinhouses.Object, index);

        Assert.That(resolver.TryGetRegionTagForCoinhouseTag("ch1", out string? tag), Is.True);
        Assert.That(tag, Is.EqualTo("rx"));  // RegionTag normalizes to lowercase
    }

    [Test]
    public void TryGetRegionTagForCoinhouseTag_Fails_When_Coinhouse_NotFound()
    {
        Mock<ICoinhouseRepository> coinhouses = new Mock<ICoinhouseRepository>(MockBehavior.Strict);
        coinhouses.Setup(c => c.GetByTag(new CoinhouseTag("missing"))).Returns((CoinHouse?)null);

        InMemoryRegionRepository repo = new();
        RegionIndex index = new(repo);

        RegionPolicyResolver resolver = new(coinhouses.Object, index);
        Assert.That(resolver.TryGetRegionTagForCoinhouseTag("missing", out string? _), Is.False);
    }
}

