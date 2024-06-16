using System.IO.Abstractions.TestingHelpers;
using AmiaReforged.Core.Models.Settlement;
using AmiaReforged.Settlements.Services.Economy.FileReaders;
using NUnit.Framework;

namespace AmiaReforged.Settlements.Tests.Unit;

[TestFixture]
public class MaterialImporterTests
{
    private List<MockFileData> _mockFileData;
    private MockFileSystem _mockFileSystem;
    private MaterialImporter _materialImporter;

    [SetUp]
    public void SetUp()
    {
        _mockFileSystem = new MockFileSystem();
        const string fakeYaml1 = """
                                 name: TestMaterial
                                 type: Wood
                                 valueModifier: 1.0
                                 magicModifier: 0.0
                                 durabilityModifier: 1.0
                                 """;
        const string fakeYaml2 = """
                                 name: TestMaterial2
                                 type: Metal
                                 valueModifier: 1.0
                                 magicModifier: 0.0
                                 durabilityModifier: 1.0
                                 """;
        const string badYaml = """
                               florp: TestMaterial2
                               flep: Metal
                               fleep: 1.0
                               flap: 0.0
                               flup: 1.0
                               """;


        string absolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Anvil/PluginData/AmiaReforged.Settlements/Materials/test_material1.yaml");
        string absolutePath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Anvil/PluginData/AmiaReforged.Settlements/Materials/test_material2.yaml");
        string absolutePath3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Anvil/PluginData/AmiaReforged.Settlements/Materials/test_material3.yaml");

        _mockFileData = new List<MockFileData>
        {
            new(fakeYaml1),
            new(fakeYaml2),
            new(badYaml)
        };

        _mockFileSystem.AddFile(absolutePath, _mockFileData[0]);
        _mockFileSystem.AddFile(absolutePath2, _mockFileData[1]);
        _mockFileSystem.AddFile(absolutePath3, _mockFileData[2]);

        _materialImporter = new MaterialImporter(_mockFileSystem);
    }

    [Test]
    public void LoadResources_ShouldReturnMaterials()
    {
        // Arrange
        Material expectedMaterial = new()
        {
            Name = "TestMaterial",
            Type = MaterialType.Wood,
            ValueModifier = 1.0f,
            MagicModifier = 0.0f,
            DurabilityModifier = 1.0f
        };

        // Act
        IEnumerable<Material> result = _materialImporter.LoadResources();

        // Assert
        IEnumerable<Material> enumerable = result as Material[] ?? result.ToArray();
        Assert.That(enumerable, Is.Not.Null);
        Assert.That(enumerable, Has.Exactly(2).Items);

        Material firstMaterial = enumerable.First();
        Assert.That(firstMaterial.Name, Is.EqualTo(expectedMaterial.Name));
        Assert.That(firstMaterial.Type, Is.EqualTo(expectedMaterial.Type));
        Assert.That(firstMaterial.ValueModifier, Is.EqualTo(expectedMaterial.ValueModifier));
        Assert.That(firstMaterial.MagicModifier, Is.EqualTo(expectedMaterial.MagicModifier));
        Assert.That(firstMaterial.DurabilityModifier, Is.EqualTo(expectedMaterial.DurabilityModifier));

        Material secondMaterial = enumerable.ElementAt(1);
        Assert.That(secondMaterial.Name, Is.EqualTo("TestMaterial2"));
        Assert.That(secondMaterial.Type, Is.EqualTo(MaterialType.Metal));
        Assert.That(secondMaterial.ValueModifier, Is.EqualTo(1.0f));
        Assert.That(secondMaterial.MagicModifier, Is.EqualTo(0.0f));
        Assert.That(secondMaterial.DurabilityModifier, Is.EqualTo(1.0f));
    }
}