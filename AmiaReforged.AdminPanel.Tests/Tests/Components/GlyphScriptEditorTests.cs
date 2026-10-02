using System.Net;
using System.Text.Json;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Microsoft.JSInterop;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class GlyphScriptEditorTests : Bunit.TestContext
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly Guid _endpointId = Guid.NewGuid();

    public GlyphScriptEditorTests()
    {
        var httpClient = new HttpClient(_handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("WorldEngine")).Returns(httpClient);

        var endpointService = new Mock<IWorldEngineEndpointService>();
        endpointService
            .Setup(s => s.GetEndpointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorldEngineEndpoint
            {
                Id = _endpointId,
                Name = "Test",
                BaseUrl = "http://localhost:8080",
                ApiKey = "test-key"
            });

        var glyph = new GlyphApiService(factory.Object, endpointService.Object);
        glyph.SelectEndpoint(_endpointId);
        var encounter = new EncounterApiService(factory.Object, endpointService.Object);
        encounter.SelectEndpoint(_endpointId);
        var trait = new TraitApiService(factory.Object, endpointService.Object);
        trait.SelectEndpoint(_endpointId);

        Services.AddSingleton(glyph);
        Services.AddSingleton(encounter);
        Services.AddSingleton(trait);
        Services.AddSingleton(new Mock<ILogger<GlyphScriptEditor>>().Object);
        var js = new Mock<IJSRuntime>();
        js.Setup(m => m.InvokeAsync<IJSObjectReference>("import", It.IsAny<object?[]>()))
            .ThrowsAsync(new JSException("Editor unavailable"));
        Services.AddSingleton(js.Object);
    }

    [SetUp]
    public void ResetHandler()
    {
        _handler.RouteResponses = new Dictionary<string, string>
        {
            ["GET /api/worldengine/glyphs"] = JsonSerializer.Serialize(new[]
            {
                new GlyphDefinitionDto(Guid.NewGuid(), "Guardian Ward", "A protective ward", "OnCreatureSpawn", "Encounter", "{}", true,
                    DateTime.UtcNow, DateTime.UtcNow)
            }),
            ["GET /api/worldengine/glyph-catalog"] = "[]",
            ["GET /api/worldengine/encounters/profiles"] = "[]"
        };
    }

    [Test]
    public async Task SelectEndpoint_ThenLoadList_ShowsDefinitions()
    {
        IRenderedComponent<GlyphScriptEditor> cut = RenderComponent<GlyphScriptEditor>();

        cut.Instance.SelectEndpoint(_endpointId);
        await cut.InvokeAsync(() => cut.Instance.LoadListAsync());

        cut.Find("table").TextContent.Should().Contain("Guardian Ward");
    }

    [Test]
    public void SelectEndpoint_Null_ShowsSelectionPrompt()
    {
        IRenderedComponent<GlyphScriptEditor> cut = RenderComponent<GlyphScriptEditor>();

        cut.Instance.SelectEndpoint(null);

        cut.Find("div.section").TextContent.Should().Contain("Select a server from the World Engine toolbar");
    }

    [TestCase("trait.on_granted", "glyph new_program : trait.on_granted {\n    \n}")]
    [TestCase("interaction", "glyph new_program : interaction {\n    attempted { }\n    started { }\n    tick { }\n    completed { }\n}")]
    public async Task New_script_uses_selected_event_template(string eventName, string expectedSource)
    {
        var metadata = new GlyphLanguageMetadataDto(6, [],
            [new("trait.on_granted", "OnTraitGranted", "Trait", []),
             new("interaction", "InteractionPipeline", "Interaction", ["attempted", "started", "tick", "completed"])],
            [], [], []);
        _handler.RouteResponses["GET /api/worldengine/glyphs/language-metadata"] = JsonSerializer.Serialize(metadata);
        var cut = RenderComponent<GlyphScriptEditor>();
        cut.Instance.SelectEndpoint(_endpointId);
        await cut.InvokeAsync(() => cut.Instance.LoadListAsync());

        cut.FindAll("button").Single(b => b.TextContent == "New script").Click();
        cut.WaitForAssertion(() => Assert.That(cut.Find("#glyph-new-event"), Is.Not.Null));
        Assert.That(cut.FindAll("button").Single(b => b.TextContent == "Open editor").HasAttribute("disabled"), Is.True);
        cut.Find("#glyph-new-event").Change(eventName);
        cut.FindAll("button").Single(b => b.TextContent == "Open editor").Click();

        Assert.That(cut.FindComponent<GlyphSourceEditor>().Instance.InitialSource, Is.EqualTo(expectedSource));
        Assert.That(cut.FindComponent<GlyphSourceEditor>().Instance.DefinitionId, Is.Null);
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        public Dictionary<string, string> RouteResponses { get; set; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string key = $"{request.Method} {request.RequestUri!.AbsolutePath}";
            string body = RouteResponses.TryGetValue(key, out string? value) ? value : "[]";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
