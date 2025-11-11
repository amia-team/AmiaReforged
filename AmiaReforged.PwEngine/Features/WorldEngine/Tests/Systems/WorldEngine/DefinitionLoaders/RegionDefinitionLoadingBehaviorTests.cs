using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Tests.Systems.WorldEngine.DefinitionLoaders;

[TestFixture]
public class RegionDefinitionLoadingBehaviorTests
{
    private string? _originalResourcePath;

    [SetUp]
    public void SetUp() => _originalResourcePath = Environment.GetEnvironmentVariable("RESOURCE_PATH");

    [TearDown]
    public void TearDown() => Environment.SetEnvironmentVariable("RESOURCE_PATH", _originalResourcePath);

    [Test]
    public void Duplicate_Settlement_Across_Files_Fails_And_Indicates_Conflict()
    {
        DirectoryInfo root = Directory.CreateTempSubdirectory("regions-dup-cross");
        try
        {
            string dir = Path.Combine(root.FullName, "Regions");
            Directory.CreateDirectory(dir);

            File.WriteAllText(Path.Combine(dir, "r1.json"), """
            {"Tag":"r1","Name":"Region One","Areas":[{"ResRef":"a1","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}},"LinkedSettlement":100}]}
            """);
            File.WriteAllText(Path.Combine(dir, "r2.json"), """
            {"Tag":"r2","Name":"Region Two","Areas":[{"ResRef":"a2","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}},"LinkedSettlement":100}]}
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", root.FullName);
            InMemoryRegionRepository repo = new();
            RegionDefinitionLoadingService loader = new(repo);

            loader.Load();
            List<FileLoadResult> failures = loader.Failures();

            Assert.That(failures, Is.Not.Empty);
            Assert.That(failures.Any(f => f.FileName == "r2.json" && (f.Message?.Contains("Duplicate settlement IDs across regions", StringComparison.OrdinalIgnoreCase) ?? false)), Is.True);
            Assert.That(repo.All().Count, Is.EqualTo(1));
        }
        finally
        {
            try { Directory.Delete(root.FullName, true); } catch { }
        }
    }

    [Test]
    public void Intra_File_Duplicate_Settlements_AreAllowed()
    {
        DirectoryInfo root = Directory.CreateTempSubdirectory("regions-dup-intra");
        try
        {
            string dir = Path.Combine(root.FullName, "Regions");
            Directory.CreateDirectory(dir);

            File.WriteAllText(Path.Combine(dir, "r1.json"), """
            {"Tag":"r1","Name":"Region One","Areas":[
                {"ResRef":"a1","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}},"LinkedSettlement":200},
                {"ResRef":"a2","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}},"LinkedSettlement":200}
            ]}
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", root.FullName);
            InMemoryRegionRepository repo = new();
            RegionDefinitionLoadingService loader = new(repo);

            loader.Load();
            List<FileLoadResult> failures = loader.Failures();

            Assert.That(failures, Is.Empty);
            IReadOnlyCollection<SettlementId> settlements = repo.GetSettlements(new RegionTag("r1"));
            CollectionAssert.AreEquivalent(new[]{200}, settlements.Select(s => s.Value));
        }
        finally
        {
            try { Directory.Delete(root.FullName, true); } catch { }
        }
    }

    [Test]
    public void Non_Positive_SettlementIds_Fail()
    {
        DirectoryInfo root = Directory.CreateTempSubdirectory("regions-bad-ids");
        try
        {
            string dir = Path.Combine(root.FullName, "Regions");
            Directory.CreateDirectory(dir);

            File.WriteAllText(Path.Combine(dir, "r1.json"), """
            {"Tag":"r1","Name":"Region One","Areas":[
                {"ResRef":"a1","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}},"LinkedSettlement":0},
                {"ResRef":"a2","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}},"LinkedSettlement":-1}
            ]}
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", root.FullName);
            InMemoryRegionRepository repo = new();
            RegionDefinitionLoadingService loader = new(repo);

            loader.Load();

            Assert.That(loader.Failures(), Is.Not.Empty);
            Assert.That(loader.Failures()[0].Message, Does.Contain("Settlement IDs must be positive integers"));
            Assert.That(repo.All(), Is.Empty);
        }
        finally
        {
            try { Directory.Delete(root.FullName, true); } catch { }
        }
    }

    [Test]
    public void Duplicate_Region_Tags_Fail()
    {
        DirectoryInfo root = Directory.CreateTempSubdirectory("regions-dup-tags");
        try
        {
            string dir = Path.Combine(root.FullName, "Regions");
            Directory.CreateDirectory(dir);

            File.WriteAllText(Path.Combine(dir, "r1.json"), """
            {"Tag":"r","Name":"Region R1","Areas":[{"ResRef":"a1","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}},"LinkedSettlement":311}]}
            """);
            File.WriteAllText(Path.Combine(dir, "r2.json"), """
            {"Tag":"r","Name":"Region R2","Areas":[{"ResRef":"a2","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}},"LinkedSettlement":312}]}
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", root.FullName);
            InMemoryRegionRepository repo = new();
            RegionDefinitionLoadingService loader = new(repo);

            loader.Load();

            List<FileLoadResult> failures = loader.Failures();
            Assert.That(failures, Is.Not.Empty);
            Assert.That(failures.Any(f => f.Message?.Contains("Duplicate region tag", StringComparison.OrdinalIgnoreCase) ?? false), Is.True);
            Assert.That(repo.All().Count, Is.EqualTo(1));
        }
        finally
        {
            try { Directory.Delete(root.FullName, true); } catch { }
        }
    }

    [Test]
    public void Reload_Clears_And_Rebuilds_State()
    {
        DirectoryInfo root = Directory.CreateTempSubdirectory("regions-reload");
        try
        {
            string dir = Path.Combine(root.FullName, "Regions");
            Directory.CreateDirectory(dir);

            string file = Path.Combine(dir, "r1.json");
            File.WriteAllText(file, """
            {"Tag":"r1","Name":"Region One","Areas":[{"ResRef":"a1","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}},"LinkedSettlement":900}]}
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", root.FullName);
            InMemoryRegionRepository repo = new();
            RegionDefinitionLoadingService loader = new(repo);

            loader.Load();
            Assert.That(repo.All().Count, Is.EqualTo(1));
            Assert.That(repo.TryGetRegionBySettlement(SettlementId.Parse(900), out RegionDefinition? _), Is.True);

            // Change file to different settlement and reload
            File.WriteAllText(file, """
            {"Tag":"r1","Name":"Region One","Areas":[{"ResRef":"a1","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}},"LinkedSettlement":901}]}
            """);

            loader.Load();
            Assert.That(repo.All().Count, Is.EqualTo(1));
            Assert.That(repo.TryGetRegionBySettlement(SettlementId.Parse(900), out RegionDefinition? _), Is.False);
            Assert.That(repo.TryGetRegionBySettlement(SettlementId.Parse(901), out RegionDefinition? _), Is.True);
        }
        finally
        {
            try { Directory.Delete(root.FullName, true); } catch { }
        }
    }

    [Test]
    public void Idempotent_Reload_Does_Not_Duplicate()
    {
        DirectoryInfo root = Directory.CreateTempSubdirectory("regions-idempotent");
        try
        {
            string dir = Path.Combine(root.FullName, "Regions");
            Directory.CreateDirectory(dir);

            File.WriteAllText(Path.Combine(dir, "r1.json"), """
            {"Tag":"r1","Name":"Region One","Areas":[
                {"ResRef":"a1","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}},"LinkedSettlement":1000},
                {"ResRef":"a2","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}},"LinkedSettlement":1001}
            ]}
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", root.FullName);
            InMemoryRegionRepository repo = new();
            RegionDefinitionLoadingService loader = new(repo);

            loader.Load();
            RegionDefinition first = repo.All().Single();
            IReadOnlyCollection<SettlementId> initialSettlements = repo.GetSettlements(new RegionTag("r1"));
            CollectionAssert.AreEquivalent(new[]{1000,1001}, initialSettlements.Select(s => s.Value).ToArray());

            loader.Load();
            RegionDefinition second = repo.All().Single();
            IReadOnlyCollection<SettlementId> reloadedSettlements = repo.GetSettlements(new RegionTag("r1"));
            CollectionAssert.AreEquivalent(new[]{1000,1001}, reloadedSettlements.Select(s => s.Value).ToArray());
        }
        finally
        {
            try { Directory.Delete(root.FullName, true); } catch { }
        }
    }

    [Test]
    public void Unknown_Region_Tag_Query_Returns_Empty()
    {
        InMemoryRegionRepository repo = new();
        IReadOnlyCollection<SettlementId> result = repo.GetSettlements(new RegionTag("missing-tag"));
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }
}

