using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.CraftAndIndustry;

[TestFixture]
public class RecipeManagementTests
{
    private Industry _testIndustry = null!;
    private Recipe _testRecipe = null!;

    [SetUp]
    public void SetUp()
    {
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
                },
                new Ingredient
                {
                    ItemResRef = "leather_strip",
                    Quantity = Quantity.Parse(1),
                    MinQuality = null
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
            CraftingTimeSeconds = 300,
            KnowledgePointsAwarded = 5
        };
    }

    [Test]
    public void Industry_CanAddRecipe()
    {
        // Arrange - industry starts empty
        Assert.That(_testIndustry.Recipes, Is.Empty);

        // Act - add recipe
        _testIndustry.Recipes.Add(_testRecipe);

        // Assert
        Assert.That(_testIndustry.Recipes, Has.Count.EqualTo(1));
        Assert.That(_testIndustry.Recipes[0].RecipeId.Value, Is.EqualTo("iron_sword"));
        Assert.That(_testIndustry.Recipes[0].Name, Is.EqualTo("Iron Sword"));
    }

    [Test]
    public void Recipe_HasCorrectIngredients()
    {
        // Assert
        Assert.That(_testRecipe.Ingredients, Has.Count.EqualTo(2));

        Ingredient ironIngot = _testRecipe.Ingredients[0];
        Assert.That(ironIngot.ItemResRef, Is.EqualTo("iron_ingot"));
        Assert.That(ironIngot.Quantity.Value, Is.EqualTo(3));
        Assert.That(ironIngot.MinQuality, Is.EqualTo(1));
        Assert.That(ironIngot.IsConsumed, Is.True);

        Ingredient leatherStrip = _testRecipe.Ingredients[1];
        Assert.That(leatherStrip.ItemResRef, Is.EqualTo("leather_strip"));
        Assert.That(leatherStrip.Quantity.Value, Is.EqualTo(1));
        Assert.That(leatherStrip.MinQuality, Is.Null);
    }

    [Test]
    public void Recipe_HasCorrectProducts()
    {
        // Assert
        Assert.That(_testRecipe.Products, Has.Count.EqualTo(1));

        Product sword = _testRecipe.Products[0];
        Assert.That(sword.ItemResRef, Is.EqualTo("iron_sword"));
        Assert.That(sword.Quantity.Value, Is.EqualTo(1));
        Assert.That(sword.Quality, Is.EqualTo(2));
    }

    [Test]
    public void Recipe_HasCorrectRequirements()
    {
        // Assert
        Assert.That(_testRecipe.RequiredProficiency, Is.EqualTo(ProficiencyLevel.Novice));
        Assert.That(_testRecipe.RequiredKnowledge, Contains.Item("basic_forging"));
        Assert.That(_testRecipe.IndustryTag.Value, Is.EqualTo("blacksmithing"));
    }

    [Test]
    public void Industry_CanRemoveRecipe()
    {
        // Arrange
        _testIndustry.Recipes.Add(_testRecipe);
        Assert.That(_testIndustry.Recipes, Has.Count.EqualTo(1));

        // Act
        _testIndustry.Recipes.RemoveAll(r => r.RecipeId == _testRecipe.RecipeId);

        // Assert
        Assert.That(_testIndustry.Recipes, Is.Empty);
    }

    [Test]
    public void Industry_CanFindRecipeById()
    {
        // Arrange
        _testIndustry.Recipes.Add(_testRecipe);
        RecipeId searchId = new RecipeId("iron_sword");

        // Act
        Recipe? found = _testIndustry.Recipes.FirstOrDefault(r => r.RecipeId == searchId);

        // Assert
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Name, Is.EqualTo("Iron Sword"));
    }
}

