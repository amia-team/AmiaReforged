using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;

[TestFixture]
public class WorkstationRecipeQueryTests
{
    private TestWorkstationRepository _workstationRepository = null!;
    private InMemoryIndustryRepository _industryRepository = null!;
    private TestIndustryMembershipRepository _membershipRepository = null!;
    private TestCharacterKnowledgeRepository _knowledgeRepository = null!;
    private GetWorkstationRecipesHandler _handler = null!;

    private CharacterId _testCharacterId;
    private Knowledge _basicForging = null!;
    private Knowledge _advancedForging = null!;
    private Knowledge _basicAlchemy = null!;

    private Recipe _ironSword = null!;      // Forge, blacksmithing, novice
    private Recipe _steelSword = null!;     // Forge, blacksmithing, apprentice (needs advanced_forging)
    private Recipe _portableRecipe = null!; // No workstation, blacksmithing
    private Recipe _healthPotion = null!;   // Alchemy table, alchemy, novice

    [SetUp]
    public void SetUp()
    {
        _workstationRepository = new TestWorkstationRepository();
        _industryRepository = new InMemoryIndustryRepository();
        _membershipRepository = new TestIndustryMembershipRepository();
        _knowledgeRepository = new TestCharacterKnowledgeRepository();
        _handler = new GetWorkstationRecipesHandler(
            _workstationRepository,
            _industryRepository,
            _membershipRepository,
            _knowledgeRepository,
            new RecipeTemplateExpander(
                new EmptyRecipeTemplateRepository(),
                new EmptyItemDefinitionRepository()));

        _testCharacterId = new CharacterId(Guid.NewGuid());

        // Knowledge definitions
        _basicForging = new Knowledge
        {
            Tag = "basic_forging", Name = "Basic Forging",
            Description = "Basic forging techniques",
            Level = ProficiencyLevel.Novice, PointCost = 1
        };
        _advancedForging = new Knowledge
        {
            Tag = "advanced_forging", Name = "Advanced Forging",
            Description = "Advanced forging techniques",
            Level = ProficiencyLevel.Apprentice, PointCost = 2
        };
        _basicAlchemy = new Knowledge
        {
            Tag = "basic_alchemy", Name = "Basic Alchemy",
            Description = "Basic alchemy techniques",
            Level = ProficiencyLevel.Novice, PointCost = 1
        };

        // Recipes
        _ironSword = new Recipe
        {
            RecipeId = new RecipeId("iron_sword"), Name = "Iron Sword",
            IndustryTag = new IndustryTag("blacksmithing"),
            RequiredKnowledge = ["basic_forging"],
            RequiredProficiency = ProficiencyLevel.Novice,
            RequiredWorkstation = new WorkstationTag("forge"),
            Ingredients = [new Ingredient { ItemTag = "iron_ingot", Quantity = Quantity.Parse(3) }],
            Products = [new Product { ItemTag = "iron_sword", Quantity = Quantity.Parse(1) }]
        };

        _steelSword = new Recipe
        {
            RecipeId = new RecipeId("steel_sword"), Name = "Steel Sword",
            IndustryTag = new IndustryTag("blacksmithing"),
            RequiredKnowledge = ["basic_forging", "advanced_forging"],
            RequiredProficiency = ProficiencyLevel.Apprentice,
            RequiredWorkstation = new WorkstationTag("forge"),
            Ingredients = [new Ingredient { ItemTag = "steel_ingot", Quantity = Quantity.Parse(3) }],
            Products = [new Product { ItemTag = "steel_sword", Quantity = Quantity.Parse(1) }]
        };

        _portableRecipe = new Recipe
        {
            RecipeId = new RecipeId("bandage"), Name = "Bandage",
            IndustryTag = new IndustryTag("blacksmithing"),
            RequiredKnowledge = [],
            RequiredProficiency = ProficiencyLevel.Layman,
            RequiredWorkstation = null, // portable — no workstation
            Ingredients = [new Ingredient { ItemTag = "cloth", Quantity = Quantity.Parse(1) }],
            Products = [new Product { ItemTag = "bandage", Quantity = Quantity.Parse(1) }]
        };

        _healthPotion = new Recipe
        {
            RecipeId = new RecipeId("health_potion"), Name = "Health Potion",
            IndustryTag = new IndustryTag("alchemy"),
            RequiredKnowledge = ["basic_alchemy"],
            RequiredProficiency = ProficiencyLevel.Novice,
            RequiredWorkstation = new WorkstationTag("alchemy_table"),
            Ingredients = [new Ingredient { ItemTag = "herb", Quantity = Quantity.Parse(2) }],
            Products = [new Product { ItemTag = "health_potion", Quantity = Quantity.Parse(1) }]
        };

        // Industries
        Industry blacksmithing = new Industry
        {
            Tag = "blacksmithing", Name = "Blacksmithing",
            Knowledge = [_basicForging, _advancedForging],
            Recipes = [_ironSword, _steelSword, _portableRecipe]
        };
        Industry alchemy = new Industry
        {
            Tag = "alchemy", Name = "Alchemy",
            Knowledge = [_basicAlchemy],
            Recipes = [_healthPotion]
        };
        _industryRepository.Add(blacksmithing);
        _industryRepository.Add(alchemy);

        // Workstations
        _workstationRepository.Add(new Workstation
        {
            Tag = new WorkstationTag("forge"),
            Name = "Forge",
            SupportedIndustries = [new IndustryTag("blacksmithing")]
        });
        _workstationRepository.Add(new Workstation
        {
            Tag = new WorkstationTag("alchemy_table"),
            Name = "Alchemy Table",
            SupportedIndustries = [new IndustryTag("alchemy")]
        });
    }

    [Test]
    public async Task WorkstationNotFound_ReturnsEmpty()
    {
        GetWorkstationRecipesQuery query = new GetWorkstationRecipesQuery
        {
            CharacterId = _testCharacterId,
            WorkstationTag = new WorkstationTag("nonexistent")
        };

        List<Recipe> result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task CharacterNotEnrolled_ReturnsEmpty()
    {
        // No memberships added
        GetWorkstationRecipesQuery query = new GetWorkstationRecipesQuery
        {
            CharacterId = _testCharacterId,
            WorkstationTag = new WorkstationTag("forge")
        };

        List<Recipe> result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task NoviceWithKnowledge_ReturnsOnlyNoviceForgeRecipes()
    {
        // Enroll as novice blacksmith
        _membershipRepository.Add(new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing"),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = []
        });
        _knowledgeRepository.AddKnowledge(_testCharacterId.Value, _basicForging);

        GetWorkstationRecipesQuery query = new GetWorkstationRecipesQuery
        {
            CharacterId = _testCharacterId,
            WorkstationTag = new WorkstationTag("forge")
        };

        List<Recipe> result = await _handler.HandleAsync(query);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].RecipeId.Value, Is.EqualTo("iron_sword"));
    }

    [Test]
    public async Task ApprenticeWithAllKnowledge_ReturnsBothForgeRecipes()
    {
        _membershipRepository.Add(new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing"),
            Level = ProficiencyLevel.Apprentice,
            CharacterKnowledge = []
        });
        _knowledgeRepository.AddKnowledge(_testCharacterId.Value, _basicForging);
        _knowledgeRepository.AddKnowledge(_testCharacterId.Value, _advancedForging);

        GetWorkstationRecipesQuery query = new GetWorkstationRecipesQuery
        {
            CharacterId = _testCharacterId,
            WorkstationTag = new WorkstationTag("forge")
        };

        List<Recipe> result = await _handler.HandleAsync(query);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(r => r.RecipeId.Value), Is.EquivalentTo(new[] { "iron_sword", "steel_sword" }));
    }

    [Test]
    public async Task PortableRecipes_DoNotAppearAtWorkstation()
    {
        // Even if the character is enrolled, portable recipes (RequiredWorkstation=null) 
        // should NOT appear in the workstation UI
        _membershipRepository.Add(new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing"),
            Level = ProficiencyLevel.Grandmaster,
            CharacterKnowledge = []
        });
        _knowledgeRepository.AddKnowledge(_testCharacterId.Value, _basicForging);
        _knowledgeRepository.AddKnowledge(_testCharacterId.Value, _advancedForging);

        GetWorkstationRecipesQuery query = new GetWorkstationRecipesQuery
        {
            CharacterId = _testCharacterId,
            WorkstationTag = new WorkstationTag("forge")
        };

        List<Recipe> result = await _handler.HandleAsync(query);

        Assert.That(result.Any(r => r.RecipeId.Value == "bandage"), Is.False,
            "Portable recipes (no workstation requirement) should not appear at a workstation.");
    }

    [Test]
    public async Task WrongWorkstation_DoesNotShowOtherIndustryRecipes()
    {
        // Enrolled in alchemy, using the forge → should get nothing
        _membershipRepository.Add(new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("alchemy"),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = []
        });
        _knowledgeRepository.AddKnowledge(_testCharacterId.Value, _basicAlchemy);

        GetWorkstationRecipesQuery query = new GetWorkstationRecipesQuery
        {
            CharacterId = _testCharacterId,
            WorkstationTag = new WorkstationTag("forge")
        };

        List<Recipe> result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task EnrolledInWrongIndustry_ForSameWorkstation_ReturnsEmpty()
    {
        // Enrolled in alchemy, using alchemy_table → should only see alchemy recipes
        _membershipRepository.Add(new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("alchemy"),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = []
        });
        _knowledgeRepository.AddKnowledge(_testCharacterId.Value, _basicAlchemy);

        GetWorkstationRecipesQuery query = new GetWorkstationRecipesQuery
        {
            CharacterId = _testCharacterId,
            WorkstationTag = new WorkstationTag("alchemy_table")
        };

        List<Recipe> result = await _handler.HandleAsync(query);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].RecipeId.Value, Is.EqualTo("health_potion"));
    }

    [Test]
    public async Task MissingKnowledge_ExcludesRecipe()
    {
        // Apprentice level but only basic_forging knowledge → steel_sword should be excluded
        _membershipRepository.Add(new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing"),
            Level = ProficiencyLevel.Apprentice,
            CharacterKnowledge = []
        });
        _knowledgeRepository.AddKnowledge(_testCharacterId.Value, _basicForging);
        // NOT adding advanced_forging

        GetWorkstationRecipesQuery query = new GetWorkstationRecipesQuery
        {
            CharacterId = _testCharacterId,
            WorkstationTag = new WorkstationTag("forge")
        };

        List<Recipe> result = await _handler.HandleAsync(query);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].RecipeId.Value, Is.EqualTo("iron_sword"));
    }
}

/// <summary>
/// In-memory test double for IWorkstationRepository.
/// </summary>
internal class TestWorkstationRepository : IWorkstationRepository
{
    private readonly List<Workstation> _workstations = [];

    public bool WorkstationExists(string tag) =>
        _workstations.Any(w => w.Tag.Value == tag);

    public Workstation? GetByTag(WorkstationTag tag) =>
        _workstations.FirstOrDefault(w => w.Tag.Value == tag.Value);

    public List<Workstation> All() => [.._workstations];

    public void Add(Workstation workstation) => _workstations.Add(workstation);

    public void Update(Workstation workstation)
    {
        _workstations.RemoveAll(w => w.Tag.Value == workstation.Tag.Value);
        _workstations.Add(workstation);
    }

    public bool Delete(string tag)
    {
        int removed = _workstations.RemoveAll(w => w.Tag.Value == tag);
        return removed > 0;
    }

    public List<Workstation> Search(string? searchTerm, int page, int pageSize, out int totalCount)
    {
        IEnumerable<Workstation> results = _workstations;
        if (!string.IsNullOrWhiteSpace(searchTerm))
            results = results.Where(w => w.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

        List<Workstation> list = results.ToList();
        totalCount = list.Count;
        return list.Skip(page * pageSize).Take(pageSize).ToList();
    }
}

/// <summary>
/// Empty test double for IRecipeTemplateRepository — no templates exist.
/// </summary>
internal class EmptyRecipeTemplateRepository : IRecipeTemplateRepository
{
    public void Add(RecipeTemplate template) { }
    public RecipeTemplate? GetByTag(string tag) => null;
    public List<RecipeTemplate> GetByIndustry(IndustryTag industryTag) => [];
    public List<RecipeTemplate> All() => [];
    public void Update(RecipeTemplate template) { }
    public bool Delete(string tag) => false;
    public List<RecipeTemplate> Search(string? searchTerm, int page, int pageSize, out int totalCount)
    {
        totalCount = 0;
        return [];
    }
}

/// <summary>
/// Empty test double for IItemDefinitionRepository — no items exist.
/// </summary>
internal class EmptyItemDefinitionRepository : IItemDefinitionRepository
{
    public void AddItemDefinition(ItemBlueprint definition) { }
    public ItemBlueprint? GetByTag(string harvestOutputItemDefinitionTag) => null;
    public ItemBlueprint? GetByResRef(string resRef) => null;
    public List<ItemBlueprint> AllItems() => [];
    public List<string> FindSimilarTags(string tag, int maxResults = 3) => [];
}
