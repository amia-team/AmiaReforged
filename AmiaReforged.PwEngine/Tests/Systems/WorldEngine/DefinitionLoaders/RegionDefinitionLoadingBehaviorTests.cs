using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.DefinitionLoaders;

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
            {"Tag":"r1","Name":"Region One","Settlements":[100],"Areas":[{"ResRef":"a1","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}}}]}
            """);
            File.WriteAllText(Path.Combine(dir, "r2.json"), """
            {"Tag":"r2","Name":"Region Two","Settlements":[100],"Areas":[{"ResRef":"a2","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}}}]}
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", root.FullName);
            InMemoryRegionRepository repo = new();
            RegionDefinitionLoadingService loader = new(repo);

            loader.Load();
            var failures = loader.Failures();

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
    public void Intra_File_Duplicate_Settlements_Fail_And_Adds_No_Regions()
    {
        DirectoryInfo root = Directory.CreateTempSubdirectory("regions-dup-intra");
        try
        {
            string dir = Path.Combine(root.FullName, "Regions");
            Directory.CreateDirectory(dir);

            File.WriteAllText(Path.Combine(dir, "r1.json"), """
            {"Tag":"r1","Name":"Region One","Settlements":[200,200],"Areas":[{"ResRef":"a1","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}}}]}
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", root.FullName);
            InMemoryRegionRepository repo = new();
            RegionDefinitionLoadingService loader = new(repo);

            loader.Load();
            var failures = loader.Failures();

            Assert.That(failures, Is.Not.Empty);
            Assert.That(failures[0].Message, Does.Contain("Duplicate settlement IDs within the same region definition"));
            Assert.That(repo.All(), Is.Empty);
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
            {"Tag":"r1","Name":"Region One","Settlements":[0,-1],"Areas":[{"ResRef":"a1","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}}}]}
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
            {"Tag":"r","Name":"Region R1","Settlements":[311],"Areas":[{"ResRef":"a1","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}}}]}
            """);
            File.WriteAllText(Path.Combine(dir, "r2.json"), """
            {"Tag":"r","Name":"Region R2","Settlements":[312],"Areas":[{"ResRef":"a2","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}}}]}
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", root.FullName);
            InMemoryRegionRepository repo = new();
            RegionDefinitionLoadingService loader = new(repo);

            loader.Load();

            var failures = loader.Failures();
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
            {"Tag":"r1","Name":"Region One","Settlements":[900],"Areas":[{"ResRef":"a1","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}}}]}
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", root.FullName);
            InMemoryRegionRepository repo = new();
            RegionDefinitionLoadingService loader = new(repo);

            loader.Load();
            Assert.That(repo.All().Count, Is.EqualTo(1));
            Assert.That(repo.TryGetRegionBySettlement(900, out var _), Is.True);

            // Change file to different settlement and reload
            File.WriteAllText(file, """
            {"Tag":"r1","Name":"Region One","Settlements":[901],"Areas":[{"ResRef":"a1","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}}}]}
            """);

            loader.Load();
            Assert.That(repo.All().Count, Is.EqualTo(1));
            Assert.That(repo.TryGetRegionBySettlement(900, out var _), Is.False);
            Assert.That(repo.TryGetRegionBySettlement(901, out var _), Is.True);
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
            {"Tag":"r1","Name":"Region One","Settlements":[1000,1001],"Areas":[{"ResRef":"a1","DefinitionTags":[],"Environment":{"Climate":"Temperate","SoilQuality":"Average","MineralQualityRange":{"Min":"Average","Max":"Average"}}}]}
            """);

            Environment.SetEnvironmentVariable("RESOURCE_PATH", root.FullName);
            InMemoryRegionRepository repo = new();
            RegionDefinitionLoadingService loader = new(repo);

            loader.Load();
            var first = repo.All().Single();
            CollectionAssert.AreEquivalent(new[]{1000,1001}, first.Settlements);

            loader.Load();
            var second = repo.All().Single();
            CollectionAssert.AreEquivalent(new[]{1000,1001}, second.Settlements);
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
        var result = repo.GetSettlements("missing-tag");
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }
}

