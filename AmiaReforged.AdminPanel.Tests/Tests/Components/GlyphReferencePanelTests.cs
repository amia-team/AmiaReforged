using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using AmiaReforged.AdminPanel.Models;
using Bunit;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class GlyphReferencePanelTests
{
    private Bunit.TestContext _context = null!;
    private BunitJSModuleInterop _module = null!;
    internal static readonly GlyphFunctionMetadataDto GetTag = Function("nwn.get_tag", "Objects", "Returns the tag of an object.") with
    {
        Source = "NWScript.GetTag", Backend = "NWScript",
        Parameters = [new("object", "Object", "Object", true, null), new("base", "Base", "Bool", false, "false")]
    };
    internal static readonly GlyphConstantMetadataDto Creature = new("OBJECT_TYPE.CREATURE", "OBJECT_TYPE", "Int", 5, "NWScript.OBJECT_TYPE_CREATURE", "Creature objects");
    internal static GlyphLanguageMetadataDto Metadata => new(1,
        [GetTag, GetTag with { Name = "tag", ImplicitArgument = "player", Parameters = [] },
            Function("nwn.set_tag", "Objects", "Changes the tag."), Function("nwn.get_item_stack_size", "Items", "A tag in documentation."),
            Function("industry.level", "Industry", "Industry level.") with { ScriptCategory = "Interaction", Deprecated = "Use industry.rank", AvailableIn = [new("interaction", "completed")] }],
        [new("interaction", "InteractionPipeline", "Interaction", ["tick", "completed"])], [], [],
        [new("get_x", "Location", "nwn.location_x", "Location coordinate", "Float", "Value", [], [new("interaction", "tick")])])
    { Constants = [Creature, new("DAMAGE_TYPE.FIRE", "DAMAGE_TYPE", "Int", 8, "NWScript.DAMAGE_TYPE_FIRE", "Fire damage")], Types = ["Object", "Location"] };
    private static GlyphFunctionMetadataDto Function(string name, string category, string description) =>
        new(name, name, description, "String", "Value", [], null, null, null, null, [new("interaction", "tick"), new("interaction", "completed")]) { Category = category };

    [SetUp] public void Setup()
    {
        _context = new();
        _module = _context.JSInterop.SetupModule("./js/glyph-editor.js?v=6");
        _module.Mode = JSRuntimeMode.Loose;
    }
    [TearDown] public void Cleanup() => _context.Dispose();
    private IRenderedComponent<GlyphReferencePanel> Render() => _context.RenderComponent<GlyphReferencePanel>(p => p.Add(c => c.Metadata, Metadata));
    private static void Search(IRenderedComponent<GlyphReferencePanel> cut, string text) => cut.Find("input[type=search]").Input(text);
    private static string[] Names(IRenderedComponent<GlyphReferencePanel> cut) => cut.FindAll(".glyph-reference-row").Select(r => r.GetAttribute("title")!).ToArray();

    [Test] public void Metadata_renders_categories_and_canonical_apis_without_duplicate_aliases()
    {
        var cut = Render();
        Assert.That(cut.FindAll(".glyph-reference-group").Select(b => b.TextContent), Has.Some.Contains("NWN / Objects"));
        Assert.That(cut.Markup, Does.Contain("World Engine / Industry"));
        Search(cut, "tag");
        Assert.That(Names(cut), Is.EqualTo(new[] { "nwn.get_tag", "nwn.set_tag", "nwn.get_item_stack_size" }));
        Assert.That(Names(cut), Does.Not.Contain("tag"));
    }

    [TestCase("GetTag", "nwn.get_tag")]
    [TestCase("Bool", "nwn.get_tag")]
    [TestCase("object base", "nwn.get_tag")]
    [TestCase("Industry", "industry.level")]
    [TestCase("stack", "nwn.get_item_stack_size")]
    public void Search_matches_source_parameters_category_and_partial_names(string query, string expected)
    {
        var cut = Render(); Search(cut, query);
        Assert.That(Names(cut), Does.Contain(expected));
    }

    [Test] public void Selection_shows_signature_defaults_provenance_aliases_and_availability()
    {
        var cut = Render(); Search(cut, "GetTag"); cut.Find(".glyph-reference-row").Click();
        Assert.That(cut.Find(".glyph-reference-row").GetAttribute("aria-pressed"), Is.EqualTo("true"));
        var detail = cut.Find(".glyph-reference-detail").TextContent;
        Assert.That(detail, Does.Contain("nwn.get_tag(object: Object, base: Bool = false) → String")
            .And.Contain("NWScript.GetTag").And.Contain("Required").And.Contain("Optional, default: false")
            .And.Contain("Available everywhere").And.Contain("Aliases").And.Contain("tag"));
        Assert.That(cut.Find("button.btn-primary").TextContent, Is.EqualTo("Insert"));
        if (Environment.GetEnvironmentVariable("GLYPH_REFERENCE_PREVIEW") is { } path)
            File.WriteAllText(path, cut.Markup);
    }

    [Test] public void Constants_are_collapsed_by_domain_and_selection_shows_actual_type_value_and_source()
    {
        var cut = Render(); cut.FindAll(".glyph-reference-tabs button").Single(b => b.TextContent == "Constants").Click();
        Assert.That(cut.FindAll(".glyph-reference-row"), Is.Empty);
        cut.FindAll(".glyph-reference-group").Single(b => b.TextContent.Contains("OBJECT_TYPE")).Click();
        cut.Find(".glyph-reference-row").Click();
        Assert.That(cut.Find(".glyph-reference-detail").TextContent, Does.Contain("OBJECT_TYPE.CREATURE").And.Contain("Int = 5").And.Contain("NWScript.OBJECT_TYPE_CREATURE"));
        Search(cut, "fire"); Assert.That(Names(cut), Is.EqualTo(new[] { "DAMAGE_TYPE.FIRE" }));
        Search(cut, "OBJECT_TYPE"); Assert.That(Names(cut), Is.EqualTo(new[] { "OBJECT_TYPE.CREATURE" }));
    }

    [Test] public void Unavailable_metadata_supports_retry_and_loading_disables_retry()
    {
        var retries = 0;
        var cut = _context.RenderComponent<GlyphReferencePanel>(p => p.Add(c => c.Unavailable, true).Add(c => c.Retry, () => retries++));
        Assert.That(cut.Markup, Does.Contain("Glyph reference is unavailable"));
        cut.Find("button.btn-secondary").Click(); Assert.That(retries, Is.EqualTo(1));
        cut.SetParametersAndRender(p => p.Add(c => c.Loading, true));
        Assert.That(cut.Find("button.btn-secondary").HasAttribute("disabled"), Is.True);
    }

    [Test] public void Metadata_replacement_clears_stale_selection_and_lists()
    {
        var cut = Render(); Search(cut, "GetTag"); cut.Find(".glyph-reference-row").Click();
        cut.SetParametersAndRender(p => p.Add(c => c.Metadata, (GlyphLanguageMetadataDto?)null));
        Assert.That(cut.Markup, Does.Not.Contain("NWScript.GetTag"));
        cut.SetParametersAndRender(p => p.Add(c => c.Metadata, new GlyphLanguageMetadataDto(1, [], [], [], [], [])));
        Assert.That(cut.FindAll(".glyph-reference-row"), Is.Empty);
        Assert.That(cut.Find(".glyph-reference-detail").TextContent, Does.Contain("Select an entry"));
    }

    [Test] public void Deprecated_and_unavailable_functions_remain_browsable_in_all_contexts()
    {
        var cut = Render(); cut.SetParametersAndRender(p => p.Add(c => c.Context, new GlyphCursorContextDto("interaction", "tick")));
        Search(cut, "industry.level"); cut.Find(".glyph-reference-row").Click();
        Assert.That(cut.Find(".glyph-reference-row").ClassList, Does.Contain("glyph-reference-unavailable"));
        Assert.That(cut.Find(".glyph-reference-detail").TextContent, Does.Contain("Deprecated: Use industry.rank")
            .And.Contain("Unavailable in current context").And.Contain("interaction.completed"));
        cut.SetParametersAndRender(p => p.Add(c => c.Context, new GlyphCursorContextDto("interaction", "completed")));
        Assert.That(cut.Find(".glyph-reference-detail").TextContent, Does.Contain("Available here"));
        cut.Find("input[type=checkbox]").Change(false);
        Assert.That(Names(cut), Does.Contain("industry.level"));
    }

    [Test] public void Keyboard_controls_and_function_and_constant_callbacks_use_canonical_metadata()
    {
        GlyphFunctionMetadataDto? insertedFunction = null; GlyphConstantMetadataDto? insertedConstant = null;
        var cut = _context.RenderComponent<GlyphReferencePanel>(p => p.Add(c => c.Metadata, Metadata).Add(c => c.CanInsert, true)
            .Add(c => c.InsertFunction, f => insertedFunction = f).Add(c => c.InsertConstant, c => insertedConstant = c));
        Assert.That(cut.Find("input[type=search]").GetAttribute("aria-label"), Is.EqualTo("Search reference"));
        Search(cut, "GetTag"); cut.Find("button.glyph-reference-row").Click(); cut.Find("button.btn-primary").Click();
        Assert.That(insertedFunction, Is.SameAs(GetTag));
        Assert.That(insertedFunction!.Parameters.Count, Is.EqualTo(2));
        cut.Find("button.glyph-reference-row").DoubleClick(); Assert.That(insertedFunction, Is.SameAs(GetTag));
        cut.FindAll(".glyph-reference-tabs button").Single(b => b.TextContent == "Constants").Click();
        Search(cut, "CREATURE"); cut.Find("button.glyph-reference-row").DoubleClick();
        Assert.That(insertedConstant, Is.SameAs(Creature));
    }

    [Test] public void Large_domains_render_bounded_results_and_more_is_explicit()
    {
        var cut = Render();
        cut.SetParametersAndRender(p => p.Add(c => c.Metadata, Metadata with { Constants = Enumerable.Range(0, 3000)
            .Select(i => new GlyphConstantMetadataDto($"VFX.VALUE_{i}", "VFX", "Int", i, "NWScript", "Effect")).ToArray() }));
        cut.FindAll(".glyph-reference-tabs button").Single(b => b.TextContent == "Constants").Click();
        Assert.That(cut.FindAll(".glyph-reference-row"), Is.Empty);
        Search(cut, "VFX"); Assert.That(cut.FindAll(".glyph-reference-row").Count, Is.EqualTo(80));
        cut.Find(".glyph-reference-more").Click(); Assert.That(cut.FindAll(".glyph-reference-row").Count, Is.EqualTo(160));
    }

    [Test] public async Task Browser_selection_callbacks_reject_stale_metadata_generations()
    {
        var cut = Render();
        var module = _module;
        var generation = (int)module.Invocations["setReferenceSearch"].Last().Arguments[2]!;
        await cut.InvokeAsync(() => cut.Instance.OnReferenceSelected("Functions", "nwn.get_tag", false, generation));
        Assert.That(cut.Find(".glyph-reference-detail").TextContent, Does.Contain("NWScript.GetTag"));
        cut.SetParametersAndRender(p => p.Add(c => c.Metadata, Metadata with { Functions = [GetTag with { Description = "New server documentation" }] }));
        await cut.InvokeAsync(() => cut.Instance.OnReferenceSelected("Functions", "nwn.get_tag", false, generation));
        Assert.That(cut.Find(".glyph-reference-detail").TextContent, Does.Contain("Select an entry"));
        var nextGeneration = (int)module.Invocations["setReferenceSearch"].Last().Arguments[2]!;
        await cut.InvokeAsync(() => cut.Instance.OnReferenceSelected("Functions", "nwn.get_tag", false, nextGeneration));
        Assert.That(cut.Find(".glyph-reference-detail").TextContent, Does.Contain("New server documentation"));
    }

    [Test] public void Unrestricted_domain_functions_share_the_metadata_derived_World_Engine_family()
    {
        var cut = _context.RenderComponent<GlyphReferencePanel>(p => p.Add(c => c.Metadata, Metadata with
        {
            Functions = [Function("has_trait", "Traits", "Checks a character trait."),
                Function("get_creature_traits", "Traits", "Lists character traits.") with { ScriptCategory = "Trait" },
                Function("scalar_equal", "Math / Logic", "Scalar equality.")]
        }));
        Assert.That(cut.Markup, Does.Contain("World Engine / Traits").And.Contain("Glyph standard library / Math / Logic"));
        Assert.That(cut.Markup, Does.Not.Contain("Glyph standard library / Traits"));
    }

    [Test] public void Typed_members_and_types_have_separate_sections()
    {
        var cut = Render(); cut.FindAll(".glyph-reference-tabs button").Single(b => b.TextContent == "Members").Click();
        cut.Find(".glyph-reference-row").Click();
        Assert.That(cut.Find(".glyph-reference-detail").TextContent, Does.Contain("Location.get_x() → Float").And.Contain("nwn.location_x"));
        cut.FindAll(".glyph-reference-tabs button").Single(b => b.TextContent == "Types").Click();
        Assert.That(Names(cut), Is.EqualTo(new[] { "Location", "Object" }));
    }
}
