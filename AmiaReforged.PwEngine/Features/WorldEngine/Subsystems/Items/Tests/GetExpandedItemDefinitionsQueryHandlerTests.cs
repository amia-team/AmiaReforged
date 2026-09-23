using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.Tests;

[TestFixture]
public class GetExpandedItemDefinitionsQueryHandlerTests
{
    private const string TemplateTag = "wpn_sword_template";

    private Mock<ItemBlueprintExpander> _expander = null!;
    private GetExpandedItemDefinitionsQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _expander = new Mock<ItemBlueprintExpander>(MockBehavior.Strict, Mock.Of<IItemDefinitionRepository>());
        _handler = new GetExpandedItemDefinitionsQueryHandler(_expander.Object);
    }

    [Test]
    public async Task Handler_ForwardsRequestedTemplateTagToExpander()
    {
        _expander.Setup(e => e.GetExpandedItemsForTemplate(TemplateTag))
            .Returns(new List<ItemBlueprint>());

        await _handler.HandleAsync(new GetExpandedItemDefinitionsQuery(TemplateTag));

        _expander.Verify(
            e => e.GetExpandedItemsForTemplate(TemplateTag),
            Times.Once,
            "Handler must pass the query's template tag through to the expander unchanged");
    }

    [Test]
    public async Task Handler_ReturnsPopulatedExpansionResults()
    {
        List<ItemBlueprint> expanded = new()
        {
            MakeItem($"{TemplateTag}_wood"),
            MakeItem($"{TemplateTag}_coldiron")
        };
        _expander.Setup(e => e.GetExpandedItemsForTemplate(TemplateTag)).Returns(expanded);

        List<ItemBlueprint> result = await _handler.HandleAsync(new GetExpandedItemDefinitionsQuery(TemplateTag));

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(i => i.ItemTag),
            Is.EqualTo(expanded.Select(i => i.ItemTag)));
    }

    [Test]
    public async Task Handler_ReturnsEmptyListWhenNoExpansionExists()
    {
        _expander.Setup(e => e.GetExpandedItemsForTemplate(TemplateTag)).Returns(new List<ItemBlueprint>());

        List<ItemBlueprint> result = await _handler.HandleAsync(new GetExpandedItemDefinitionsQuery(TemplateTag));

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    private static ItemBlueprint MakeItem(string itemTag) => new(
        ResRef: itemTag.ToUpperInvariant(),
        ItemTag: itemTag,
        Name: itemTag,
        Description: string.Empty,
        Materials: [MaterialEnum.WoodOak],
        ItemForm: Subsystems.Harvesting.ItemForm.None,
        BaseItemType: 0,
        Appearance: new AppearanceData(0, null, null));
}
