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
public class CoinhouseLoaderBehaviorTests
{
    private string? _originalResourcePath;

    [SetUp]
    public void SetUp() => _originalResourcePath = Environment.GetEnvironmentVariable("RESOURCE_PATH");

    [TearDown]
    public void TearDown() => Environment.SetEnvironmentVariable("RESOURCE_PATH", _originalResourcePath);

    [Test]
    public void Unknown_Settlement_Rejected_With_Specific_Reason()
    {
        DirectoryInfo tmp = Directory.CreateTempSubdirectory("coinhouse-unknown-settlement");
        try
        {
            string dir = Path.Combine(tmp.FullName, "Economy", "Coinhouses");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "ch.json"), """ {"Tag":"ch-1","Settlement":777} """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", tmp.FullName);

            Mock<ICoinhouseRepository> coinhouses = new Mock<ICoinhouseRepository>(MockBehavior.Strict);
            coinhouses.Setup(c => c.SettlementHasCoinhouse(SettlementId.Parse(777))).Returns(false);
            coinhouses.Setup(c => c.TagExists(new CoinhouseTag("ch-1"))).Returns(false);

            Mock<IRegionRepository> regions = new Mock<IRegionRepository>(MockBehavior.Strict);
            RegionDefinition? notUsed;
            regions.Setup(r => r.TryGetRegionBySettlement(SettlementId.Parse(777), out notUsed)).Returns(false);

            CoinhouseLoader loader = new(coinhouses.Object, regions.Object);
            loader.Load();

            List<FileLoadResult> failures = loader.Failures();
            Assert.That(failures, Is.Not.Empty);
            Assert.That(failures.Any(f => f.Message?.Contains("Settlement is not defined", StringComparison.OrdinalIgnoreCase) ?? false), Is.True);
        }
        finally
        {
            try { Directory.Delete(tmp.FullName, true); } catch (Exception) { /* best-effort cleanup */ }
        }
    }

    [Test]
    public void Acceptance_Path_Known_Settlement_Adds_And_Accessible_By_Tag()
    {
        DirectoryInfo tmp = Directory.CreateTempSubdirectory("coinhouse-accept");
        try
        {
            string dir = Path.Combine(tmp.FullName, "Economy", "Coinhouses");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "ch.json"), """ {"Tag":"ch-2","Settlement":101} """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", tmp.FullName);

            Mock<ICoinhouseRepository> coinhouses = new Mock<ICoinhouseRepository>(MockBehavior.Strict);
            coinhouses.Setup(c => c.SettlementHasCoinhouse(SettlementId.Parse(101))).Returns(false);
            coinhouses.Setup(c => c.TagExists(new CoinhouseTag("ch-2"))).Returns(false);
            coinhouses.Setup(c => c.AddNewCoinhouse(It.Is<CoinHouse>(x => x.Tag == "ch-2" && x.Settlement == 101)));

            Mock<IRegionRepository> regions = new Mock<IRegionRepository>(MockBehavior.Strict);
            RegionDefinition existing = new() { Tag = new RegionTag("r1"), Name = "Region", Areas = new(), Settlements = new(){SettlementId.Parse(101)} };
            regions.Setup(r => r.TryGetRegionBySettlement(SettlementId.Parse(101), out existing)).Returns(true);

            CoinhouseLoader loader = new(coinhouses.Object, regions.Object);
            loader.Load();

            Assert.That(loader.Failures(), Is.Empty);
            coinhouses.Verify(c => c.AddNewCoinhouse(It.Is<CoinHouse>(x => x.Tag == "ch-2" && x.Settlement == 101)), Times.Once);
        }
        finally
        {
            try { Directory.Delete(tmp.FullName, true); } catch (Exception) { /* best-effort cleanup */ }
        }
    }

    [Test]
    public void Conflict_After_Region_Reload_Surfaces_Failure_On_Next_Validation()
    {
        DirectoryInfo tmp = Directory.CreateTempSubdirectory("coinhouse-reload-conflict");
        try
        {
            string dir = Path.Combine(tmp.FullName, "Economy", "Coinhouses");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "ch.json"), """ {"Tag":"ch-3","Settlement":202} """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", tmp.FullName);

            Mock<ICoinhouseRepository> coinhouses = new Mock<ICoinhouseRepository>(MockBehavior.Strict);
            coinhouses.Setup(c => c.SettlementHasCoinhouse(SettlementId.Parse(202))).Returns(false);
            coinhouses.Setup(c => c.TagExists(new CoinhouseTag("ch-3"))).Returns(false);
            coinhouses.Setup(c => c.AddNewCoinhouse(It.IsAny<CoinHouse>()));

            // First pass: region known
            Mock<IRegionRepository> regions = new Mock<IRegionRepository>(MockBehavior.Strict);
            RegionDefinition existing = new() { Tag = new RegionTag("r1"), Name = "Region", Areas = new(), Settlements = new(){SettlementId.Parse(202)} };
            regions.Setup(r => r.TryGetRegionBySettlement(SettlementId.Parse(202), out existing)).Returns(true);

            CoinhouseLoader loader = new(coinhouses.Object, regions.Object);
            loader.Load();
            Assert.That(loader.Failures(), Is.Empty);

            // Simulate regions reload where settlement becomes unknown
            Mock<IRegionRepository> regions2 = new Mock<IRegionRepository>(MockBehavior.Strict);
            RegionDefinition? notUsed;
            regions2.Setup(r => r.TryGetRegionBySettlement(SettlementId.Parse(202), out notUsed)).Returns(false);

            CoinhouseLoader loader2 = new(coinhouses.Object, regions2.Object);
            loader2.Load();
            Assert.That(loader2.Failures(), Is.Not.Empty);
            Assert.That(loader2.Failures().Any(f => f.Message?.Contains("Settlement is not defined", StringComparison.OrdinalIgnoreCase) ?? false), Is.True);
        }
        finally
        {
            try { Directory.Delete(tmp.FullName, true); } catch (Exception) { /* best-effort cleanup */ }
        }
    }
}
