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
