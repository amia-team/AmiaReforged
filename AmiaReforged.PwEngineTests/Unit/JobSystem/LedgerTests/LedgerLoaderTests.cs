using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Nui.ViewModels;

namespace AmiaReforged.PwEngineTest.Unit.JobSystem.LedgerTests;

[TestFixture]
public class LedgerLoaderTests
{
    private ItemStorage _storage;

    [OneTimeSetUp]
    public void SetUp()
    {
        _storage = new ItemStorage
        {
            Id = 1,
            Items = new List<StoredJobItem>()
        };

        JobItem goodTestGem = new()
        {
            Id = 1,
            Name = "Test Gem",
            Material = MaterialEnum.GemGeneral,
            Quality = QualityEnum.Good,
            Type = ItemType.Gem,
            BaseValue = 1
        };
        JobItem badTestGem = new()
        {
            Id = 1,
            Name = "Test Gem",
            Material = MaterialEnum.GemGeneral,
            Quality = QualityEnum.Poor,
            Type = ItemType.Gem,
            BaseValue = 1
        };
        JobItem qualityCutWood = new()
        {
            Id = 1,
            Name = "Oak Wood Plank",
            Material = MaterialEnum.OakWood,
            Quality = QualityEnum.AboveAverage,
            Type = ItemType.Plank,
            BaseValue = 1
        };

        int storedItemIndex = 1;
        int jobItemIndex = 1;
        for (int i = 1; i <= 12; i++)
        {
            StoredJobItem storedItem = new()
            {
                Id = storedItemIndex,
                ItemStorage = _storage,
                ItemStorageId = 1,
                JobItemId = jobItemIndex,
                JobItem = new JobItem
                {
                    Id = jobItemIndex,
                    Material = qualityCutWood.Material,
                    Name = qualityCutWood.Name,
                    Quality = qualityCutWood.Quality,
                    Type = qualityCutWood.Type,
                    BaseValue = qualityCutWood.BaseValue
                }
            };
            _storage.Items.Add(storedItem);
            storedItemIndex++;
            jobItemIndex++;
        }

        for (int i = 1; i <= 12; i++)
        {
            StoredJobItem storedItem = new()
            {
                Id = storedItemIndex,
                ItemStorage = _storage,
                ItemStorageId = 1,
                JobItemId = jobItemIndex,
                JobItem = new JobItem
                {
                    Id = jobItemIndex,
                    Material = goodTestGem.Material,
                    Name = goodTestGem.Name,
                    Quality = goodTestGem.Quality,
                    Type = goodTestGem.Type,
                    BaseValue = goodTestGem.BaseValue
                }
            };
            StoredJobItem storedItem2 = new()
            {
                Id = storedItemIndex,
                ItemStorage = _storage,
                ItemStorageId = 1,
                JobItemId = jobItemIndex,
                JobItem = new JobItem
                {
                    Id = jobItemIndex,
                    Material = badTestGem.Material,
                    Name = badTestGem.Name,
                    Quality = badTestGem.Quality,
                    Type = badTestGem.Type,
                    BaseValue = badTestGem.BaseValue
                }
            };
            _storage.Items.Add(storedItem);
            _storage.Items.Add(storedItem2);
            storedItemIndex++;
            jobItemIndex++;
        }
    }

    [Test]
    public void ShouldMapFromItemStorageToLedger()
    {
        LedgerLoader loader = new();

        Ledger ledger = loader.FromItemStorage(_storage);
        
        Assert.Multiple(() =>
        {
            Assert.That(ledger.Entries[0].Quantity, Is.EqualTo(12));
            Assert.That(ledger.Entries[1].Quantity, Is.EqualTo(24));
        });
    }

    [Test]
    public void ShouldAverageItemQuality()
    {
        LedgerLoader ledgerLoader = new();

        Ledger ledger = ledgerLoader.FromItemStorage(_storage);

        Assert.That(ledger.Entries[1].AverageQuality, Is.EqualTo(QualityEnum.Average));
    }

    [Test]
    public void ShouldDisplayOverallQuality()
    {
        LedgerLoader ledgerLoader = new();

        Ledger ledger = ledgerLoader.FromItemStorage(_storage);

        Assert.That(ledger.Entries[1].ToolTip, Is.EqualTo("Overall Quality: Average"));
    }

    [Test]
    public void ShouldGiveOverallValue()
    {
        LedgerLoader ledgerLoader = new();

        Ledger ledger = ledgerLoader.FromItemStorage(_storage);

        int expectedValue = _storage.Items.Sum(c => c.JobItem.BaseValue);
        
        Assert.That(ledger.TotalValue, Is.EqualTo(expectedValue));
    }
    
    [Test]
    public void ShouldProvideListOfIndividualItems()
    {
        LedgerLoader ledgerLoader = new();

        Ledger ledger = ledgerLoader.FromItemStorage(_storage);
        Assert.Multiple(() =>
        {
            Assert.That(ledger.Entries[0].Items, Is.Not.Null);
            Assert.That(ledger.Entries[1].Items, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(ledger.Entries[0].Items, Has.Count.EqualTo(12));
            Assert.That(ledger.Entries[1].Items, Has.Count.EqualTo(24));
        });
    }
}