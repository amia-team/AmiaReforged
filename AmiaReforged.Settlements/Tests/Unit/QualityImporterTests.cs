using System.IO.Abstractions.TestingHelpers;
using AmiaReforged.Core.Models.Settlement;
using AmiaReforged.Settlements.Services.Economy.FileReaders;
using NUnit.Framework;

namespace AmiaReforged.Settlements.Tests.Unit;

[TestFixture]
public class QualityImporterTests
{
    private List<MockFileData> _mockFileData;
    private MockFileSystem _fileSystem;
    private QualityImporter _qualityImporter;

    [SetUp]
    public void SetUp()
    {
        _fileSystem = new MockFileSystem();

        const string fakeYaml1 = """
                                 name: TestQuality
                                 valueModifier: 1.0
                                 """;
        const string fakeYaml2 = """
                                 name: TestQuality2
                                 valueModifier: 2.0   
                                 """;
        const string badYaml = """
                               nnemn = TestQuality2
                               ValueModifier = 2.0
                               """;

        string absolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Anvil/PluginData/AmiaReforged.Settlements/ItemQuality/test_quality1.yaml");
        string absolutePath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Anvil/PluginData/AmiaReforged.Settlements/ItemQuality/test_quality2.yaml");
        string absolutePath3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Anvil/PluginData/AmiaReforged.Settlements/ItemQuality/test_quality3.yaml");

        _mockFileData = new List<MockFileData>
        {
            new(fakeYaml1),
            new(fakeYaml2),
            new(badYaml)
        };

        _fileSystem.AddFile(absolutePath, _mockFileData[0]);
        _fileSystem.AddFile(absolutePath2, _mockFileData[1]);
        _fileSystem.AddFile(absolutePath3, _mockFileData[2]);

        _qualityImporter = new QualityImporter(_fileSystem);
    }

    [Test]
    public void LoadResources_ShouldReturnQualities()
    {
        // Arrange
        Quality expectedQuality = new Quality
        {
            Name = "TestQuality",
            ValueModifier = 1.0f
        };

        Quality expectedQuality2 = new Quality
        {
            Name = "TestQuality2",
            ValueModifier = 2.0f
        };

        // Act
        IEnumerable<Quality> result = _qualityImporter.LoadResources();

        // Assert
        IEnumerable<Quality> enumerable = result as Quality[] ?? result.ToArray();
        Assert.That(enumerable, Is.Not.Null);
        Assert.That(enumerable, Is.Not.Empty);
        Assert.That(enumerable, Has.Exactly(2).Items);

        Quality firstQuality = enumerable.First();
        Assert.That(firstQuality.Name, Is.EqualTo(expectedQuality.Name));
        Assert.That(firstQuality.ValueModifier, Is.EqualTo(expectedQuality.ValueModifier));

        Quality secondQuality = enumerable.ElementAt(1);
        Assert.That(secondQuality.Name, Is.EqualTo(expectedQuality2.Name));
        Assert.That(secondQuality.ValueModifier, Is.EqualTo(expectedQuality2.ValueModifier));
    }
}