using AmiaReforged.AdminPanel.Components.Shared;
using Bunit;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class ResizableSplitTests : Bunit.TestContext
{
    private IRenderedComponent<ResizableSplit> RenderSplit(
        string direction = "horizontal", bool collapsibleSecond = true) =>
        RenderComponent<ResizableSplit>(parameters => parameters
            .Add(p => p.First, "pane one")
            .Add(p => p.Second, "pane two")
            .Add(p => p.Direction, direction)
            .Add(p => p.CollapsibleSecond, collapsibleSecond));

    [Test]
    public void RendersBothPanes_AndHandle()
    {
        IRenderedComponent<ResizableSplit> cut = RenderSplit();

        cut.FindAll(".we-split__pane").Should().HaveCount(2);
        cut.Find(".we-split__handle").Should().NotBeNull();
        cut.Find(".we-split--horizontal").Should().NotBeNull();
        cut.Find(".we-split__pane--first").TextContent.Should().Contain("pane one");
        cut.Find(".we-split__pane--second").TextContent.Should().Contain("pane two");
    }

    [Test]
    public void AppliesVerticalDirectionClass()
    {
        IRenderedComponent<ResizableSplit> cut = RenderSplit(direction: "vertical");

        cut.Find(".we-split--vertical").Should().NotBeNull();
    }

    [Test]
    public void ClickingHandle_CollapsesSecondPane()
    {
        IRenderedComponent<ResizableSplit> cut = RenderSplit();

        cut.Find(".we-split__handle").Click();

        cut.FindAll(".we-split__pane").Should().HaveCount(1);
        cut.Find(".we-split__pane--first").Should().NotBeNull();
    }

    [Test]
    public void ClickingHandleAgain_ExpandsPanes()
    {
        IRenderedComponent<ResizableSplit> cut = RenderSplit();

        cut.Find(".we-split__handle").Click();
        cut.Find(".we-split__handle").Click();

        cut.FindAll(".we-split__pane").Should().HaveCount(2);
    }
}
