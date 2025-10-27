using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.DefinitionLoaders;

[TestFixture]
public class RegionDefinitionLoadingServiceTests
{
    private string? _originalResourcePath;

    [SetUp]
    public void SetUp()
    {
        _originalResourcePath = Environment.GetEnvironmentVariable("RESOURCE_PATH");
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("RESOURCE_PATH", _originalResourcePath);
    }

    [Test]
    public void Load_ReadsJson_WithSettlements_AndIndexesSettlementToRegion()
    {
        DirectoryInfo tempRoot = Directory.CreateTempSubdirectory("region-settlement-index");
        try
        {
            string regionsDir = Path.Combine(tempRoot.FullName, "Regions");
            Directory.CreateDirectory(regionsDir);

            File.WriteAllText(Path.Combine(regionsDir, "region_a.json"), """
            {
              "Tag": "region-a",
              "Name": "Region A",
              "Settlements": [ 10, 11 ],
              "Areas": [
                {"ResRef":"area_a1","DefinitionTags":[],"Environment": {"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}}}
              ]
            }
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", tempRoot.FullName);

            InMemoryRegionRepository repo = new();
            RegionDefinitionLoadingService loader = new(repo);

            // Act
            loader.Load();

            // Assert
            Assert.That(loader.Failures(), Is.Empty);

            Assert.That(repo.TryGetRegionBySettlement(SettlementId.Parse(10), out RegionDefinition? reg), Is.True);
            Assert.That(reg, Is.Not.Null);
            Assert.That(reg!.Tag.Value, Is.EqualTo("region-a"));

            IReadOnlyCollection<SettlementId> settlements = repo.GetSettlements(new RegionTag("region-a"));
            CollectionAssert.AreEquivalent(new[] {10, 11}, settlements.Select(s => s.Value).ToArray());
        }
        finally
        {
            try { Directory.Delete(tempRoot.FullName, true); } catch { /* ignore */ }
        }
    }

    [Test]
    public void Load_Rejects_InvalidSettlementIds()
    {
        DirectoryInfo tempRoot = Directory.CreateTempSubdirectory("region-invalid-settlement");
        try
        {
            string regionsDir = Path.Combine(tempRoot.FullName, "Regions");
            Directory.CreateDirectory(regionsDir);

            File.WriteAllText(Path.Combine(regionsDir, "bad_region.json"), """
            {
              "Tag": "region-bad",
              "Name": "Region Bad",
              "Settlements": [ -1 ],
              "Areas": [
                {"ResRef":"area_b1","DefinitionTags":[],"Environment": {"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}}}
              ]
            }
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", tempRoot.FullName);

            InMemoryRegionRepository repo = new();
            RegionDefinitionLoadingService loader = new(repo);

            // Act
            loader.Load();

            // Assert
            List<FileLoadResult> failures = loader.Failures();
            Assert.That(failures, Is.Not.Empty);
            Assert.That(failures.Any(f => f.FileName == "bad_region.json" &&
                (f.Message?.Contains("Settlement IDs must be positive integers", StringComparison.OrdinalIgnoreCase) ?? false)), Is.True);

            Assert.That(repo.All(), Is.Empty);
        }
        finally
        {
            try { Directory.Delete(tempRoot.FullName, true); } catch { /* ignore */ }
        }
    }
}
