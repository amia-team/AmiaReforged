using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;

[TestFixture]
public class IndustryQueryTests
{
    private IIndustryRepository _industryRepository = null!;
    private TestIndustryMembershipRepository _membershipRepository = null!;
    private TestCharacterKnowledgeRepository _knowledgeRepository = null!;
    private GetIndustryRecipesHandler _getRecipesHandler = null!;
    private GetAvailableRecipesHandler _getAvailableRecipesHandler = null!;

    private Industry _testIndustry = null!;
    private Recipe _noviceRecipe = null!;
    private Recipe _apprenticeRecipe = null!;
    private CharacterId _testCharacterId;
    private Knowledge _basicForgingKnowledge = null!;
    private Knowledge _advancedForgingKnowledge = null!;

    [SetUp]
    public void SetUp()
    {
        _industryRepository = new InMemoryIndustryRepository();
        _membershipRepository = new TestIndustryMembershipRepository();
        _knowledgeRepository = new TestCharacterKnowledgeRepository();
        _getRecipesHandler = new GetIndustryRecipesHandler(_industryRepository);
        _getAvailableRecipesHandler = new GetAvailableRecipesHandler(
            _industryRepository,
            _membershipRepository,
            _knowledgeRepository);

        _testCharacterId = new CharacterId(Guid.NewGuid());

        // Create knowledge definitions
        _basicForgingKnowledge = new Knowledge
        {
            Tag = "basic_forging",
            Name = "Basic Forging",
            Description = "Basic forging techniques",
            Level = ProficiencyLevel.Novice,
            PointCost = 1
        };

        _advancedForgingKnowledge = new Knowledge
        {
            Tag = "advanced_forging",
            Name = "Advanced Forging",
            Description = "Advanced forging techniques",
            Level = ProficiencyLevel.Apprentice,
            PointCost = 2
        };

        // Create test recipes
        _noviceRecipe = new Recipe
        {
            RecipeId = new RecipeId("iron_sword"),
            Name = "Iron Sword",
            IndustryTag = new IndustryTag("blacksmithing"),
            RequiredKnowledge = ["basic_forging"],
            RequiredProficiency = ProficiencyLevel.Novice,
            Ingredients =
            [
                new Ingredient
                {
                    ItemResRef = "iron_ingot",
                    Quantity = Quantity.Parse(3),
                    MinQuality = 1
                }
            ],
            Products =
            [
                new Product
                {
                    ItemResRef = "iron_sword",
                    Quantity = Quantity.Parse(1),
                    Quality = 2
                }
            ],
            KnowledgePointsAwarded = 5
        };

        _apprenticeRecipe = new Recipe
        {
            RecipeId = new RecipeId("steel_sword"),
            Name = "Steel Sword",
            IndustryTag = new IndustryTag("blacksmithing"),
            RequiredKnowledge = ["basic_forging", "advanced_forging"],
            RequiredProficiency = ProficiencyLevel.Apprentice,
            Ingredients =
            [
                new Ingredient
                {
                    ItemResRef = "steel_ingot",
                    Quantity = Quantity.Parse(3),
                    MinQuality = 1
                }
            ],
            Products =
            [
                new Product
                {
                    ItemResRef = "steel_sword",
                    Quantity = Quantity.Parse(1),
                    Quality = 3
                }
            ],
            KnowledgePointsAwarded = 10
        };

        _testIndustry = new Industry
        {
            Tag = "blacksmithing",
            Name = "Blacksmithing",
            Knowledge = [_basicForgingKnowledge, _advancedForgingKnowledge],
            Recipes = [_noviceRecipe, _apprenticeRecipe]
        };

        _industryRepository.Add(_testIndustry);
    }

    [Test]
    public async Task GetIndustryRecipes_ReturnsAllRecipes()
    {
        // Arrange
        GetIndustryRecipesQuery query = new GetIndustryRecipesQuery
        {
            IndustryTag = new IndustryTag("blacksmithing")
        };

        // Act
        List<Recipe> recipes = await _getRecipesHandler.HandleAsync(query);

        // Assert
        Assert.That(recipes, Has.Count.EqualTo(2));
        Assert.That(recipes, Has.Exactly(1).Matches<Recipe>(r => r.RecipeId.Value == "iron_sword"));
        Assert.That(recipes, Has.Exactly(1).Matches<Recipe>(r => r.RecipeId.Value == "steel_sword"));
    }

    [Test]
    public async Task GetIndustryRecipes_IndustryNotFound_ReturnsEmpty()
    {
        // Arrange
        GetIndustryRecipesQuery query = new GetIndustryRecipesQuery
        {
            IndustryTag = new IndustryTag("nonexistent")
        };

        // Act
        List<Recipe> recipes = await _getRecipesHandler.HandleAsync(query);

        // Assert
        Assert.That(recipes, Is.Empty);
    }

    [Test]
    public async Task GetAvailableRecipes_CharacterNotMember_ReturnsEmpty()
    {
        // Arrange
        GetAvailableRecipesQuery query = new GetAvailableRecipesQuery
        {
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing")
        };

        // Act
        List<Recipe> recipes = await _getAvailableRecipesHandler.HandleAsync(query);

        // Assert
        Assert.That(recipes, Is.Empty);
    }

    [Test]
    public async Task GetAvailableRecipes_NoviceWithKnowledge_ReturnsNoviceRecipes()
    {
        // Arrange - create membership
        IndustryMembership membership = new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing"),
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = []
        };
        _membershipRepository.Add(membership);

        // Add knowledge
        _knowledgeRepository.AddKnowledge(_testCharacterId.Value, _basicForgingKnowledge);

        GetAvailableRecipesQuery query = new GetAvailableRecipesQuery
        {
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing")
        };

        // Act
        List<Recipe> recipes = await _getAvailableRecipesHandler.HandleAsync(query);

        // Assert
        Assert.That(recipes, Has.Count.EqualTo(1));
        Assert.That(recipes[0].RecipeId.Value, Is.EqualTo("iron_sword"));
    }

    [Test]
    public async Task GetAvailableRecipes_ApprenticeWithAllKnowledge_ReturnsBothRecipes()
    {
        // Arrange - create membership
        IndustryMembership membership = new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing"),
            Level = ProficiencyLevel.Apprentice,
            CharacterKnowledge = []
        };
        _membershipRepository.Add(membership);

        // Add both knowledge items
        _knowledgeRepository.AddKnowledge(_testCharacterId.Value, _basicForgingKnowledge);
        _knowledgeRepository.AddKnowledge(_testCharacterId.Value, _advancedForgingKnowledge);

        GetAvailableRecipesQuery query = new GetAvailableRecipesQuery
        {
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing")
        };

        // Act
        List<Recipe> recipes = await _getAvailableRecipesHandler.HandleAsync(query);

        // Assert
        Assert.That(recipes, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAvailableRecipes_MissingKnowledge_ExcludesRecipe()
    {
        // Arrange - apprentice level but missing knowledge
        IndustryMembership membership = new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing"),
            Level = ProficiencyLevel.Apprentice,
            CharacterKnowledge = []
        };
        _membershipRepository.Add(membership);

        // Add only basic knowledge, not advanced
        _knowledgeRepository.AddKnowledge(_testCharacterId.Value, _basicForgingKnowledge);

        GetAvailableRecipesQuery query = new GetAvailableRecipesQuery
        {
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing")
        };

        // Act
        List<Recipe> recipes = await _getAvailableRecipesHandler.HandleAsync(query);

        // Assert - should only get novice recipe
        Assert.That(recipes, Has.Count.EqualTo(1));
        Assert.That(recipes[0].RecipeId.Value, Is.EqualTo("iron_sword"));
    }
}

// Test repositories
internal class TestIndustryMembershipRepository : IIndustryMembershipRepository
{
    private readonly List<IndustryMembership> _memberships = new();

    public List<IndustryMembership> All(Guid characterGuid)
    {
        return _memberships.Where(m => m.CharacterId.Value == characterGuid).ToList();
    }

    public void Add(IndustryMembership membership)
    {
        _memberships.Add(membership);
    }

    public void Update(IndustryMembership membership)
    {
        IndustryMembership? existing = _memberships.FirstOrDefault(m => m.Id == membership.Id);
        if (existing != null)
        {
            _memberships.Remove(existing);
            _memberships.Add(membership);
        }
    }

    public void SaveChanges()
    {
        // No-op for in-memory
    }
}

internal class TestCharacterKnowledgeRepository : ICharacterKnowledgeRepository
{
    private readonly List<Knowledge> _knowledge = new();

    public void AddKnowledge(Guid characterId, Knowledge knowledge)
    {
        _knowledge.Add(knowledge);
    }

    public List<CharacterKnowledge> GetKnowledgeForIndustry(string industryTag, Guid characterId)
    {
        return new List<CharacterKnowledge>();
    }

    public void Add(CharacterKnowledge ck)
    {
        // Not used in these tests
    }

    public bool AlreadyKnows(Guid membershipCharacterId, Knowledge tag)
    {
        return _knowledge.Any(k => k.Tag == tag.Tag);
    }

    public List<Knowledge> GetAllKnowledge(Guid getId)
    {
        return _knowledge.ToList();
    }

    public void SaveChanges()
    {
        // No-op for in-memory
    }
}

