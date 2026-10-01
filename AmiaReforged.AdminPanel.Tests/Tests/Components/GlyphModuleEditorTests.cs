using System.Net;
using System.Text.Json;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public sealed class GlyphModuleEditorTests
{
    private Bunit.TestContext _context = null!;
    private Handler _handler = null!;
    private readonly Guid _endpoint = Guid.NewGuid();
    [SetUp] public void Setup()
    {
        _context = new(); _handler = new();
        var js = new Mock<IJSRuntime>(); js.Setup(j => j.InvokeAsync<IJSObjectReference>("import", It.IsAny<object?[]>())).ThrowsAsync(new JSException("Fallback"));
        _context.Services.AddSingleton(js.Object);
        var factory = new Mock<IHttpClientFactory>(); factory.Setup(f => f.CreateClient("WorldEngine")).Returns(new HttpClient(_handler));
        var endpoints = new Mock<IWorldEngineEndpointService>(); endpoints.Setup(e => e.GetEndpointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new WorldEngineEndpoint { Id = _endpoint, Name = "test", BaseUrl = "http://localhost:8080", ApiKey = "test" });
        var api = new GlyphApiService(factory.Object, endpoints.Object); api.SelectEndpoint(_endpoint); _context.Services.AddSingleton(api);
    }
    [TearDown] public void Cleanup() => _context.Dispose();
    [Test] public void Module_drafts_publish_with_validation_fingerprint_and_do_not_show_script_binding_controls()
    {
        var cut = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.LibraryMode, true).Add(c => c.ModuleName, "helpers").Add(c => c.DefinitionId, _handler.Module.Id));
        Assert.That(cut.Markup, Does.Not.Contain("Runtime traces"));
        cut.FindAll("button").Single(b => b.TextContent == "Save draft").Click();
        cut.WaitForAssertion(() => Assert.That(_handler.Paths, Does.Contain($"PUT /api/worldengine/glyph-modules/{_handler.Module.Id}")));
        Assert.That(_handler.Paths.Any(p => p.EndsWith("/publish")), Is.False);
        cut.FindAll("button").Single(b => b.TextContent == "Compile / validate").Click();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("button").Single(b => b.TextContent == "Publish module").HasAttribute("disabled"), Is.False));
        cut.FindAll("button").Single(b => b.TextContent == "Publish module").Click();
        cut.WaitForAssertion(() => Assert.That(_handler.ExpectedHash, Is.EqualTo("module-hash")));
        Assert.That(_handler.Paths.Any(p => p.Contains("/activate")), Is.False);
    }
    [Test] public void Editing_module_source_invalidates_publication_and_management_can_archive_and_restore()
    {
        var source = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.LibraryMode, true).Add(c => c.ModuleName, "helpers").Add(c => c.DefinitionId, _handler.Module.Id));
        source.FindAll("button").Single(b => b.TextContent == "Compile / validate").Click();
        source.WaitForAssertion(() => Assert.That(source.FindAll("button").Single(b => b.TextContent == "Publish module").HasAttribute("disabled"), Is.False));
        source.Find("textarea").Input("mod helpers { pub const TEXT = \"changed\" }");
        Assert.That(source.FindAll("button").Single(b => b.TextContent == "Publish module").HasAttribute("disabled"), Is.True);
        source.Dispose();
        var manager = _context.RenderComponent<GlyphModuleEditor>(p => p.Add(c => c.EndpointId, _endpoint));
        manager.FindAll("button").Single(b => b.TextContent == "Archive").Click();
        manager.WaitForAssertion(() => Assert.That(manager.Markup, Does.Contain("Archived")));
        manager.FindAll("button").Single(b => b.TextContent == "Restore").Click();
        manager.WaitForAssertion(() => Assert.That(manager.FindAll("button").Any(b => b.TextContent == "Edit module"), Is.True));
    }
    [Test] public void Module_overlay_updates_do_not_resend_standard_documentation_to_the_code_editor()
    {
        var js = new Mock<IJSRuntime>(); var module = new Mock<IJSObjectReference>();
        js.Setup(j => j.InvokeAsync<IJSObjectReference>("import", It.IsAny<object?[]>())).ReturnsAsync(module.Object); _context.Services.AddSingleton(js.Object);
        var source = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.LibraryMode, true).Add(c => c.ModuleName, "helpers").Add(c => c.DefinitionId, _handler.Module.Id));
        source.WaitForAssertion(() => module.Verify(m => m.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>("setMetadata", It.IsAny<object?[]>()), Times.Once));
        source.WaitForAssertion(() => module.Verify(m => m.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>("setModuleMetadata", It.IsAny<object?[]>()), Times.AtLeastOnce));
        var editor = source.FindComponent<GlyphCodeEditor>();
        editor.InvokeAsync(() => editor.Instance.OnEditorChanged("mod helpers { pub const TEXT = \"edited\" }", 1)).GetAwaiter().GetResult();
        source.WaitForAssertion(() => Assert.That(_handler.ModuleMetadataRequests, Is.GreaterThan(1)), TimeSpan.FromSeconds(2));
        module.Verify(m => m.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>("setMetadata", It.IsAny<object?[]>()), Times.Once);
    }
    [Test] public void Legacy_scripts_keep_their_version_and_real_prelude_imports_upgrade_it()
    {
        var source = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.DefinitionId, _handler.Module.Id));
        source.FindAll("button").Single(b => b.TextContent == "Compile / validate").Click();
        source.WaitForAssertion(() => Assert.That(_handler.LastLanguageVersion, Is.EqualTo(1)));
        source.Find("textarea").Input("const TEXT = \"using helpers\" using helpers glyph t : interaction {}");
        source.FindAll("button").Single(b => b.TextContent == "Compile / validate").Click();
        source.WaitForAssertion(() => Assert.That(_handler.LastLanguageVersion, Is.EqualTo(2)));
        source.Find("textarea").Input("// fn f(): Int { return 1 }\nconst TEXT = \"fn f(): Int { return 1 }\" glyph t : interaction {}");
        source.FindAll("button").Single(b => b.TextContent == "Compile / validate").Click();
        source.WaitForAssertion(() => Assert.That(_handler.LastLanguageVersion, Is.EqualTo(1)));
        source.Find("textarea").Input("fn f(): Int { return 1 } glyph t : interaction { attempted { let n = f() } }");
        source.FindAll("button").Single(b => b.TextContent == "Compile / validate").Click();
        source.WaitForAssertion(() => Assert.That(_handler.LastLanguageVersion, Is.EqualTo(3)));
    }
    [Test] public void Generic_syntax_selects_glyph_5_and_comments_do_not_upgrade_legacy_scripts()
    {
        var source = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.DefinitionId, _handler.Module.Id));
        foreach (string code in new[] {
            "// type Option<T> {}\nconst TEXT = \"fn identity<T>(value: T): T = value\" glyph t : interaction {}",
            "struct Box<T> { value: T, } glyph t : interaction {}",
            "glyph t : interaction { completed { let target = Option<Object>.None() } }" })
        {
            source.Find("textarea").Input(code);
            source.FindAll("button").Single(b => b.TextContent == "Compile / validate").Click();
            int expectedVersion = code.StartsWith("//") ? 1 : 5;
            source.WaitForAssertion(() => Assert.That(_handler.LastLanguageVersion, Is.EqualTo(expectedVersion)));
        }
    }
    [Test] public void Explicit_language_upgrade_preserves_source_and_invalidates_validation()
    {
        var source = _context.RenderComponent<GlyphSourceEditor>(p => p.Add(c => c.DefinitionId, _handler.Module.Id));
        source.FindAll("button").Single(b => b.TextContent == "Compile / validate").Click();
        source.WaitForAssertion(() => Assert.That(source.FindAll("button").Single(b => b.TextContent == "Activate").HasAttribute("disabled"), Is.False));
        string original = source.Find("textarea").GetAttribute("value")!;
        source.FindAll("button").Single(b => b.TextContent == "Upgrade to Glyph 6").Click();
        Assert.That(source.FindAll("button").Single(b => b.TextContent == "Activate").HasAttribute("disabled"), Is.True);
        Assert.That(source.Find("textarea").GetAttribute("value"), Is.EqualTo(original));
        source.FindAll("button").Single(b => b.TextContent == "Compile / validate").Click();
        source.WaitForAssertion(() => Assert.That(_handler.LastLanguageVersion, Is.EqualTo(6)));
    }
    private sealed class Handler : HttpMessageHandler
    {
        public GlyphModuleDto Module = new(Guid.NewGuid(), "helpers", "mod helpers { pub const TEXT = \"test\" }", false, null, DateTime.UtcNow, DateTime.UtcNow, []);
        public List<string> Paths = []; public string? ExpectedHash; public int ModuleMetadataRequests, LastLanguageVersion;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string path = request.RequestUri!.AbsolutePath; Paths.Add(request.Method + " " + path); object response;
            if (path.EndsWith("/language-metadata")) response = new GlyphLanguageMetadataDto(2, [], [], [], [], []);
            else if (path.EndsWith("/module-metadata"))
            { ModuleMetadataRequests++; response = new GlyphModuleMetadataDto([], [], [], [], ["helpers"], new Dictionary<string, GlyphSourceSpanDto>(), "registry", []); }
            else if (path.EndsWith("/compile"))
            {
                if (path.Contains("/glyphs/")) LastLanguageVersion = JsonSerializer.Deserialize<CompileGlyphRequest>(await request.Content!.ReadAsStringAsync(), new JsonSerializerOptions(JsonSerializerDefaults.Web))!.LanguageVersion;
                response = new GlyphCompilationDto(true, [], CompilationHash: "module-hash");
            }
            else if (path.EndsWith("/publish"))
            { var payload = JsonSerializer.Deserialize<GlyphModulePublicationRequest>(await request.Content!.ReadAsStringAsync(), new JsonSerializerOptions(JsonSerializerDefaults.Web))!; ExpectedHash = payload.ExpectedCompilationHash; response = new GlyphCompilationDto(true, []); }
            else if (request.Method == HttpMethod.Delete) { Module = Module with { IsArchived = true }; response = new { }; }
            else if (request.Method == HttpMethod.Put)
            {
                var payload = JsonSerializer.Deserialize<GlyphModuleRequest>(await request.Content!.ReadAsStringAsync(), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
                Module = Module with { SourceText = payload.SourceText ?? Module.SourceText, IsArchived = payload.IsArchived ?? Module.IsArchived }; response = Module;
            }
            else if (path.EndsWith("/versions")) response = Array.Empty<GlyphVersionDto>();
            else if (path.StartsWith("/api/worldengine/glyphs/")) response = new GlyphDefinitionDto(Module.Id, "legacy", null, "InteractionPipeline", "Interaction", "const TEXT = \"using helpers\" glyph t : interaction {}", false, DateTime.UtcNow, DateTime.UtcNow, 1);
            else if (path == "/api/worldengine/glyph-modules") response = new[] { Module };
            else response = Module;
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(response)) };
        }
    }
}
