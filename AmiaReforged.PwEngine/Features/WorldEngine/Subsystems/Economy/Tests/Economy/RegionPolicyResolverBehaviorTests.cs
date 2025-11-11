using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Taxation;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Economy;

[TestFixture]
public class RegionPolicyResolverBehaviorTests
{
    [Test]
    public void Coinhouse_Found_But_Settlement_Unknown_Returns_False()
    {
        Mock<ICoinhouseRepository> coinhouses = new(MockBehavior.Strict);
        CoinhouseTag coinhouseTag = new("ch1");
        coinhouses
            .Setup(c => c.GetByTagAsync(coinhouseTag, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CoinhouseDto
            {
                Id = 100,
                Tag = coinhouseTag,
                Settlement = 999,
                EngineId = Guid.NewGuid(),
                Persona = PersonaId.FromCoinhouse(coinhouseTag)
            });
        InMemoryRegionRepository repo = new();
        RegionIndex index = new(repo);
        RegionPolicyResolver resolver = new(coinhouses.Object, index);

        Assert.That(resolver.TryGetRegionTagForCoinhouseTag("ch1", out string? regionTag), Is.False);
        Assert.That(regionTag, Is.Null);
    }

    [Test]
    public void Null_Or_Empty_Input_Returns_False()
    {
        Mock<ICoinhouseRepository> coinhouses = new(MockBehavior.Strict);
        RegionPolicyResolver resolver = new(coinhouses.Object, new RegionIndex(new InMemoryRegionRepository()));

        Assert.That(resolver.TryGetRegionTagForCoinhouseTag(null!, out string? _), Is.False);
        Assert.That(resolver.TryGetRegionTagForCoinhouseTag("", out string? _), Is.False);
        Assert.That(resolver.TryGetRegionTagForCoinhouseTag("   ", out string? _), Is.False);
    }

    [Test]
    public void Repository_Exception_Is_Swalllowed_And_Returns_False()
    {
        Mock<ICoinhouseRepository> coinhouses = new(MockBehavior.Strict);
        CoinhouseTag coinhouseTag = new("boom");
        coinhouses
            .Setup(c => c.GetByTagAsync(coinhouseTag, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db is down"));

        RegionPolicyResolver resolver = new(coinhouses.Object, new RegionIndex(new InMemoryRegionRepository()));

        Assert.That(resolver.TryGetRegionTagForCoinhouseTag("boom", out string? regionTag), Is.False);
        Assert.That(regionTag, Is.Null);
    }
}

