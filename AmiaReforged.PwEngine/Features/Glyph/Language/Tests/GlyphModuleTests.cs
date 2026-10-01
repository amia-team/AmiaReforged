using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Language.Modules;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public sealed class GlyphModuleTests
{
    private GlyphBootstrap _runtime = null!;
    [SetUp] public void Setup() => _runtime = new(new GlyphNodeDefinitionRegistry());
    private GlyphModuleRevision Module(string name, string declarations) => GlyphModuleRevision.Create(name, $"mod {name} {{ {declarations} }}");
    private void Install(params GlyphModuleRevision[] revisions) => _runtime.Compiler.Modules.Replace(new(revisions));
    private GlyphCompilationResult Compile(string prelude, string body) => _runtime.Compiler.Compile(prelude + " glyph t : interaction { attempted { " + body + " } }");
    private static void Valid(GlyphCompilationResult result) => Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));

    [Test] public void Statement_module_functions_validate_unused_bodies_and_preserve_private_helpers()
    {
        var good = Module("helpers", "fn tag(): String { return \"module\" } pub fn target(): String { return tag() }");
        Assert.That(_runtime.Compiler.CompileModule(good).Success, Is.True);
        Install(good);
        Valid(Compile("using helpers", "fail target()"));
        var bad = Module("bad", "pub fn unused(): Int { if true { return 1 } }");
        Assert.That(_runtime.Compiler.CompileModule(bad).Diagnostics.Any(d => d.Code == "GLYPH2030"), Is.True);
    }
    [Test] public void Version_three_scripts_import_version_two_modules_but_older_consumers_reject_newer_modules()
    {
        var old = Module("old", "pub fn value(): String = \"old\"") with { LanguageVersion = 2 };
        Install(old);
        Valid(Compile("using old", "fail value()"));
        Install(Module("newer", "pub fn value(): String { return \"new\" }"));
        var result = _runtime.Compiler.Compile("using newer glyph t : interaction { attempted { fail value() } }", new(LanguageVersion: 2));
        Assert.That(result.Diagnostics.Any(d => d.Code == "GLYPH1007"), Is.True);
    }
    [Test] public void Statement_function_availability_includes_body_actions()
    {
        var revision = Module("helpers", "pub fn count(): Int { var value = spawn.count return value }");
        var result = _runtime.Compiler.CompileModule(revision);
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
        Install(revision);
        Assert.That(Compile("using helpers", "let n = count()").Success, Is.False);
        var metadata = GlyphModuleMetadata.Create(_runtime.Compiler, revision.SourceText);
        Assert.That(metadata.Diagnostics, Is.Empty);
        Assert.That(metadata.Functions.Single(f => f.Name == "count").AvailableIn.All(a => a.Event.StartsWith("encounter.")), Is.True);
    }

    [Test] public void Public_function_keeps_its_private_helpers_and_does_not_capture_caller_names()
    {
        var helpers = Module("helpers", "const TAG = \"module\" fn tag(): String = TAG pub fn target(): String = tag()");
        Install(helpers);
        Assert.That(_runtime.Compiler.CompileModule(helpers).Success, Is.True);
        var result = Compile("using helpers const TAG = \"caller\" fn tag(): String = TAG", "fail target()");
        Valid(result);
        Assert.That(result.Executable!.CreateExecutionGraph().Nodes.Any(n => n.PropertyOverrides.Values.Contains("module")), Is.True);
        Assert.That(result.Executable.DependencyLock.Single().RevisionId, Is.EqualTo(helpers.RevisionId));
    }
    [TestCase("helpers.tag()")]
    [TestCase("helpers.TAG")]
    [TestCase("tag()")]
    public void Private_declarations_are_not_imported(string expression)
    {
        Install(Module("helpers", "const TAG = \"private\" fn tag(): String = TAG pub const PUBLIC = \"public\""));
        Assert.That(Compile("using helpers", "fail " + expression).Success, Is.False);
    }
    [Test] public void Qualified_names_resolve_ambiguous_exports()
    {
        Install(Module("a", "pub const VALUE = \"a\""), Module("b", "pub const VALUE = \"b\""));
        Assert.That(Compile("using a using b", "fail VALUE").Diagnostics.Any(d => d.Code == "GLYPH2020"), Is.True);
        Valid(Compile("using a using b", "if a.VALUE == b.VALUE { fail a.VALUE }"));
    }
    [Test] public void Local_names_keep_their_meaning_and_imports_still_have_qualified_names()
    {
        Install(Module("a", "pub const VALUE = \"a\""));
        Valid(Compile("using a const VALUE = \"local\"", "if VALUE == a.VALUE { fail VALUE }"));
    }
    [Test] public void Public_structs_ADTs_and_qualified_type_signatures_keep_nominal_identity()
    {
        var a = Module("a", "pub struct Item { actor: Object } pub type Result { Found { item: Item } Missing {} } pub fn wrap(actor: Object): Result = Result.Found(Item(actor))");
        var b = Module("b", "pub struct Item { actor: Object }");
        Install(a, b);
        var validation = _runtime.Compiler.CompileModule(a);
        Assert.That(validation.Success, Is.True, string.Join("\n", validation.Diagnostics));
        Valid(Compile("using a using b fn actor(item: a.Item): Object = item.actor", "var result = a.wrap(context.creature) match result { a.Result.Found { item } { nwn.set_local_int(actor(item), \"seen\", 1) } a.Result.Missing {} {} }"));
        Assert.That(Compile("using a using b", "var item = a.Item(context.creature) item = b.Item(context.creature)").Success, Is.False);
    }
    [Test] public void Public_APIs_cannot_expose_private_types()
    {
        var revision = Module("a", "struct Hidden { actor: Object } pub fn leak(actor: Object): Hidden = Hidden(actor)");
        Assert.That(_runtime.Compiler.CompileModule(revision).Diagnostics.Any(d => d.Code == "GLYPH2026"), Is.True);
    }
    [Test] public void Publication_validates_unused_functions_and_rejects_recursion()
    {
        var invalid = Module("a", "pub const OK = 1 fn broken(): Int = nonexistent()");
        Assert.That(_runtime.Compiler.CompileModule(invalid).Success, Is.False);
        var recursive = Module("a", "fn one(): Int = two() fn two(): Int = one() pub const OK = 1");
        Assert.That(_runtime.Compiler.CompileModule(recursive).Diagnostics.Any(d => d.Code == "GLYPH2012"), Is.True);
    }
    [Test] public void Transitive_imports_are_private_to_the_importing_module()
    {
        var b = Module("b", "pub const TEXT = \"b\"");
        var a = Module("a", "using b pub fn text(): String = TEXT");
        Install(a, b);
        Valid(Compile("using a", "fail text()"));
        Assert.That(Compile("using a", "fail b.TEXT").Success, Is.False);
        Valid(Compile("using a using b", "fail b.TEXT"));
    }
    [Test] public void Missing_modules_and_import_cycles_are_diagnosed()
    {
        Assert.That(Compile("using missing", "").Diagnostics.Any(d => d.Code == "GLYPH2025"), Is.True);
        Install(Module("a", "using b pub const X = 1"), Module("b", "using a pub const Y = 2"));
        Assert.That(Compile("using a", "").Diagnostics.Any(d => d.Code == "GLYPH2022"), Is.True);
    }
    [Test] public void Intrinsic_context_restrictions_survive_module_wrappers()
    {
        var revision = Module("a", "pub fn count(): Int = spawn.count");
        Install(revision);
        var result = _runtime.Compiler.CompileModule(revision);
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
        Assert.That(result.Availability["a.count"].Any(s => s.Event == "encounter.before_group_spawn"), Is.True);
        Assert.That(Compile("using a", "let x = count()").Success, Is.False);
    }
    [Test] public void Locked_revisions_survive_updates_and_restore_without_latest_modules()
    {
        var old = Module("a", "pub const TEXT = \"old\""); Install(old);
        var first = Compile("using a", "fail TEXT"); Valid(first);
        var fresh = Module("a", "pub const TEXT = \"new\""); Install(fresh);
        var second = Compile("using a", "fail TEXT"); Valid(second);
        Assert.That(first.SourceHash, Is.EqualTo(second.SourceHash));
        Assert.That(first.Executable!.CompilationHash, Is.Not.EqualTo(second.Executable!.CompilationHash));
        GlyphRuntimeRegistry programs = new(); Guid id = Guid.NewGuid();
        var version = new GlyphProgramVersion(Guid.NewGuid(), id, DateTime.UtcNow, null, first.Executable);
        var definition = new GlyphDefinition { Id = id, IsActive = true, SourceText = "invalid draft", PublishedVersionsJson = GlyphPublishedVersion.Serialize([version]) };
        Install(); GlyphPublishedVersion.Restore(definition, _runtime.Compiler, programs);
        Assert.That(programs.GetActive(id)!.Executable.CompilationHash, Is.EqualTo(first.Executable.CompilationHash));
    }
    [Test] public void Conflicting_transitive_revisions_cannot_be_mixed()
    {
        var c1 = Module("c", "pub const TEXT = \"one\""); var c2 = Module("c", "pub const TEXT = \"two\"");
        var a = Module("a", "using c pub fn text(): String = TEXT") with { Imports = [new("c", c1.RevisionId, c1.SourceHash)] };
        var b = Module("b", "using c pub fn text(): String = TEXT") with { Imports = [new("c", c2.RevisionId, c2.SourceHash)] };
        _runtime.Compiler.Modules.Replace(new([a, b, c1, c2], [a.RevisionId, b.RevisionId, c2.RevisionId]));
        Assert.That(Compile("using a using b", "").Diagnostics.Any(d => d.Code == "GLYPH2023"), Is.True);
    }
    [Test] public void Version_one_identifiers_and_sources_remain_supported()
    {
        Valid(_runtime.Compiler.Compile("const mod = 1 glyph t : interaction { attempted { let using = mod let pub = using } }", new(LanguageVersion: 1)));
        Assert.That(_runtime.Compiler.Compile("using a glyph t : interaction {}", new(LanguageVersion: 1)).Success, Is.False);
    }
    [Test, Timeout(3000)] public void Malformed_module_fields_report_errors_without_hanging()
    {
        var revision = Module("a", "pub struct Item { pub actor: Object }");
        Assert.That(_runtime.Compiler.CompileModule(revision).Success, Is.False);
    }
    [Test] public void Deep_constant_chains_and_function_expansion_have_binding_budgets()
    {
        string declarations = string.Join("\n", Enumerable.Range(0, 140).Select(i => $"const C{i} = C{i + 1}")) + "\nconst C140 = 1";
        Assert.That(_runtime.Compiler.CompileModule(Module("a", declarations)).Diagnostics.Any(d => d.Code == "GLYPH1007"), Is.True);
        string functions = "fn f0(): Float = 1.0\n" + string.Join("\n", Enumerable.Range(1, 20).Select(i => $"fn f{i}(): Float = f{i - 1}() + f{i - 1}()"));
        var revision = Module("a", functions + "\npub fn result(): Float = f19()");
        Install(revision);
        Assert.That(Compile("using a", "let x = result()").Diagnostics.Any(d => d.Code == "GLYPH1007"), Is.True);
    }

    [Test] public void Imported_names_cannot_redirect_standard_namespaces_and_builtin_types_are_reserved()
    {
        var module = Module("helpers", "pub fn nwn(): String = \"local\" pub const OBJECT = \"object\" pub fn tag(actor: Object): String = nwn.get_tag(actor)");
        Install(module);
        Valid(Compile("using helpers", "let tag = nwn.get_tag(context.creature) if context.creature == OBJECT.INVALID { fail helpers.nwn() }"));
        Assert.That(_runtime.Compiler.CompileModule(module).Success, Is.True);
        Assert.That(_runtime.Compiler.CompileModule(Module("helpers", "pub struct Object { actor: Int }")).Diagnostics.Any(d => d.Code == "GLYPH2006"), Is.True);
    }

}
