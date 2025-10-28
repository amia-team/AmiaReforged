using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.CraftAndIndustry;

[TestFixture]
public class IndustryCommandTests
{
    private IIndustryRepository _industryRepository = null!;
    private AddRecipeToIndustryHandler _addRecipeHandler = null!;
    private RemoveRecipeFromIndustryHandler _removeRecipeHandler = null!;
    private Industry _testIndustry = null!;
    private Recipe _testRecipe = null!;

    [SetUp]
    public void SetUp()
    {
        _industryRepository = new InMemoryIndustryRepository();
        _addRecipeHandler = new AddRecipeToIndustryHandler(_industryRepository);
        _removeRecipeHandler = new RemoveRecipeFromIndustryHandler(_industryRepository);

        _testIndustry = new Industry
        {
            Tag = "blacksmithing",
            Name = "Blacksmithing",
            Knowledge = [],
            Recipes = []
        };

        _testRecipe = new Recipe
        {
            RecipeId = new RecipeId("iron_sword"),
            Name = "Iron Sword",
            Description = "A basic iron sword",
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

        // Add industry to repository
        _industryRepository.Add(_testIndustry);
    }

    [Test]
    public async Task AddRecipe_Success()
    {
        // Arrange
        var command = new AddRecipeToIndustryCommand
        {
            IndustryTag = new IndustryTag("blacksmithing"),
            Recipe = _testRecipe
        };

        // Act
        var result = await _addRecipeHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_testIndustry.Recipes, Has.Count.EqualTo(1));
        Assert.That(_testIndustry.Recipes[0].RecipeId, Is.EqualTo(_testRecipe.RecipeId));
    }

    [Test]
    public async Task AddRecipe_IndustryNotFound_Fails()
    {
        // Arrange
        var command = new AddRecipeToIndustryCommand
        {
            IndustryTag = new IndustryTag("nonexistent"),
            Recipe = _testRecipe
        };

        // Act
        var result = await _addRecipeHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public async Task AddRecipe_DuplicateRecipeId_Fails()
    {
        // Arrange - add recipe first time
        _testIndustry.Recipes.Add(_testRecipe);

        var command = new AddRecipeToIndustryCommand
        {
            IndustryTag = new IndustryTag("blacksmithing"),
            Recipe = _testRecipe
        };

        // Act
        var result = await _addRecipeHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("already exists"));
    }

    [Test]
    public async Task AddRecipe_IndustryTagMismatch_Fails()
    {
        // Arrange - recipe for different industry
        var wrongRecipe = new Recipe
        {
            RecipeId = new RecipeId("healing_potion"),
            Name = "Healing Potion",
            IndustryTag = new IndustryTag("alchemy"), // Different industry!
            RequiredKnowledge = [],
            RequiredProficiency = ProficiencyLevel.Novice,
            Ingredients = [],
            Products = [],
            KnowledgePointsAwarded = 0
        };

        var command = new AddRecipeToIndustryCommand
        {
            IndustryTag = new IndustryTag("blacksmithing"),
            Recipe = wrongRecipe
        };

        // Act
        var result = await _addRecipeHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("does not match"));
    }

    [Test]
    public async Task RemoveRecipe_Success()
    {
        // Arrange - add recipe first
        _testIndustry.Recipes.Add(_testRecipe);

        var command = new RemoveRecipeFromIndustryCommand
        {
            IndustryTag = new IndustryTag("blacksmithing"),
            RecipeId = new RecipeId("iron_sword")
        };

        // Act
        var result = await _removeRecipeHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_testIndustry.Recipes, Is.Empty);
    }

    [Test]
    public async Task RemoveRecipe_RecipeNotFound_Fails()
    {
        // Arrange
        var command = new RemoveRecipeFromIndustryCommand
        {
            IndustryTag = new IndustryTag("blacksmithing"),
            RecipeId = new RecipeId("nonexistent_recipe")
        };

        // Act
        var result = await _removeRecipeHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }
}

