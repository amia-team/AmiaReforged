using AmiaReforged.AdminPanel.Components.Shared;
using AmiaReforged.AdminPanel.Models;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class TabStripTests : Bunit.TestContext
{
    private static List<EditorTab> CreateTabs() =>
    [
        new EditorTab { EntityType = WorldEngineEntityType.Items, Title = "Item A", EntityKey = "a" },
        new EditorTab { EntityType = WorldEngineEntityType.Traits, Title = "Trait B", EntityKey = "b", IsDirty = true },
    ];

    [Test]
    public void RendersNothing_WhenNoTabs()
    {
        IRenderedComponent<TabStrip> cut = RenderComponent<TabStrip>(parameters => parameters
            .Add(p => p.Tabs, new List<EditorTab>()));

        cut.FindAll(".we-editor__tab").Should().BeEmpty();
    }

    [Test]
    public void RendersTabTitles_AndMarksActive()
    {
        List<EditorTab> tabs = CreateTabs();

        IRenderedComponent<TabStrip> cut = RenderComponent<TabStrip>(parameters => parameters
            .Add(p => p.Tabs, tabs)
            .Add(p => p.ActiveTabId, tabs[1].Id)
            .Add(p => p.GetIcon, _ => "bi-box"));

        cut.FindAll(".we-editor__tab").Should().HaveCount(2);
        cut.FindAll(".we-editor__tab-title")[0].TextContent.Should().Be("Item A");
        cut.FindAll(".we-editor__tab--active").Should().HaveCount(1);
        cut.Find(".we-editor__tab--active .we-editor__tab-title").TextContent.Should().Be("Trait B");
    }

    [Test]
    public void ShowsDirtyDot_ForDirtyTabs()
    {
        List<EditorTab> tabs = CreateTabs();

        IRenderedComponent<TabStrip> cut = RenderComponent<TabStrip>(parameters => parameters
            .Add(p => p.Tabs, tabs));

        cut.FindAll(".we-editor__tab-dirty").Should().HaveCount(1);
    }

    [Test]
    public void FiresOnActivate_WhenTabClicked()
    {
        List<EditorTab> tabs = CreateTabs();
        string? activated = null;

        IRenderedComponent<TabStrip> cut = RenderComponent<TabStrip>(parameters => parameters
            .Add(p => p.Tabs, tabs)
            .Add(p => p.OnActivate, EventCallback.Factory.Create<string>(this, id => activated = id)));

        cut.FindAll(".we-editor__tab")[0].Click();

        activated.Should().Be(tabs[0].Id);
    }

    [Test]
    public void FiresOnClose_WhenCloseButtonClicked()
    {
        List<EditorTab> tabs = CreateTabs();
        string? closed = null;

        IRenderedComponent<TabStrip> cut = RenderComponent<TabStrip>(parameters => parameters
            .Add(p => p.Tabs, tabs)
            .Add(p => p.OnClose, EventCallback.Factory.Create<string>(this, id => closed = id)));

        cut.FindAll(".we-editor__tab-close")[1].Click();

        closed.Should().Be(tabs[1].Id);
    }
}
