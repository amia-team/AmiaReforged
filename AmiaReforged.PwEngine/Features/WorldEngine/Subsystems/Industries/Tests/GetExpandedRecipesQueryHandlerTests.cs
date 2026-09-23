using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;

[TestFixture]
public class GetExpandedRecipesQueryHandlerTests
{
    private const string TemplateTag = "wpn_sword_template";

    private Mock<RecipeTemplateExpander> _expander = null!;
    private GetExpandedRecipesQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _expander = new Mock<RecipeTemplateExpander>(MockBehavior.Strict, Mock.Of<IRecipeTemplateRepository>(), Mock.Of<IItemDefinitionRepository>());
        _handler = new GetExpandedRecipesQueryHandler(_expander.Object);
    }

    [Test]
    public async Task Handler_ForwardsRequestedTemplateTagToExpander()
    {
        _expander.Setup(e => e.GetExpandedRecipesForTemplate(TemplateTag))
            .Returns(new List<Recipe>());

        await _handler.HandleAsync(new GetExpandedRecipesQuery(TemplateTag));

        _expander.Verify(
            e => e.GetExpandedRecipesForTemplate(TemplateTag),
            Times.Once,
            "Handler must pass the query's template tag through to the expander unchanged");
    }

    [Test]
    public async Task Handler_ReturnsPopulatedExpansionResults()
    {
        List<Recipe> expanded = new()
        {
            MakeRecipe($"{TemplateTag}_wood"),
            MakeRecipe($"{TemplateTag}_coldiron")
        };
        _expander.Setup(e => e.GetExpandedRecipesForTemplate(TemplateTag)).Returns(expanded);

        List<Recipe> result = await _handler.HandleAsync(new GetExpandedRecipesQuery(TemplateTag));

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(r => r.RecipeId.Value),
            Is.EqualTo(expanded.Select(r => r.RecipeId.Value)));
    }

    [Test]
    public async Task Handler_ReturnsEmptyListWhenNoExpansionExists()
    {
        _expander.Setup(e => e.GetExpandedRecipesForTemplate(TemplateTag)).Returns(new List<Recipe>());

        List<Recipe> result = await _handler.HandleAsync(new GetExpandedRecipesQuery(TemplateTag));

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    private static Recipe MakeRecipe(string recipeId) => new()
    {
        RecipeId = new RecipeId(recipeId),
        Name = recipeId,
        Description = string.Empty,
        IndustryTag = new IndustryTag("smithing"),
        Ingredients = [],
        Products = []
    };
}
