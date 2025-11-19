using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.DefinitionLoaders;

public class ItemBlueprintLoadingServiceTests
{
    [Test]
    public void LoadsBlueprints_WhenDirectoryExists()
    {
        // Arrange
        string tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(tempRoot, "Items", "Blueprints"));
        string filePath = Path.Combine(tempRoot, "Items", "Blueprints", "sample.json");
        File.WriteAllText(filePath, "{\n  \"ResRef\": \"btest01\",\n  \"ItemTag\": \"blueprint_test_item\",\n  \"Name\": \"Test Item\",\n  \"Description\": \"Desc\",\n  \"Materials\": [\"Gem\"],\n  \"JobSystemType\": \"ResourceOre\",\n  \"BaseItemType\": 74,\n  \"Appearance\": { \"ModelType\": 0, \"SimpleModelNumber\": 1 },\n  \"BaseValue\": 5,\n  \"WeightIncreaseConstant\": -1\n}\n");

        Environment.SetEnvironmentVariable("RESOURCE_PATH", tempRoot);
        InMemoryItemDefinitionRepository repo = new();
        ItemBlueprintLoadingService loader = new(repo);

        // Act
        loader.Load();

        // Assert
        Assert.That(loader.Failures(), Is.Empty, "Expected no failures loading valid blueprint.");
        Assert.That(repo.GetByTag("blueprint_test_item"), Is.Not.Null, "Blueprint should be added to repository.");
    }

    [Test]
    public void DoesNotFail_WhenBlueprintDirectoryMissing()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot); // no Items/Blueprints subfolder
        Environment.SetEnvironmentVariable("RESOURCE_PATH", tempRoot);
        InMemoryItemDefinitionRepository repo = new();
        ItemBlueprintLoadingService loader = new(repo);
        loader.Load();
        Assert.That(loader.Failures().Count, Is.EqualTo(0));
    }

    [Test]
    public void Fails_OnInvalidResRefLength()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string bpDir = Path.Combine(tempRoot, "Items", "Blueprints");
        Directory.CreateDirectory(bpDir);
        string invalidJson = "{\n  \"ResRef\": \"thisresrefiswaytoolongfornwn\",\n  \"ItemTag\": \"bp_invalid\",\n  \"Name\": \"Invalid\",\n  \"Description\": \"Desc\",\n  \"Materials\": [\"Gem\"],\n  \"JobSystemType\": \"ResourceOre\",\n  \"BaseItemType\": 74,\n  \"Appearance\": { \"ModelType\": 0, \"SimpleModelNumber\": 1 },\n  \"BaseValue\": 5,\n  \"WeightIncreaseConstant\": -1\n}\n";
        File.WriteAllText(Path.Combine(bpDir, "invalid.json"), invalidJson);
        Environment.SetEnvironmentVariable("RESOURCE_PATH", tempRoot);
        InMemoryItemDefinitionRepository repo = new();
        ItemBlueprintLoadingService loader = new(repo);
        loader.Load();
        Assert.That(loader.Failures().Count, Is.EqualTo(1));
        Assert.That(repo.GetByTag("bp_invalid"), Is.Null);
    }
}
