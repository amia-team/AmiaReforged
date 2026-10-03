using System.Net;
using System.Net.Http.Json;
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
    private readonly TestHttpMessageHandler _handler = new();

    public CodexEditorTests()
    {
        var httpClient = new HttpClient(_handler);
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
    public void RendersToolbar_WithoutViewMenuButton()
    {
        IRenderedComponent<CodexEditor> cut = RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => { })));

        cut.FindAll("button").Should().NotContain(b => b.TextContent.Contains("View"));
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
    public void RendersSplitLayout_InsteadOfGLContainer()
    {
        IRenderedComponent<CodexEditor> cut = RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => { })));

        cut.FindAll("#we-codex-gl-container").Should().BeEmpty();
        cut.Find(".we-ce-layout-area .we-split").Should().NotBeNull();
        cut.FindAll(".we-ce-layout-area .we-split__pane").Should().HaveCountGreaterThan(1);
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

    // ==================== Deletion ====================

    [TestCase(CodexEditor.CodexSubType.Lore)]
    [TestCase(CodexEditor.CodexSubType.Quest)]
    public void Delete_RequiresConfirmation_AndCancelKeepsEntry(CodexEditor.CodexSubType subType)
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(subType);

        cut.Find(".we-ce-toolbar .btn-outline-danger").Click();

        cut.Find("[role='dialog']").TextContent.Should().Contain("Test entry").And.Contain("test_entry");
        _handler.Requests.Should().NotContain(r => r.Method == HttpMethod.Delete);

        cut.Find("[role='dialog'] .btn-secondary").Click();

        cut.FindAll("[role='dialog']").Should().BeEmpty();
        cut.Find(".we-ce-toolbar").TextContent.Should().Contain("Test entry");
        _handler.Requests.Should().NotContain(r => r.Method == HttpMethod.Delete);
    }

    [TestCase(CodexEditor.CodexSubType.Lore)]
    [TestCase(CodexEditor.CodexSubType.Quest)]
    public void Delete_RemovesEntry_ResetsForm_AndRefreshesLists(CodexEditor.CodexSubType subType)
    {
        int refreshCount = 0;
        IRenderedComponent<CodexEditor> cut = RenderEntry(subType, onRefresh: () => refreshCount++);
        int listRequestsBefore = _handler.Requests.Count(r => r.Path == EntryBase(subType));
        cut.FindAll(".we-ce-layout-area span").Should().Contain(e => e.TextContent == "Test entry");

        cut.Find(".we-ce-toolbar .btn-outline-danger").Click();
        cut.Find("[role='dialog'] .btn-danger").Click();

        cut.WaitForAssertion(() =>
        {
            _handler.Requests.Where(r => r.Method == HttpMethod.Delete).Should().ContainSingle()
                .Which.Path.Should().Be($"{EntryBase(subType)}/test_entry");
            refreshCount.Should().Be(1);
            _handler.Requests.Count(r => r.Path == EntryBase(subType)).Should().Be(listRequestsBefore + 1);
            cut.Find(".we-ce-toolbar").TextContent.Should().Contain($"New {subType}").And.Contain("entry deleted.");
            cut.FindAll(".we-ce-toolbar .btn-outline-danger").Should().BeEmpty();
            cut.FindAll("[role='dialog']").Should().BeEmpty();
            cut.Find($"input[placeholder='e.g. {(subType == CodexEditor.CodexSubType.Lore ? "lore_ancient_ruins" : "quest_lost_artifact")}']")
                .GetAttribute("value").Should().BeNullOrEmpty();
            cut.FindAll(".we-ce-layout-area span").Should().NotContain(e => e.TextContent == "Test entry");
        });
    }

    [TestCase(CodexEditor.CodexSubType.Lore)]
    [TestCase(CodexEditor.CodexSubType.Quest)]
    public async Task Delete_DisablesActionsUntilRequestCompletes(CodexEditor.CodexSubType subType)
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(subType);
        var completion = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _handler.DeleteResponse = completion.Task;
        cut.Find(".we-ce-toolbar .btn-outline-danger").Click();

        Task deleting = cut.Find("[role='dialog'] .btn-danger").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        try
        {
            cut.WaitForAssertion(() =>
            {
                cut.Find(".we-ce-toolbar .btn-outline-danger").TextContent.Should().Contain("Deleting");
                cut.FindAll(".we-ce-toolbar button").Should().OnlyContain(b => b.HasAttribute("disabled"));
                cut.Find("select").HasAttribute("disabled").Should().BeTrue();
                _handler.Requests.Where(r => r.Method == HttpMethod.Delete).Should().ContainSingle();
            });
        }
        finally
        {
            completion.SetResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            await deleting;
        }
    }

    [TestCase(CodexEditor.CodexSubType.Lore)]
    [TestCase(CodexEditor.CodexSubType.Quest)]
    public void Delete_ApiFailure_KeepsEntryAndShowsError(CodexEditor.CodexSubType subType)
    {
        int refreshCount = 0;
        IRenderedComponent<CodexEditor> cut = RenderEntry(subType, deleteFails: true, onRefresh: () => refreshCount++);

        cut.Find(".we-ce-toolbar .btn-outline-danger").Click();
        cut.Find("[role='dialog'] .btn-danger").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".we-ce-toolbar").TextContent.Should().Contain("Test entry").And.Contain("Delete failed");
            cut.Find(".we-ce-toolbar .btn-outline-danger").HasAttribute("disabled").Should().BeFalse();
            refreshCount.Should().Be(0);
        });
    }

    [TestCase(CodexEditor.CodexSubType.Lore)]
    [TestCase(CodexEditor.CodexSubType.Quest)]
    public void NewEntry_HasNoDeleteButton(CodexEditor.CodexSubType subType)
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(subType, isNew: true);

        cut.FindAll(".we-ce-toolbar .btn-outline-danger").Should().BeEmpty();
    }

    [Test]
    public void SwitchingEntryType_ClearsDeleteTarget()
    {
        IRenderedComponent<CodexEditor> cut = RenderEntry(CodexEditor.CodexSubType.Lore);

        cut.Find("select").Change("Quest");

        cut.WaitForAssertion(() =>
        {
            cut.Find(".we-ce-toolbar").TextContent.Should().Contain("New Quest");
            cut.FindAll(".we-ce-toolbar .btn-outline-danger").Should().BeEmpty();
            _handler.Requests.Should().NotContain(r => r.Method == HttpMethod.Delete);
        });
    }

    private IRenderedComponent<CodexEditor> RenderEntry(CodexEditor.CodexSubType subType,
        bool isNew = false, bool deleteFails = false, Action? onRefresh = null)
    {
        _handler.Requests.Clear();
        _handler.DeleteResponse = null;
        bool deleted = false;
        object entry = subType == CodexEditor.CodexSubType.Lore
            ? new LoreDefinitionDto { LoreId = "test_entry", Title = "Test entry" }
            : new QuestDefinitionDto { QuestId = "test_entry", Title = "Test entry" };
        _handler.ResponseFactory = request =>
        {
            if (request.Method == HttpMethod.Delete)
            {
                deleted = !deleteFails;
                return deleteFails
                    ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = JsonContent.Create(new { error = "Delete failed", detail = "Test failure" })
                    }
                    : new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            object body = request.RequestUri!.AbsolutePath.EndsWith("/test_entry")
                ? entry
                : new
                {
                    items = !deleted && request.RequestUri.AbsolutePath == EntryBase(subType)
                        ? new[] { entry } : Array.Empty<object>()
                };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
        };
        Guid endpointId = Guid.NewGuid();
        Services.GetRequiredService<LoreApiService>().SelectEndpoint(endpointId);
        Services.GetRequiredService<QuestApiService>().SelectEndpoint(endpointId);
        Services.GetRequiredService<IndustryApiService>().SelectEndpoint(endpointId);

        return RenderComponent<CodexEditor>(parameters => parameters
            .Add(p => p.OpenOnParameters, true)
            .Add(p => p.EntityKey, isNew ? null : "test_entry")
            .Add(p => p.InitialSubType, subType)
            .Add(p => p.OnEntityListRefresh, EventCallback.Factory.Create(this, () => onRefresh?.Invoke())));
    }

    private static string EntryBase(CodexEditor.CodexSubType subType) =>
        $"/api/worldengine/codex/{(subType == CodexEditor.CodexSubType.Lore ? "lore" : "quests")}";

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
        public List<(HttpMethod Method, string Path)> Requests { get; } = [];
        public Func<HttpRequestMessage, HttpResponseMessage>? ResponseFactory { get; set; }
        public Task<HttpResponseMessage>? DeleteResponse { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.RequestUri!.AbsolutePath));
            if (request.Method == HttpMethod.Delete && DeleteResponse != null) return DeleteResponse;
            if (ResponseFactory != null) return Task.FromResult(ResponseFactory(request));

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
