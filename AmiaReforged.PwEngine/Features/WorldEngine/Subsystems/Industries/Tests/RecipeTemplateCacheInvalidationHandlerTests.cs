using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;

[TestFixture]
public class RecipeTemplateCacheInvalidationHandlerTests
{
    private const string Tag = "wpn_sword_template";

    private Mock<RecipeTemplateExpander> _expander = null!;
    private RecipeTemplateCacheInvalidationHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _expander = new Mock<RecipeTemplateExpander>(
            MockBehavior.Strict,
            Mock.Of<IRecipeTemplateRepository>(),
            Mock.Of<IItemDefinitionRepository>());
        _handler = new RecipeTemplateCacheInvalidationHandler(_expander.Object);
    }

    private static CommandExecutedEvent<TCommand> EventFor<TCommand>(TCommand command)
        where TCommand : ICommand
        => new(command, CommandResult.Ok());

    private static RecipeTemplate MakeTemplate(string tag) => new()
    {
        Tag = tag,
        Name = tag,
        IndustryTag = new IndustryTag("blacksmithing"),
        Ingredients = [],
        Products = []
    };

    [Test]
    public async Task Create_InvalidatesExpansionCache()
    {
        _expander.Setup(e => e.Invalidate());

        await _handler.HandleAsync(EventFor(new CreateRecipeTemplateCommand
        {
            Template = MakeTemplate(Tag)
        }));

        _expander.Verify(e => e.Invalidate(), Times.Once, "A created template must refresh the cache");
    }

    [Test]
    public async Task Update_InvalidatesExpansionCache()
    {
        _expander.Setup(e => e.Invalidate());

        await _handler.HandleAsync(EventFor(new UpdateRecipeTemplateCommand
        {
            Tag = Tag,
            Template = MakeTemplate(Tag)
        }));

        _expander.Verify(e => e.Invalidate(), Times.Once, "An updated template must refresh the cache");
    }

    [Test]
    public async Task Delete_InvalidatesExpansionCache()
    {
        _expander.Setup(e => e.Invalidate());

        await _handler.HandleAsync(EventFor(new DeleteRecipeTemplateCommand { Tag = Tag }));

        _expander.Verify(e => e.Invalidate(), Times.Once, "A deleted template must refresh the cache");
    }

    [Test]
    public async Task EachMutationType_InvalidatesExactlyOnce()
    {
        _expander.Setup(e => e.Invalidate());

        await _handler.HandleAsync(EventFor(new CreateRecipeTemplateCommand { Template = MakeTemplate(Tag) }));
        await _handler.HandleAsync(EventFor(new UpdateRecipeTemplateCommand { Tag = Tag, Template = MakeTemplate(Tag) }));
        await _handler.HandleAsync(EventFor(new DeleteRecipeTemplateCommand { Tag = Tag }));

        _expander.Verify(e => e.Invalidate(), Times.Exactly(3), "Every successful mutation type must trigger invalidation");
    }
}
