using System.Net;
using System.Net.Http.Json;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.EditorFramework;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class QuestEditorHostTests : Bunit.TestContext
{
    private readonly WorldEngineEndpoint _first = new() { Id = Guid.NewGuid(), Name = "First", BaseUrl = "http://localhost:8080" };
    private readonly WorldEngineEndpoint _second = new() { Id = Guid.NewGuid(), Name = "Second", BaseUrl = "http://localhost:8081" };

    public QuestEditorHostTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Mock<IHttpClientFactory> factory = new();
        factory.Setup(f => f.CreateClient("WorldEngine")).Returns(new HttpClient(new EmptyListsHandler()));
        Mock<IWorldEngineEndpointService> endpoints = new();
        endpoints.Setup(e => e.GetEnabledEndpointsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { _first, _second });
        endpoints.Setup(e => e.GetEndpointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => id == _first.Id ? _first : _second);
        Services.AddSingleton(factory.Object);
        Services.AddSingleton(endpoints.Object);
        Services.AddLogging();
        Services.AddSingleton<WorldEngineEditorState>();
        Services.AddSingleton<LayoutPresetService>();
        Services.AddSingleton<GraphLayoutService>();
        Services.AddSingleton<DeploymentService>();
        foreach (Type api in typeof(ApiServiceBase).Assembly.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(ApiServiceBase))))
            Services.AddSingleton(api);
        Services.AddWorldEngineEditorFramework();
    }

    [Test]
    public void Quest_drafts_survive_tab_activation_and_closing_requires_a_choice()
    {
        var cut = OpenHost();
        AddQuest(cut);
        var first = cut.FindComponents<CodexEditor>().Single();
        first.Find("[data-quest-title]").Input("First draft");
        AddQuest(cut);
        var editors = cut.FindComponents<CodexEditor>();
        editors.Should().HaveCount(2);
        var second = editors.Single(e => e.Instance != first.Instance);
        second.Find("[data-quest-title]").Input("Second draft");

        cut.FindAll(".we-editor__tab")[0].Click();
        first.Find("[data-quest-title]").GetAttribute("value").Should().Be("First draft");
        second.Find("[data-quest-title]").GetAttribute("value").Should().Be("Second draft");
        cut.FindAll(".we-editor__tab-fill")[0].HasAttribute("hidden").Should().BeFalse();
        cut.FindAll(".we-editor__tab-fill")[1].HasAttribute("hidden").Should().BeTrue();
        cut.FindAll(".we-editor__tab-close")[0].Click();
        cut.Find("[role='dialog'] .btn-secondary").Click();
        cut.FindComponents<CodexEditor>().Should().HaveCount(2);
        cut.FindAll(".we-editor__tab-close")[0].Click();
        cut.Find("[role='dialog'] .btn-outline-danger").Click();
        cut.WaitForAssertion(() => cut.FindComponents<CodexEditor>().Should().ContainSingle());
        second.Find("[data-quest-title]").GetAttribute("value").Should().Be("Second draft");
    }

    [Test]
    public void Server_switch_can_be_canceled_and_discards_resolve_each_quest_draft()
    {
        var cut = OpenHost();
        AddQuest(cut);
        cut.FindComponents<CodexEditor>().Single().Find("[data-quest-title]").Input("First draft");
        AddQuest(cut);
        cut.FindComponents<CodexEditor>().Last().Find("[data-quest-title]").Input("Second draft");
        var state = Services.GetRequiredService<WorldEngineEditorState>();
        cut.Find(".we-editor__endpoint-selector select").Change(_second.Id.ToString());
        state.SelectedEndpointId.Should().Be(_first.Id);
        cut.Find("[role='dialog'] .btn-secondary").Click();
        cut.Find(".we-editor__endpoint-selector select").GetAttribute("value").Should().Be(_first.Id.ToString());
        cut.Find(".we-editor__endpoint-selector select").Change(_second.Id.ToString());
        cut.Find("[role='dialog'] .btn-outline-danger").Click();
        state.SelectedEndpointId.Should().Be(_first.Id);
        cut.Find("[role='dialog'] .btn-outline-danger").Click();
        cut.WaitForAssertion(() => state.SelectedEndpointId.Should().Be(_second.Id));
        cut.FindComponents<CodexEditor>().Should().BeEmpty();
    }

    [Test]
    public async Task Navigation_waits_until_the_quest_draft_is_resolved()
    {
        var cut = OpenHost();
        AddQuest(cut);
        cut.FindComponents<CodexEditor>().Single().Find("[data-quest-title]").Input("Unsaved quest");
        var navigation = (FakeNavigationManager)Services.GetRequiredService<NavigationManager>();
        await cut.InvokeAsync(() => navigation.NavigateTo("/another-page"));
        cut.WaitForAssertion(() => cut.Find("[role='dialog']").TextContent.Should().Contain("unsaved changes"));
        navigation.History.First().State.Should().Be(NavigationState.Prevented);
        cut.Find("[role='dialog'] .btn-outline-danger").Click();
        cut.WaitForAssertion(() => navigation.Uri.Should().EndWith("/another-page"));
        navigation.History.First().State.Should().Be(NavigationState.Succeeded);
    }

    private IRenderedComponent<WorldEngineEditor> OpenHost()
    {
        var cut = RenderComponent<WorldEngineEditor>();
        cut.Find(".we-editor__endpoint-selector select").Change(_first.Id.ToString());
        cut.Find("button[title='Codex']").Click();
        return cut;
    }

    private static void AddQuest(IRenderedComponent<WorldEngineEditor> cut)
    {
        cut.Find("button[title='Create new codex entry']").Click();
        cut.FindAll(".we-editor__new-menu-items button").Single(b => b.TextContent == "Quest").Click();
        cut.WaitForAssertion(() => cut.FindComponents<CodexEditor>().Last().Find("[data-quest-title]"));
    }

    private sealed class EmptyListsHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { items = Array.Empty<object>() }) });
    }
}
