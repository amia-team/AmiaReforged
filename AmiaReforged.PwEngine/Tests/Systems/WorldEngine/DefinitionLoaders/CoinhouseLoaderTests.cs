using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
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

            var coinhouses = new Mock<ICoinhouseRepository>(MockBehavior.Strict);
            coinhouses.Setup(c => c.SettlementHasCoinhouse(It.IsAny<int>())).Returns(false);
            coinhouses.Setup(c => c.TagExists(It.IsAny<string>())).Returns(false);

            var regions = new Mock<IRegionRepository>(MockBehavior.Strict);
            regions.Setup(r => r.TryGetRegionBySettlement(It.IsAny<int>(), out It.Ref<RegionDefinition?>.IsAny))
                   .Returns(false);

            CoinhouseLoader loader = new(coinhouses.Object, regions.Object);

            // Act
            loader.Load();

            // Assert
            var failures = loader.Failures();
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

            var coinhouses = new Mock<ICoinhouseRepository>(MockBehavior.Strict);
            coinhouses.Setup(c => c.SettlementHasCoinhouse(101)).Returns(false);
            coinhouses.Setup(c => c.TagExists("ch-southport")).Returns(false);
            coinhouses.Setup(c => c.AddNewCoinhouse(It.IsAny<CoinHouse>()));

            var regions = new Mock<IRegionRepository>(MockBehavior.Strict);
            RegionDefinition region = new() { Tag = "region-a", Name = "Region A", Areas = new(), Settlements = new(){101} };
            regions.Setup(r => r.TryGetRegionBySettlement(101, out region)).Returns(true);

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

