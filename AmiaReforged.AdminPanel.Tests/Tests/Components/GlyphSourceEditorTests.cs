using System.Net;
using System.Text.Json;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class GlyphSourceEditorTests
{
    private Bunit.TestContext _context = null!;
    private Handler _handler = null!;
    private readonly Guid _id = Guid.NewGuid();
    [SetUp] public void Setup()
    {
        _context = new(); _handler = new(_id);
        var factory = new Mock<IHttpClientFactory>(); factory.Setup(f => f.CreateClient("WorldEngine")).Returns(new HttpClient(_handler));
        var endpoints = new Mock<IWorldEngineEndpointService>();
        endpoints.Setup(e => e.GetEndpointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorldEngineEndpoint { Id = Guid.NewGuid(), Name = "test", BaseUrl = "http://localhost:8080", ApiKey = "test" });
        var api = new GlyphApiService(factory.Object, endpoints.Object); api.SelectEndpoint(Guid.NewGuid());
        _context.Services.AddSingleton(api);
    }
    [TearDown] public void Cleanup() => _context.Dispose();
    [Test] public void Structured_diagnostics_are_visible_and_invalid_source_cannot_activate()
    {
        var cut = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.DefinitionId, _id));
        cut.Find("textarea").Input("invalid");
        cut.FindAll("button").Single(b => b.TextContent == "Compile / validate").Click();
        cut.WaitForAssertion(() => Assert.That(cut.Find("ul").TextContent, Does.Contain("GLYPH2002").And.Contain("line 3, column 4")));
        Assert.That(cut.FindAll("button").Single(b => b.TextContent == "Activate").HasAttribute("disabled"), Is.True);
        Assert.That(_handler.Paths.Any(p => p.EndsWith("/activate")), Is.False);
    }
    [Test] public void Save_draft_is_separate_from_activation_and_edit_invalidates_validation()
    {
        _handler.Valid = true;
        var cut = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.DefinitionId, _id));
        cut.FindAll("button").Single(b => b.TextContent == "Save draft").Click();
        cut.WaitForAssertion(() => Assert.That(_handler.Paths, Does.Contain($"PUT /api/worldengine/glyphs/{_id}")));
        Assert.That(_handler.Paths.Any(p => p.EndsWith("/activate")), Is.False);
        cut.FindAll("button").Single(b => b.TextContent == "Compile / validate").Click();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("button").Single(b => b.TextContent == "Activate").HasAttribute("disabled"), Is.False));
        cut.Find("textarea").Input("changed");
        Assert.That(cut.FindAll("button").Single(b => b.TextContent == "Activate").HasAttribute("disabled"), Is.True);
    }
    private sealed class Handler(Guid id) : HttpMessageHandler
    {
        public bool Valid;
        public List<string> Paths { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string path = request.RequestUri!.AbsolutePath;
            Paths.Add($"{request.Method} {path}");
            object response = path.EndsWith("/versions") ? Array.Empty<GlyphVersionDto>() :
                path.EndsWith("/compile") ? new GlyphCompilationDto(Valid, Valid ? [] : [new("GLYPH2002", "Unknown function", new("test.glyph", 0, 1, 3, 4))]) :
                new GlyphDefinitionDto(id, "test", null, "InteractionPipeline", "Interaction", "glyph a : interaction {}", false, DateTime.UtcNow, DateTime.UtcNow);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(response), System.Text.Encoding.UTF8, "application/json") });
        }
    }
}
