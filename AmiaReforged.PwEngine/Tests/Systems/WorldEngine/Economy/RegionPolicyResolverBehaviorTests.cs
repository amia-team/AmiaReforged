using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Taxation;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy;

[TestFixture]
public class RegionPolicyResolverBehaviorTests
{
    [Test]
    public void Coinhouse_Found_But_Settlement_Unknown_Returns_False()
    {
        Mock<ICoinhouseRepository> coinhouses = new Mock<ICoinhouseRepository>(MockBehavior.Strict);
        coinhouses.Setup(c => c.GetByTag(new CoinhouseTag("ch1"))).Returns(new CoinHouse { Tag = "ch1", Settlement = 999, EngineId = Guid.NewGuid() });
        InMemoryRegionRepository repo = new();
        RegionIndex index = new(repo);
        RegionPolicyResolver resolver = new(coinhouses.Object, index);

        Assert.That(resolver.TryGetRegionTagForCoinhouseTag("ch1", out string? tag), Is.False);
        Assert.That(tag, Is.Null);
    }

    [Test]
    public void Null_Or_Empty_Input_Returns_False()
    {
        Mock<ICoinhouseRepository> coinhouses = new Mock<ICoinhouseRepository>(MockBehavior.Strict);
        RegionPolicyResolver resolver = new(coinhouses.Object, new RegionIndex(new InMemoryRegionRepository()));

        Assert.That(resolver.TryGetRegionTagForCoinhouseTag(null!, out string? _), Is.False);
        Assert.That(resolver.TryGetRegionTagForCoinhouseTag("", out string? _), Is.False);
        Assert.That(resolver.TryGetRegionTagForCoinhouseTag("   ", out string? _), Is.False);
    }

    [Test]
    public void Repository_Exception_Is_Swalllowed_And_Returns_False()
    {
        Mock<ICoinhouseRepository> coinhouses = new Mock<ICoinhouseRepository>(MockBehavior.Strict);
        coinhouses.Setup(c => c.GetByTag(new CoinhouseTag("boom"))).Throws(new Exception("db is down"));

        RegionPolicyResolver resolver = new(coinhouses.Object, new RegionIndex(new InMemoryRegionRepository()));

        Assert.That(resolver.TryGetRegionTagForCoinhouseTag("boom", out string? tag), Is.False);
        Assert.That(tag, Is.Null);
    }
}

