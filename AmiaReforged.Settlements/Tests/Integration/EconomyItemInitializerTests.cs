using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models.Settlement;
using AmiaReforged.Core.Services;
using AmiaReforged.Settlements.Services.Economy.FileReaders;
using AmiaReforged.Settlements.Services.Economy.Initialization;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.Settlements.Tests.Integration;

[TestFixture]
public class EconomyItemInitializerTests
{

    private EconomyItemInitializer _initializer;
    private DbContext _dbContext;
    private Repository<EconomyItem, long> _itemRepository;
    private Mock<IResourceImporter<EconomyItem>> _mockImporter;
    private Mock<IRepositoryFactory> _mockRepositoryFactory;
    
    
    [SetUp]
    public void SetUp()
    {
        ScaffoldMockImporter();
        ScaffoldRepository();
        
        _initializer = new EconomyItemInitializer(_mockImporter.Object, _mockRepositoryFactory.Object, new NwTaskHelper());
    }

    private void ScaffoldRepository()
    {
        DbContextOptions<DbContext> options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
        
        _dbContext = new DbContext(options);
        SeedContext();
        
        _itemRepository = new Repository<EconomyItem, long>(_dbContext);
        
        _mockRepositoryFactory = new Mock<IRepositoryFactory>();
        
        _mockRepositoryFactory.Setup(x => x.CreateRepository<EconomyItem, long>()).Returns(_itemRepository);
        
    }

    private void SeedContext()
    {
        List<EconomyItem> mockRepoItems = new()
        {
            new EconomyItem
            {
                Id = 1,
                Name = "TestItem",
                BaseValue = 20,
                Quality = new Quality
                {
                    Id = 1,
                    Name = "TestQuality",
                    ValueModifier = 1.0f
                },
                Material = new Material
                {
                    Id = 1,
                    Name = "TestMaterial",
                    Type = MaterialType.Wood,
                    ValueModifier = 1.0f,
                    MagicModifier = 0.0f,
                    DurabilityModifier = 1.0f
                }
            }
        };
        
        _dbContext.AddRange(mockRepoItems);
        _dbContext.SaveChanges();
    }

    private void ScaffoldMockImporter()
    {
        _mockImporter = new Mock<IResourceImporter<EconomyItem>>();
        
        _mockImporter.Setup(x => x.LoadResources()).Returns(new List<EconomyItem>
        {
            new()
            {
                Name = "TestItem",
                BaseValue = 100,
                QualityId = 1,
                MaterialId = 1
            },
            new()
            {
                Name = "TestItem2",
                BaseValue = 100,
                QualityId = 1,
                MaterialId = 1
            }
        });
    }
    
    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}