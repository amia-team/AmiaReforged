using AmiaReforged.Core;
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
public class MaterialInitializerTests
{
    private InMemoryDbContext _dbContext;
    private Repository<Material, int> _materialRepository;
    private MaterialInitializer _materialInitializer;
    private Mock<IResourceImporter<Material>> _mockMaterialImporter;
    private Mock<IRepositoryFactory> _mockRepositoryFactory;
    private readonly NwTaskHelper _helper = new();

    [SetUp]
    public void SetUp()
    {
        ScaffoldMockMaterialImporter();
        ScaffoldRepository();

        _materialInitializer =
            new MaterialInitializer(_mockMaterialImporter.Object, _mockRepositoryFactory.Object, _helper);
    }

    private void ScaffoldRepository()
    {
        DbContextOptions<InMemoryDbContext> options = new DbContextOptionsBuilder<InMemoryDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        _dbContext = new InMemoryDbContext(options);

        SeedContext();
        
        _materialRepository = new Repository<Material, int>(_dbContext);

        _mockRepositoryFactory = new Mock<IRepositoryFactory>();
        _mockRepositoryFactory.Setup(x => x.CreateRepository<Material, int>()).Returns(_materialRepository);
    }

    private void SeedContext()
    {
        List<Material> mockRepoMaterials = new()
        {
            new Material
            {
                Id = 1,
                Name = "TestMaterial",
                Type = MaterialType.Wood,
                ValueModifier = 1.0f,
                MagicModifier = 0.0f,
                DurabilityModifier = 1.0f
            },
            new Material
            {
                Id = 2,
                Name = "TestMaterial2",
                Type = MaterialType.Metal,
                ValueModifier = 1.0f,
                MagicModifier = 0.0f,
                DurabilityModifier = 1.0f
            }
        };

        _dbContext.Materials.AddRange(mockRepoMaterials);
        _dbContext.SaveChanges();
    }
    private void ScaffoldMockMaterialImporter()
    {
        List<Material> mockMats = new()
        {
            new Material
            {
                Name = "TestMaterial",
                Type = MaterialType.Wood,
                ValueModifier = 1.0f,
                MagicModifier = 0.1f,
                DurabilityModifier = 0.2f
            },
            new Material
            {
                Name = "TestMaterial2",
                Type = MaterialType.Metal,
                ValueModifier = 1.3f,
                MagicModifier = 0.0f,
                DurabilityModifier = 1.0f
            },
            new Material
            {
                Name = "TestMaterial3",
                Type = MaterialType.Metal,
                ValueModifier = 1.0f,
                MagicModifier = 0.0f,
                DurabilityModifier = 1.0f
            }
        };

        _mockMaterialImporter = new Mock<IResourceImporter<Material>>();

        _mockMaterialImporter.Setup(x => x.LoadResources()).Returns(mockMats);
    }

    [Test]
    public async Task Initialize_ShouldAddNewMaterials()
    {
        await _materialInitializer.Initialize();
        
        IEnumerable<Material?> changedMaterials = await _materialRepository.GetAll();

        IEnumerable<Material?> materials = changedMaterials as Material[] ?? changedMaterials.ToArray();
        
        Material newMat = materials.First(m => m?.Name == "TestMaterial3")!;
        
        Assert.That(materials.Count() == 3);
        
        Assert.That(newMat.Name == "TestMaterial3");
        Assert.That(newMat.Type == MaterialType.Metal);
        Assert.That(newMat.ValueModifier, Is.EqualTo(1.0f));
        Assert.That(newMat.MagicModifier, Is.EqualTo(0.0f));
    }


    [Test]
    public async Task Initialize_ShouldChangeUpdatedMaterials()
    {
        await _materialInitializer.Initialize();
        
        
        IEnumerable<Material?> changedMaterials = await _materialRepository.GetAll();
        
        IEnumerable<Material?> materials = changedMaterials as Material[] ?? changedMaterials.ToArray();
        
        Material updatedMat = materials.First(m => m?.Name == "TestMaterial")!;
        
        Assert.That(updatedMat.Name == "TestMaterial");
        Assert.That(updatedMat.Type == MaterialType.Wood);
        Assert.That(updatedMat.ValueModifier, Is.EqualTo(1.0f));
        Assert.That(updatedMat.MagicModifier, Is.EqualTo(0.1f));
        Assert.That(updatedMat.DurabilityModifier, Is.EqualTo(0.2f));
    }
    
    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}