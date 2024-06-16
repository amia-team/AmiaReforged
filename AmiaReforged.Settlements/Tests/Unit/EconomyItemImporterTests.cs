using System.IO.Abstractions.TestingHelpers;
using AmiaReforged.Core.Models.Settlement;
using AmiaReforged.Settlements.Services.Economy.FileReaders;
using NUnit.Framework;

namespace AmiaReforged.Settlements.Tests.Unit;

[TestFixture]
public class EconomyItemImporterTests
{
    private MockFileSystem _mockFileSystem;
    private MockFileData _mockFileData;
    private MockFileData _mockFileData2;
    private EconomyItemImporter _economyItemImporter;

    [SetUp]
    public void SetUp()
    {
        _mockFileSystem = new MockFileSystem();
        const string fakeYaml = """
                                name: Test
                                materialId: 1
                                qualityId: 1
                                baseValue: 10
                                """;
        const string fakeYaml2 = """
                                 name: Test2
                                 materialId: 2
                                 qualityId: 0
                                 baseValue: 10
                                 """;
        const string badYaml = """
                                 nameRerer ---- Test2
                                 materialId: 2
                                 qualityId: 0
                                 baseValue: 10
                                 """;
        _mockFileData = new MockFileData(fakeYaml);
        _mockFileData2 = new MockFileData(fakeYaml2);

        // Use an absolute path when adding the file to the MockFileSystem
        string absolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Anvil/PluginData/AmiaReforged.Settlements/Items/test.yaml");
        string absolutePath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Anvil/PluginData/AmiaReforged.Settlements/Items/test2.yaml");
        string absolutePath3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "Anvil/PluginData/AmiaReforged.Settlements/Items/test3.yaml");
        
        _mockFileSystem.AddFile(absolutePath, _mockFileData);
        _mockFileSystem.AddFile(absolutePath2, _mockFileData2);
        _mockFileSystem.AddFile(absolutePath3, new MockFileData(badYaml));
        
        _economyItemImporter = new EconomyItemImporter(_mockFileSystem);
    }

    [Test]
    public void LoadResources_ShouldReturnEconomyItems()
    {
        // Arrange
        EconomyItem expectedEconomyItem = new()
        {
            Name = "Test",
            MaterialId = 1,
            QualityId = 1,
            BaseValue = 10,
        };
        // Act
        IEnumerable<EconomyItem> result = _economyItemImporter.LoadResources();

        // Assert
        IEnumerable<EconomyItem> economyItems = result as EconomyItem[] ?? result.ToArray();
        Assert.That(economyItems, Is.Not.Empty);

        EconomyItem firstItem = economyItems.First();
        Assert.That(firstItem.Name, Is.EqualTo(expectedEconomyItem.Name));
        Assert.That(firstItem.MaterialId, Is.EqualTo(expectedEconomyItem.MaterialId));
        Assert.That(firstItem.QualityId, Is.EqualTo(expectedEconomyItem.QualityId));
        Assert.That(firstItem.BaseValue, Is.EqualTo(expectedEconomyItem.BaseValue));
    }

    [Test]
    public void LoadResources_ShouldLoadMultipleFiles()
    {
        IEnumerable<EconomyItem> result = _economyItemImporter.LoadResources();
            
        IEnumerable<EconomyItem> economyItems = result as EconomyItem[] ?? result.ToArray();
            
        Assert.That(economyItems.ToList(), Has.Count.EqualTo(2));
    }
}