using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Taxation;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Economy;

[TestFixture]
public class RegionPolicyResolverTests
{
    [Test]
    public void TryGetRegionTagForCoinhouseTag_Returns_RegionTag_When_Found()
    {
        Mock<ICoinhouseRepository> coinhouses = new(MockBehavior.Strict);
        CoinhouseTag coinhouseTag = new("ch1");
        CoinhouseDto dto = new()
        {
            Id = 10,
            Tag = coinhouseTag,
            Settlement = 42,
            EngineId = Guid.NewGuid(),
            Persona = PersonaId.FromCoinhouse(coinhouseTag)
        };
        coinhouses
            .Setup(c => c.GetByTagAsync(coinhouseTag, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        InMemoryRegionRepository repo = new();
        repo.Add(new RegionDefinition
        {
            Tag = new RegionTag("rX"),
            Name = "Region X",
            Areas = [CreateArea("rx-area", 42)]
        });
        RegionIndex index = new(repo);

        RegionPolicyResolver resolver = new(coinhouses.Object, index);

        Assert.That(resolver.TryGetRegionTagForCoinhouseTag("ch1", out string? regionTag), Is.True);
        Assert.That(regionTag, Is.EqualTo("rx"));  // RegionTag normalizes to lowercase
    }

    [Test]
    public void TryGetRegionTagForCoinhouseTag_Fails_When_Coinhouse_NotFound()
    {
        Mock<ICoinhouseRepository> coinhouses = new(MockBehavior.Strict);
        CoinhouseTag coinhouseTag = new("missing");
        coinhouses
            .Setup(c => c.GetByTagAsync(coinhouseTag, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseDto?)null);

        InMemoryRegionRepository repo = new();
        RegionIndex index = new(repo);

        RegionPolicyResolver resolver = new(coinhouses.Object, index);
        Assert.That(resolver.TryGetRegionTagForCoinhouseTag("missing", out string? _), Is.False);
    }

    private static AreaDefinition CreateArea(string resRef, int settlement)
    {
        return new AreaDefinition(
            new AreaTag(resRef),
            new List<string>(),
            new EnvironmentData(Climate.Temperate, EconomyQuality.Average, new QualityRange()),
            LinkedSettlement: SettlementId.Parse(settlement));
    }
}

