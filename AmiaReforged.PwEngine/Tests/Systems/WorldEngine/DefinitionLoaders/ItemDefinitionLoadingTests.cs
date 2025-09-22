using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.DefinitionLoaders;

[TestFixture]
public class ItemDefinitionLoadingTests
{
    private string? _originalResourcePath;

    [SetUp]
    public void SetUp()
    {
        // Save original env var to restore later
        _originalResourcePath = Environment.GetEnvironmentVariable("RESOURCE_PATH");
    }

    [TearDown]
    public void TearDown()
    {
        // Restore original env var
        Environment.SetEnvironmentVariable("RESOURCE_PATH", _originalResourcePath);
    }

    [Test]
    public void Load_ReadsJsonFromTempFilesystem_AndCreatesDefinition()
    {
        // Arrange
        string tempRoot = Path.Combine(Path.GetTempPath(), "item-def-happy-" + Guid.NewGuid());
        string itemsDir = Path.Combine(tempRoot, "Items");
        Directory.CreateDirectory(itemsDir);

        string jsonPath = Path.Combine(itemsDir, "shortsword.json");
        // Minimal valid JSON for ItemDefinition
        // Materials: empty array (validation doesn't require them)
        // JobSystemType: numeric enum value for deserialization
        // BaseItemType: arbitrary example
        string json = """
                      {
                        "ResRef": "itm_shortswd",
                        "ItemTag": "TAG_SHORTSWD",
                        "Name": "Shortsword",
                        "Description": "A basic shortsword.",
                        "Materials": [],
                        "JobSystemType": 0,
                        "BaseItemType": 1,
                        "PartData": { "ModelType":0 },
                        "Appearance": { "ModelType":0 }
                      }
                      """;
        File.WriteAllText(jsonPath, json);

        string? previousEnv = Environment.GetEnvironmentVariable("RESOURCE_PATH");
        Environment.SetEnvironmentVariable("RESOURCE_PATH", tempRoot);

        InMemoryItemDefinitionRepository repo = new();
        ItemDefinitionLoadingService service = new(repo);

        try
        {
            // Act
            service.Load();

            // Assert
            List<FileLoadResult> failures = service.Failures();
            Assert.That(failures, Is.Empty, "No failures expected for valid item definition.");

            // Optional: verify what was loaded via service.Definitions()
            // (relies on service exposing successfully loaded definitions)
            List<ItemDefinition> defs = service.Definitions();
            Assert.That(defs.Count, Is.EqualTo(1));
            Assert.That(defs[0].ResRef, Is.EqualTo("itm_shortswd"));
            Assert.That(defs[0].ItemTag, Is.EqualTo("TAG_SHORTSWD"));
            Assert.That(defs[0].Name, Is.EqualTo("Shortsword"));
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("RESOURCE_PATH", previousEnv);
            try
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
            catch
            {
                // ignore cleanup errors in tests
            }
        }
    }

    [Test]
    public void Load_InvalidDefinitions_AreRejected_WithFailures()
    {
        // Arrange
        string tempRoot =
            Path.Combine(Path.GetTempPath(), "item-def-loader-tests-" + Guid.NewGuid());
        string itemsDir = Path.Combine(tempRoot, "Items");
        Directory.CreateDirectory(itemsDir);

        // Invalid JSON file (deserialization should fail)
        string invalidJsonPath = Path.Combine(itemsDir, "invalid.json");
        File.WriteAllText(invalidJsonPath, "{not-json}");

        // Valid JSON syntax but invalid definition semantically (validation should fail)
        // Using an empty object to ensure it deserializes but fails TryValidate
        string badItemPath = Path.Combine(itemsDir, "bad-item.json");
        File.WriteAllText(badItemPath, "{}");

        // Point the loader to our temp root via the expected environment variable
        // Replace ITEMS_PATH with the exact variable name used by your loader if it differs.
        string envVarName = "RESOURCE_PATH";
        string? previousEnv = Environment.GetEnvironmentVariable(envVarName);
        Environment.SetEnvironmentVariable(envVarName, tempRoot);

        InMemoryItemDefinitionRepository repo = new();
        ItemDefinitionLoadingService service = new(repo);

        try
        {
            // Act
            service.Load();
            List<FileLoadResult> failures = service.Failures();

            // Assert
            Assert.That(failures, Is.Not.Null);
            Assert.That(failures.Count, Is.EqualTo(2),
                "Expected one failure for invalid JSON and one for validation failure.");
            Assert.That(
                failures.Any(f => string.Equals(f.FileName, "invalid.json", StringComparison.OrdinalIgnoreCase)),
                Is.True,
                "Failures should include invalid.json");
            Assert.That(
                failures.Any(f => string.Equals(f.FileName, "bad-item.json", StringComparison.OrdinalIgnoreCase)),
                Is.True,
                "Failures should include bad-item.json");
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(envVarName, previousEnv);
            try
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
