using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.DefinitionLoaders;

[TestFixture]
public class CoinhouseLoaderTests
{
    private string? _originalResourcePath;

    [SetUp]
    public void SetUp() => _originalResourcePath = Environment.GetEnvironmentVariable("RESOURCE_PATH");

    [TearDown]
    public void TearDown() => Environment.SetEnvironmentVariable("RESOURCE_PATH", _originalResourcePath);

    [Test]
    public void Load_Rejects_Coinhouse_When_Settlement_NotInAnyRegion()
    {
        DirectoryInfo tempRoot = Directory.CreateTempSubdirectory("coinhouse-missing-settlement");
        try
        {
            string coinDir = Path.Combine(tempRoot.FullName, "Economy", "Coinhouses");
            Directory.CreateDirectory(coinDir);

            File.WriteAllText(Path.Combine(coinDir, "invalid.json"), """
            {"Tag":"ch-invalid","Settlement":999999}
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", tempRoot.FullName);

            Mock<ICoinhouseRepository> coinhouses = new Mock<ICoinhouseRepository>(MockBehavior.Strict);
            coinhouses.Setup(c => c.SettlementHasCoinhouse(It.IsAny<SettlementId>())).Returns(false);
            coinhouses.Setup(c => c.TagExists(It.IsAny<CoinhouseTag>())).Returns(false);

            Mock<IRegionRepository> regions = new Mock<IRegionRepository>(MockBehavior.Strict);
            regions.Setup(r => r.TryGetRegionBySettlement(It.IsAny<SettlementId>(), out It.Ref<RegionDefinition?>.IsAny))
                   .Returns(false);

            CoinhouseLoader loader = new(coinhouses.Object, regions.Object);

            // Act
            loader.Load();

            // Assert
            List<FileLoadResult> failures = loader.Failures();
            Assert.That(failures, Is.Not.Empty);
            Assert.That(failures.Any(f => f.FileName == "invalid.json" && (f.Message?.Contains("Settlement is not defined", StringComparison.OrdinalIgnoreCase) ?? false)), Is.True);

            coinhouses.Verify(c => c.AddNewCoinhouse(It.IsAny<CoinHouse>()), Times.Never);
        }
        finally
        {
            try { Directory.Delete(tempRoot.FullName, true); } catch { /* ignore */ }
        }
    }

    [Test]
    public void Load_Adds_Coinhouse_When_Settlement_InRegion()
    {
        DirectoryInfo tempRoot = Directory.CreateTempSubdirectory("coinhouse-valid");
        try
        {
            string coinDir = Path.Combine(tempRoot.FullName, "Economy", "Coinhouses");
            Directory.CreateDirectory(coinDir);

            File.WriteAllText(Path.Combine(coinDir, "southport.json"), """
            {"Tag":"ch-southport","Settlement":101}
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", tempRoot.FullName);

            Mock<ICoinhouseRepository> coinhouses = new Mock<ICoinhouseRepository>(MockBehavior.Strict);
            coinhouses.Setup(c => c.SettlementHasCoinhouse(SettlementId.Parse(101))).Returns(false);
            coinhouses.Setup(c => c.TagExists(new CoinhouseTag("ch-southport"))).Returns(false);
            coinhouses.Setup(c => c.AddNewCoinhouse(It.IsAny<CoinHouse>()));

            Mock<IRegionRepository> regions = new Mock<IRegionRepository>(MockBehavior.Strict);
            RegionDefinition region = new() { Tag = new RegionTag("region-a"), Name = "Region A", Areas = new(), Settlements = new(){SettlementId.Parse(101)} };
            regions.Setup(r => r.TryGetRegionBySettlement(SettlementId.Parse(101), out region)).Returns(true);

            CoinhouseLoader loader = new(coinhouses.Object, regions.Object);

            // Act
            loader.Load();

            // Assert
            Assert.That(loader.Failures(), Is.Empty);
            coinhouses.Verify(c => c.AddNewCoinhouse(It.Is<CoinHouse>(ch => ch.Tag == "ch-southport" && ch.Settlement == 101)), Times.Once);
        }
        finally
        {
            try { Directory.Delete(tempRoot.FullName, true); } catch { /* ignore */ }
        }
    }
}

