using System.Net;
using System.Text.Json;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Microsoft.JSInterop;
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
        var js = new Mock<IJSRuntime>();
        js.Setup(m => m.InvokeAsync<IJSObjectReference>("import", It.IsAny<object?[]>()))
            .ThrowsAsync(new JSException("Editor unavailable"));
        _context.Services.AddSingleton(js.Object);
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
        var editor = cut.FindComponent<GlyphCodeEditor>().Instance;
        Assert.That(editor.DiagnosticSource, Is.EqualTo("invalid"));
        Assert.That(editor.Diagnostics.Single().Code, Is.EqualTo("GLYPH2002"));
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
    [Test] public void Browser_snapshot_is_saved_and_unreported_edit_cannot_activate()
    {
        _handler.Valid = true;
        string browserSource = "glyph a : interaction {}";
        long revision = 0;
        var module = new Mock<IJSObjectReference>();
        module.Setup(m => m.InvokeAsync<GlyphCodeEditor.EditorSnapshot>("capture", It.IsAny<object?[]>()))
            .Returns(() => ValueTask.FromResult(new GlyphCodeEditor.EditorSnapshot(browserSource, revision)));
        var js = new Mock<IJSRuntime>();
        js.Setup(m => m.InvokeAsync<IJSObjectReference>("import", It.IsAny<object?[]>()))
            .ReturnsAsync(module.Object);
        _context.Services.AddSingleton(js.Object);
        var cut = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.DefinitionId, _id));
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("textarea"), Is.Empty));
        cut.FindAll("button").Single(b => b.TextContent == "Compile / validate").Click();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("button").Single(b => b.TextContent == "Activate").HasAttribute("disabled"), Is.False));

        browserSource = "glyph changed : interaction {}";
        revision++;
        cut.FindAll("button").Single(b => b.TextContent == "Activate").Click();
        cut.WaitForAssertion(() => Assert.That(cut.Find("[role=alert]").TextContent, Does.Contain("Compile / validate")));
        Assert.That(_handler.Paths.Any(p => p.EndsWith("/activate")), Is.False);
        cut.FindAll("button").Single(b => b.TextContent == "Save draft").Click();
        cut.WaitForAssertion(() => Assert.That(_handler.LastSavedSource, Is.EqualTo(browserSource)));
    }

    [Test] public async Task Validation_for_an_earlier_edit_is_discarded()
    {
        var pending = new TaskCompletionSource<HttpResponseMessage?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _handler.Intercept = request => request.RequestUri!.AbsolutePath.EndsWith("/compile")
            ? pending.Task : Task.FromResult<HttpResponseMessage?>(null);
        var cut = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.DefinitionId, _id));
        var validation = cut.FindAll("button").Single(b => b.TextContent == "Compile / validate").ClickAsync(new());
        cut.Find("textarea").Input("newer source");
        pending.SetResult(JsonResponse(new GlyphCompilationDto(true, [])));
        await validation;
        Assert.That(cut.FindAll("button").Single(b => b.TextContent == "Activate").HasAttribute("disabled"), Is.True);
        Assert.That(cut.FindAll("ul[aria-label='Compiler diagnostics']"), Is.Empty);
    }

    [Test] public async Task Navigation_discards_old_validation_and_load_responses()
    {
        var pending = new TaskCompletionSource<HttpResponseMessage?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _handler.Intercept = request => request.RequestUri!.AbsolutePath.EndsWith("/compile")
            ? pending.Task : Task.FromResult<HttpResponseMessage?>(null);
        var cut = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.DefinitionId, _id));
        var validation = cut.FindAll("button").Single(b => b.TextContent == "Compile / validate").ClickAsync(new());
        cut.SetParametersAndRender(p => p.Add(c => c.DefinitionId, Guid.NewGuid()));
        pending.SetResult(JsonResponse(new GlyphCompilationDto(false, [new("OLD", "Old diagnostic", new("old", 0, 1, 1, 1))])));
        await validation;
        Assert.That(cut.Markup, Does.Not.Contain("Old diagnostic"));
        Assert.That(cut.FindAll("button").Single(b => b.TextContent == "Activate").HasAttribute("disabled"), Is.True);

        var load = new TaskCompletionSource<HttpResponseMessage?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var slowId = Guid.NewGuid();
        _handler.Intercept = request => request.RequestUri!.AbsolutePath.EndsWith(slowId.ToString())
            ? load.Task : Task.FromResult<HttpResponseMessage?>(null);
        cut.SetParametersAndRender(p => p.Add(c => c.DefinitionId, slowId));
        cut.SetParametersAndRender(p => p.Add(c => c.DefinitionId, (Guid?)null).Add(c => c.InitialSource, "new document"));
        load.SetResult(JsonResponse(new GlyphDefinitionDto(slowId, "slow", null, "InteractionPipeline", "Interaction", "stale document", false, DateTime.UtcNow, DateTime.UtcNow)));
        await cut.InvokeAsync(async () => await Task.Yield());
        Assert.That(await cut.FindComponent<GlyphCodeEditor>().InvokeAsync(() => cut.FindComponent<GlyphCodeEditor>().Instance.CaptureAsync()), Is.EqualTo("new document"));
    }

    [Test] public async Task Rollback_replaces_source_and_invalidates_validation_while_history_and_traces_work()
    {
        _handler.Valid = true;
        _handler.Versions = [new(Guid.NewGuid(), _id, DateTime.UtcNow, null, "old", 1, true),
            new(Guid.NewGuid(), _id, DateTime.UtcNow, null, "new", 1, false)];
        _handler.Intercept = request =>
        {
            if (!request.RequestUri!.AbsolutePath.EndsWith("/rollback")) return Task.FromResult<HttpResponseMessage?>(null);
            _handler.Source = "glyph restored : interaction {}";
            return Task.FromResult<HttpResponseMessage?>(JsonResponse(new GlyphCompilationDto(true, [])));
        };
        var cut = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.DefinitionId, _id));
        cut.FindAll("button").Single(b => b.TextContent == "Compile / validate").Click();
        cut.FindAll("button").Single(b => b.TextContent == "Rollback").Click();
        var editor = cut.FindComponent<GlyphCodeEditor>();
        Assert.That(await editor.InvokeAsync(() => editor.Instance.CaptureAsync()), Is.EqualTo(_handler.Source));
        Assert.That(cut.FindAll("button").Single(b => b.TextContent == "Activate").HasAttribute("disabled"), Is.True);
        Assert.That(cut.FindAll("tbody tr").Count, Is.EqualTo(2));
        cut.FindAll("button").Single(b => b.TextContent == "Refresh traces").Click();
        Assert.That(_handler.Paths.Any(p => p.EndsWith("/traces")), Is.True);
    }

    [Test] public void Metadata_failure_is_nonblocking_and_retry_loads_suggestions()
    {
        _handler.Intercept = request => Task.FromResult<HttpResponseMessage?>(request.RequestUri!.AbsolutePath.EndsWith("/language-metadata")
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("{}") } : null);
        var cut = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.DefinitionId, _id));
        Assert.That(cut.Find("textarea").HasAttribute("disabled"), Is.False);
        Assert.That(cut.Markup, Does.Contain("Function suggestions are unavailable"));
        _handler.Intercept = null;
        cut.FindAll("button").Single(b => b.TextContent == "Retry suggestions").Click();
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Not.Contain("Function suggestions are unavailable")));
        Assert.That(cut.FindComponent<GlyphCodeEditor>().Instance.Metadata, Is.Not.Null);
    }

    [Test] public void Reference_insertion_reaches_CodeMirror_and_collapsing_preserves_the_editor()
    {
        var js = new Mock<IJSRuntime>();
        var module = new Mock<IJSObjectReference>();
        js.Setup(m => m.InvokeAsync<IJSObjectReference>("import", It.IsAny<object?[]>())).ReturnsAsync(module.Object);
        _context.Services.AddSingleton(js.Object);
        _handler.Metadata = GlyphReferencePanelTests.Metadata;
        var cut = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.DefinitionId, _id));
        cut.Find("input[type=search]").Input("GetTag");
        cut.Find(".glyph-reference-row").Click();
        cut.WaitForAssertion(() => Assert.That(cut.Find(".glyph-reference-actions button").HasAttribute("disabled"), Is.False));
        cut.Find(".glyph-reference-actions button").Click();
        module.Verify(m => m.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>("insertFunction",
            It.Is<object?[]>(a => ((GlyphFunctionMetadataDto)a[1]!).Name == "nwn.get_tag" && ((GlyphFunctionMetadataDto)a[1]!).Parameters.Count == 2)), Times.Once);
        cut.FindAll(".glyph-reference-tabs button").Single(b => b.TextContent == "Constants").Click();
        cut.Find("input[type=search]").Input("CREATURE"); cut.Find(".glyph-reference-row").DoubleClick();
        module.Verify(m => m.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>("insertConstant",
            It.Is<object?[]>(a => ((GlyphConstantMetadataDto)a[1]!).Name == "OBJECT_TYPE.CREATURE")), Times.Once);
        var editor = cut.FindComponent<GlyphCodeEditor>().Instance;
        cut.Find(".glyph-source-toolbar button").Click();
        Assert.That(cut.FindAll(".glyph-reference"), Is.Empty);
        Assert.That(cut.FindComponent<GlyphCodeEditor>().Instance, Is.SameAs(editor));
    }

    [Test] public async Task Endpoint_switch_discards_pending_metadata_and_clears_reference_selection()
    {
        var pending = new TaskCompletionSource<HttpResponseMessage?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _handler.Metadata = GlyphReferencePanelTests.Metadata;
        var cut = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.DefinitionId, _id));
        cut.Find("input[type=search]").Input("GetTag"); cut.Find(".glyph-reference-row").Click();
        Assert.That(cut.Markup, Does.Contain("NWScript.GetTag"));
        _handler.Intercept = request => request.RequestUri!.AbsolutePath.EndsWith("/language-metadata")
            ? pending.Task : Task.FromResult<HttpResponseMessage?>(null);
        var api = _context.Services.GetRequiredService<GlyphApiService>();
        api.SelectEndpoint(Guid.NewGuid()); cut.SetParametersAndRender(p => p.Add(c => c.DefinitionId, _id));
        Assert.That(cut.Markup, Does.Not.Contain("NWScript.GetTag"));
        _handler.Intercept = null; _handler.Metadata = new(1, [], [], [], [], []);
        api.SelectEndpoint(Guid.NewGuid()); cut.SetParametersAndRender(p => p.Add(c => c.DefinitionId, _id));
        pending.SetResult(JsonResponse(GlyphReferencePanelTests.Metadata));
        await cut.InvokeAsync(async () => await Task.Yield());
        Assert.That(cut.FindComponent<GlyphReferencePanel>().Instance.Metadata!.Functions, Is.Empty);
        Assert.That(cut.Markup, Does.Not.Contain("NWScript.GetTag"));
    }


    [Test] public async Task Documentation_opens_the_hidden_reference_without_recreating_or_changing_the_editor()
    {
        _handler.Metadata = GlyphReferencePanelTests.Metadata;
        var cut = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.DefinitionId, _id));
        var editor = cut.FindComponent<GlyphCodeEditor>().Instance;
        cut.FindAll("button").Single(b => b.TextContent == "Hide reference").Click();
        Assert.That(cut.FindAll("aside"), Is.Empty);
        await cut.InvokeAsync(() => editor.OnDocumentationRequested("nwn.get_tag"));
        cut.WaitForAssertion(() => Assert.That(cut.Find(".glyph-reference-detail").TextContent, Does.Contain("NWScript.GetTag")));
        Assert.That(cut.FindComponent<GlyphCodeEditor>().Instance, Is.SameAs(editor));
        Assert.That(await cut.InvokeAsync(() => editor.CaptureAsync()), Is.EqualTo(_handler.Source));
        Assert.That(_handler.Paths.Any(p => p.StartsWith("PUT") || p.EndsWith("/compile")), Is.False);
    }

    private static HttpResponseMessage JsonResponse(object response) => new(HttpStatusCode.OK)
    { Content = new StringContent(JsonSerializer.Serialize(response), System.Text.Encoding.UTF8, "application/json") };

    private sealed class Handler(Guid id) : HttpMessageHandler
    {
        public bool Valid;
        public GlyphLanguageMetadataDto Metadata = new(1, [], [], [], [], []);
        public string Source = "glyph a : interaction {}";
        public List<GlyphVersionDto> Versions = [];
        public Func<HttpRequestMessage, Task<HttpResponseMessage?>>? Intercept;
        public string? LastSavedSource;
        public List<string> Paths { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string path = request.RequestUri!.AbsolutePath;
            Paths.Add($"{request.Method} {path}");
            if (Intercept != null && await Intercept(request) is { } intercepted) return intercepted;
            if (request.Method == HttpMethod.Put)
            {
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                LastSavedSource = body.RootElement.GetProperty("SourceText").GetString();
            }
            object response = path.EndsWith("/language-metadata") ? Metadata :
            path.EndsWith("/traces") ? Array.Empty<GlyphTraceDto>() :
                path.EndsWith("/versions") ? Versions :
                path.EndsWith("/compile") ? new GlyphCompilationDto(Valid, Valid ? [] : [new("GLYPH2002", "Unknown function", new("test.glyph", 0, 1, 3, 4))]) :
                new GlyphDefinitionDto(id, "test", null, "InteractionPipeline", "Interaction", Source, false, DateTime.UtcNow, DateTime.UtcNow);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(response), System.Text.Encoding.UTF8, "application/json") };
        }
    }
}
