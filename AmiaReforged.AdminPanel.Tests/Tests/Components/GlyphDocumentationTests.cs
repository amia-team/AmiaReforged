using System.Text.Json;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using AmiaReforged.AdminPanel.Models;
using Bunit;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class GlyphDocumentationTests
{
    private static GlyphDocumentationPackDto Pack => JsonSerializer.Deserialize<GlyphDocumentationPackDto>(
        File.ReadAllText(Path.Combine(NUnit.Framework.TestContext.CurrentContext.TestDirectory, "Fixtures/lexicon.json")),
        new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    [Test] public void Real_Lexicon_content_is_joined_by_native_source_and_rendered_with_provenance()
    {
        using var context = new Bunit.TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var function = GlyphReferencePanelTests.GetTag with
        { Parameters = [new("object", "Object", "Object", true, null) { SourceParameter = "oObject" }] };
        var metadata = GlyphReferencePanelTests.Metadata with { Documentation = Pack, Functions = [function, function with { Name = "tag" }] };
        var catalog = new GlyphReferenceCatalog(metadata);
        Assert.That(catalog.Entries.Count(e => e.Tab == "Functions"), Is.EqualTo(1));
        var entry = catalog.Entries.Single(e => e.Tab == "Functions");
        Assert.That(entry.Rank("empty string"), Is.GreaterThanOrEqualTo(0));
        var cut = context.RenderComponent<GlyphFunctionReference>(p => p.Add(c => c.Entry, entry).Add(c => c.Metadata, metadata));
        Assert.That(cut.Find(".glyph-reference-parameters").TextContent, Does.Contain("Target object."));
        var detail = cut.Find(".glyph-lexicon-reference").TextContent;
        Assert.That(detail, Does.Contain("empty string").And.Contain("Daniel Beckman").And.Contain("57468")
            .And.Contain("GFDL-1.1-or-later").And.Contain("BioWare"));
        Assert.That(cut.FindAll("summary").Select(s => s.TextContent), Does.Contain("Example (NWScript)"));
        Assert.That(cut.Find("a").GetAttribute("href"), Is.EqualTo(Pack.Functions["NWScript.GetTag"].SourceUrl));
        var previewPath = Environment.GetEnvironmentVariable("GLYPH_DOCUMENTATION_DETAIL_PREVIEW");
        if (previewPath != null) File.WriteAllText(previewPath, cut.Markup);
    }

    [Test] public void Native_docs_keep_commands_adapters_and_missing_articles_explicit_and_HTML_inert()
    {
        using var context = new Bunit.TestContext();
        var pack = Pack;
        var article = pack.Functions["NWScript.ActionAttack"] with
        { Sections = [new("Description", [new("text", "<script>evil()</script>")])], OriginalSource = "<img src=x onerror=evil()>" };
        var function = GlyphReferencePanelTests.GetTag with { Source = "NWScript.ActionAttack", Backend = "NWScript.AssignCommand" };
        var cut = context.RenderComponent<GlyphLexiconReference>(p => p.Add(c => c.Article, article).Add(c => c.Pack, pack).Add(c => c.Function, function));
        Assert.That(cut.Markup, Does.Contain("explicit").And.Contain("AssignCommand"));
        Assert.That(cut.FindAll("script, img"), Is.Empty);
        Assert.That(cut.Markup, Does.Contain("&lt;script&gt;"));
        cut.SetParametersAndRender(p => p.Add(c => c.Function, function with { Backend = "NWScript adapter" }));
        Assert.That(cut.Markup, Does.Contain("adapter can change native inputs or results"));
        var missing = function with { Source = "NWScript.EffectPacified" };
        var metadata = GlyphReferencePanelTests.Metadata with { Documentation = pack, Functions = [missing] };
        var reference = context.RenderComponent<GlyphFunctionReference>(p => p
            .Add(c => c.Entry, new GlyphReferenceCatalog(metadata).Entries.Single(e => e.Tab == "Functions"))
            .Add(c => c.Metadata, metadata));
        Assert.That(reference.Markup, Does.Contain("No Lexicon article"));
    }

    [Test] public async Task Editor_documentation_callbacks_validate_the_current_catalog_and_preserve_source()
    {
        using var context = new Bunit.TestContext();
        var module = context.JSInterop.SetupModule("./js/glyph-editor.js?v=7"); module.Mode = JSRuntimeMode.Loose;
        module.Setup<GlyphCodeEditor.EditorSnapshot>("capture", _ => true).SetResult(new("original", 0));
        string? selected = null;
        var cut = context.RenderComponent<GlyphCodeEditor>(p => p.Add(c => c.Metadata, GlyphReferencePanelTests.Metadata)
            .Add(c => c.InitialSource, "original").Add(c => c.DocumentationRequested, name => selected = name));
        await cut.InvokeAsync(() => cut.Instance.OnDocumentationRequested("nwn.get_tag"));
        Assert.That(selected, Is.EqualTo("nwn.get_tag"));
        Assert.That(await cut.InvokeAsync(() => cut.Instance.CaptureAsync()), Is.EqualTo("original"));
        selected = null;
        cut.SetParametersAndRender(p => p.Add(c => c.Metadata, new GlyphLanguageMetadataDto(1, [], [], [], [], [])));
        await cut.InvokeAsync(() => cut.Instance.OnDocumentationRequested("nwn.get_tag"));
        Assert.That(selected, Is.Null);
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());
        await cut.InvokeAsync(() => cut.Instance.OnDocumentationRequested("nwn.get_tag"));
        Assert.That(selected, Is.Null);
    }
}
