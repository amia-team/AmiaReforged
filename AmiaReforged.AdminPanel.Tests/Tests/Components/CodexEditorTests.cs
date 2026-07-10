using System.Net;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class CodexEditorTests : Bunit.TestContext
{
    public CodexEditorTests()
    {
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("WorldEngine")).Returns(httpClient);

        var endpointService = new Mock<IWorldEngineEndpointService>();
        endpointService
            .Setup(s => s.GetEndpointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorldEngineEndpoint
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                BaseUrl = "http://localhost:8080",
                ApiKey = "test-key"
            });

        Services.AddSingleton<LoreApiService>(new LoreApiService(factory.Object, endpointService.Object));
        Services.AddSingleton<QuestApiService>(new QuestApiService(factory.Object, endpointService.Object));
        Services.AddSingleton<IndustryApiService>(new IndustryApiService(factory.Object, endpointService.Object));
        Services.AddSingleton<ILogger<CodexEditor>>(new Mock<ILogger<CodexEditor>>().Object);
    }

    // ==================== Static Helpers: Lore Category Names ====================

    [Test]
    public void GetLoreCategoryName_ReturnsCorrectNames()
    {
        CodexEditor.CeGetLoreCategoryName(0).Should().Be("General");
        CodexEditor.CeGetLoreCategoryName(1).Should().Be("History");
        CodexEditor.CeGetLoreCategoryName(2).Should().Be("Geography");
        CodexEditor.CeGetLoreCategoryName(3).Should().Be("Religion");
        CodexEditor.CeGetLoreCategoryName(4).Should().Be("Arcana");
        CodexEditor.CeGetLoreCategoryName(5).Should().Be("Nature");
        CodexEditor.CeGetLoreCategoryName(6).Should().Be("Faction");
        CodexEditor.CeGetLoreCategoryName(7).Should().Be("Character");
        CodexEditor.CeGetLoreCategoryName(8).Should().Be("Item");
        CodexEditor.CeGetLoreCategoryName(9).Should().Be("Quest");
        CodexEditor.CeGetLoreCategoryName(10).Should().Be("Miscellaneous");
    }

    [Test]
    public void GetLoreCategoryName_ReturnsFallbackForUnknown()
    {
        CodexEditor.CeGetLoreCategoryName(99).Should().Be("Unknown (99)");
        CodexEditor.CeGetLoreCategoryName(-1).Should().Be("Unknown (-1)");
    }

    // ==================== Static Helpers: Lore Tier Names ====================

    [Test]
    public void GetLoreTierName_ReturnsCorrectNames()
    {
        CodexEditor.CeGetLoreTierName(0).Should().Be("Common");
        CodexEditor.CeGetLoreTierName(1).Should().Be("Uncommon");
        CodexEditor.CeGetLoreTierName(2).Should().Be("Rare");
        CodexEditor.CeGetLoreTierName(3).Should().Be("Legendary");
    }

    [Test]
    public void GetLoreTierName_ReturnsFallbackForUnknown()
    {
        CodexEditor.CeGetLoreTierName(99).Should().Be("Unknown (99)");
    }

    // ==================== Static Helpers: Config ====================

    [Test]
    public void GetConfigString_ReturnsValue_WhenKeyExists()
    {
        var obj = new ObjectiveDefinitionDto
        {
            Config = new Dictionary<string, object> { ["mode"] = "clue_graph" }
        };

        CodexEditor.GetConfigString(obj, "mode", "fallback").Should().Be("clue_graph");
    }

    [Test]
    public void GetConfigString_ReturnsFallback_WhenKeyMissing()
    {
        var obj = new ObjectiveDefinitionDto { Config = new Dictionary<string, object>() };

        CodexEditor.GetConfigString(obj, "missing", "fallback").Should().Be("fallback");
    }

    [Test]
    public void GetConfigString_ReturnsFallback_WhenConfigNull()
    {
        var obj = new ObjectiveDefinitionDto { Config = null };

        CodexEditor.GetConfigString(obj, "key", "fallback").Should().Be("fallback");
    }

    [Test]
    public void SetConfig_SetsValue_WhenConfigNull()
    {
        var obj = new ObjectiveDefinitionDto { Config = null };

        CodexEditor.SetConfig(obj, "key", "value");

        obj.Config.Should().NotBeNull();
        obj.Config!["key"]!.ToString().Should().Be("value");
    }

    [Test]
    public void SetConfig_OverwritesExistingValue()
    {
        var obj = new ObjectiveDefinitionDto
        {
            Config = new Dictionary<string, object> { ["key"] = "old" }
        };

        CodexEditor.SetConfig(obj, "key", "new");

        obj.Config!["key"]!.ToString().Should().Be("new");
    }

    [Test]
    public void SetConfig_CreatesMultipleKeys()
    {
        var obj = new ObjectiveDefinitionDto { Config = null };

        CodexEditor.SetConfig(obj, "k1", "v1");
        CodexEditor.SetConfig(obj, "k2", "v2");

        obj.Config.Should().HaveCount(2);
        obj.Config!["k1"]!.ToString().Should().Be("v1");
        obj.Config["k2"]!.ToString().Should().Be("v2");
    }

    // ==================== Rendering Basics ====================

    [Test]
    public void RendersToolbar_WithCancelAndSaveButtons()
    {
        IRenderedComponent<CodexEditor> cut = RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => { })));

        cut.Find(".we-ce-toolbar").Should().NotBeNull();
        cut.FindAll("button").Should().Contain(b => b.TextContent.Contains("Cancel"));
        cut.FindAll("button").Should().Contain(b => b.TextContent.Contains("Save"));
    }

    [Test]
    public void RendersToolbar_WithViewMenuButton()
    {
        IRenderedComponent<CodexEditor> cut = RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => { })));

        cut.FindAll("button").Should().Contain(b => b.TextContent.Contains("View"));
    }

    [Test]
    public void RendersToolbar_ShowsLoreBadge()
    {
        IRenderedComponent<CodexEditor> cut = RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => { })));

        cut.Find(".we-ce-toolbar").TextContent.Should().Contain("Lore");
    }

    [Test]
    public void RendersGLContainer()
    {
        IRenderedComponent<CodexEditor> cut = RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => { })));

        cut.Find("#we-codex-gl-container").Should().NotBeNull();
    }

    [Test]
    public void RendersLayoutArea()
    {
        IRenderedComponent<CodexEditor> cut = RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => { })));

        cut.Find(".we-ce-layout-area").Should().NotBeNull();
    }

    // ==================== Callbacks ====================

    [Test]
    public void OnClose_Fires_WhenCancelClicked()
    {
        bool closeFired = false;

        IRenderedComponent<CodexEditor> cut = RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closeFired = true))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => { })));

        cut.FindAll("button").Single(b => b.TextContent.Contains("Cancel")).Click();

        closeFired.Should().BeTrue();
    }

    // ==================== CodexSubType Enum ====================

    [Test]
    public void CodexSubType_HasBothValues()
    {
        CodexEditor.CodexSubType.Lore.Should().Be(CodexEditor.CodexSubType.Lore);
        CodexEditor.CodexSubType.Quest.Should().Be(CodexEditor.CodexSubType.Quest);
        CodexEditor.CodexSubType.Lore.Should().NotBe(CodexEditor.CodexSubType.Quest);
    }

    // ==================== Helper types ====================

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
