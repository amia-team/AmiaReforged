using AmiaReforged.AdminPanel.Components.Shared;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class InspectorTests : Bunit.TestContext
{
    [Test]
    public void RendersTitle_AndBody()
    {
        IRenderedComponent<Inspector> cut = RenderComponent<Inspector>(parameters => parameters
            .Add(p => p.Title, "Region Props")
            .Add(p => p.Body, "body content"));

        cut.Find(".we-inspector__title").TextContent.Should().Be("Region Props");
        cut.Find(".we-inspector__body").TextContent.Should().Contain("body content");
    }

    [Test]
    public void OmitsFooter_WhenNotProvided()
    {
        IRenderedComponent<Inspector> cut = RenderComponent<Inspector>(parameters => parameters
            .Add(p => p.Title, "T")
            .Add(p => p.Body, "B"));

        cut.FindAll(".we-inspector__footer").Should().BeEmpty();
    }

    [Test]
    public void RendersFooter_WhenProvided()
    {
        IRenderedComponent<Inspector> cut = RenderComponent<Inspector>(parameters => parameters
            .Add(p => p.Title, "T")
            .Add(p => p.Body, "B")
            .Add(p => p.Footer, "footer content"));

        cut.Find(".we-inspector__footer").TextContent.Should().Contain("footer content");
    }

    [Test]
    public void FiresOnClose_WhenCloseButtonClicked()
    {
        bool closed = false;

        IRenderedComponent<Inspector> cut = RenderComponent<Inspector>(parameters => parameters
            .Add(p => p.Title, "T")
            .Add(p => p.Body, "B")
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closed = true)));

        cut.Find(".we-inspector__close").Click();

        closed.Should().BeTrue();
    }
}
