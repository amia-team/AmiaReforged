using AmiaReforged.AdminPanel.Components.Shared;
using AmiaReforged.AdminPanel.Models;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class EntityListPanelTests : Bunit.TestContext
{
    private static IReadOnlyList<EntityListItem> CreateItems(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new EntityListItem($"key_{i}", $"Item {i}", WorldEngineEntityType.Items))
            .ToList();

    // ==================== Rendering ====================

    [Test]
    public void RendersListItems_WithDisplayNameAndKey()
    {
        IReadOnlyList<EntityListItem> items = CreateItems(3);

        IRenderedComponent<EntityListPanel> cut = RenderComponent<EntityListPanel>(parameters => parameters
            .Add(p => p.Items, items));

        cut.FindAll(".we-editor__list-item").Should().HaveCount(3);
        cut.FindAll(".we-editor__list-item-name")[0].TextContent.Should().Be("Item 1");
        cut.FindAll(".we-editor__list-item-key")[0].TextContent.Should().Be("key_1");
    }

    [Test]
    public void RendersEmptyState_WhenItemsIsEmpty()
    {
        IRenderedComponent<EntityListPanel> cut = RenderComponent<EntityListPanel>(parameters => parameters
            .Add(p => p.Items, []));

        cut.Find("p.text-muted").TextContent.Should().Contain("No entities found");
    }

    [Test]
    public void RendersLoadingState_WhenLoadingIsTrue()
    {
        IRenderedComponent<EntityListPanel> cut = RenderComponent<EntityListPanel>(parameters => parameters
            .Add(p => p.Loading, true));

        cut.Find("p.text-muted em").TextContent.Should().Contain("Loading");
    }

    [Test]
    public void RendersErrorState_WhenErrorIsSet()
    {
        IRenderedComponent<EntityListPanel> cut = RenderComponent<EntityListPanel>(parameters => parameters
            .Add(p => p.Error, "Something went wrong"));

        cut.Find("p.text-danger").TextContent.Should().Be("Something went wrong");
    }

    // ==================== Load More ====================

    [Test]
    public void ShowsLoadMoreButton_WhenHasMoreIsTrue()
    {
        IReadOnlyList<EntityListItem> items = CreateItems(2);

        IRenderedComponent<EntityListPanel> cut = RenderComponent<EntityListPanel>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.HasMore, true));

        cut.Find(".we-editor__list-load-more").TextContent.Should().Contain("Load more");
    }

    [Test]
    public void HidesLoadMoreButton_WhenHasMoreIsFalse()
    {
        IReadOnlyList<EntityListItem> items = CreateItems(2);

        IRenderedComponent<EntityListPanel> cut = RenderComponent<EntityListPanel>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.HasMore, false));

        cut.FindAll(".we-editor__list-load-more").Should().BeEmpty();
    }

    // ==================== Callbacks ====================

    [Test]
    public void FiresOnLoadMore_WhenLoadMoreClicked()
    {
        bool loadMoreFired = false;
        IReadOnlyList<EntityListItem> items = CreateItems(2);

        IRenderedComponent<EntityListPanel> cut = RenderComponent<EntityListPanel>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.HasMore, true)
            .Add(p => p.OnLoadMore, EventCallback.Factory.Create(this, () => loadMoreFired = true)));

        cut.Find(".we-editor__list-load-more").Click();

        loadMoreFired.Should().BeTrue();
    }

    [Test]
    public void FiresOnItemClick_WithCorrectItem()
    {
        EntityListItem? clicked = null;
        IReadOnlyList<EntityListItem> items = CreateItems(3);

        IRenderedComponent<EntityListPanel> cut = RenderComponent<EntityListPanel>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.OnItemClick, EventCallback.Factory.Create<EntityListItem>(this, item => clicked = item)));

        cut.FindAll(".we-editor__list-item")[1].Click();

        clicked.Should().NotBeNull();
        clicked!.Key.Should().Be("key_2");
        clicked.DisplayName.Should().Be("Item 2");
    }

    [Test]
    public void FiresOnSearch_WithInputValue()
    {
        string? searched = null;

        IRenderedComponent<EntityListPanel> cut = RenderComponent<EntityListPanel>(parameters => parameters
            .Add(p => p.OnSearch, EventCallback.Factory.Create<string>(this, value => searched = value)));

        cut.Find("input").Input("sword");

        searched.Should().Be("sword");
    }

    [Test]
    public void SearchInput_ReflectsSearchParameter()
    {
        IRenderedComponent<EntityListPanel> cut = RenderComponent<EntityListPanel>(parameters => parameters
            .Add(p => p.Search, "hello"));

        cut.Find("input").GetAttribute("value").Should().Be("hello");
    }
}
