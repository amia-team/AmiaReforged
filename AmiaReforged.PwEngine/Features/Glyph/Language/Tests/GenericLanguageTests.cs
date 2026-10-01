using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Language.Modules;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public class GenericLanguageTests
{
    private GlyphBootstrap _runtime = null!;
    private ProbeModule _probe = null!;
    [SetUp] public void Setup() { _probe = new(); _runtime = new(new GlyphNodeDefinitionRegistry(), [_probe]); }
    private static string Script(string body, string prelude = "") => prelude + " glyph t : encounter.before_group_spawn { " + body + " }";
    private async Task Run(string body, string prelude = "")
    {
        var compilation = _runtime.Compiler.Compile(Script(body, prelude));
        Assert.That(compilation.Diagnostics, Is.Empty, string.Join("\n", compilation.Diagnostics));
        var context = new GlyphExecutionContext { Graph = compilation.Executable!.CreateExecutionGraph(), EnableTracing = true };
        Assert.That(await _runtime.Interpreter.ExecuteAsync(context), Is.True, string.Join("\n", context.TraceLog));
    }

    [Test] public void checked_nwn_object_wrapper_compiles_with_native_validity_checks()
    {
        var result = _runtime.Compiler.Compile("""
            fn find_object(tag: String): Option<Object> {
                let object = nwn.get_object_by_tag(tag)
                if nwn.get_is_object_valid(object) { return Option<Object>.Some(value: object) }
                return Option<Object>.None()
            }
            glyph lookup : interaction { completed {
                match find_object("quest_target") {
                    Some { value } { if nwn.get_is_object_valid(value) { message(value, "Found you") } }
                    None {} { message(player, "Target not found") }
                }
            } }
            """);
        Assert.That(result.Diagnostics, Is.Empty, string.Join("\n", result.Diagnostics));
    }

    [Test] public async Task generic_structs_and_adts_preserve_nested_runtime_types()
    {
        await Run("""
            var box = Box<Int>(value: 7)
            var outer = Box<Box<Int>>(value: box)
            let result = Result<Box<Int>, String>.Ok(value: outer.value)
            match result { Ok { value } { record(value.value) } Err { error } { record(0) } }
            var absent = Option<Box<Int>>.None()
            match absent { Some { value } { record(value.value) } None {} { record(2) } }
            absent = Option<Box<Int>>.Some(value: box)
            match absent { Option<Box<Int>>.Some { value } { record(value.value) } Option<Box<Int>>.None {} {} }
            """, "struct Box<T> { value: T, } type Result<T, E> { Ok { value: T, }, Err { error: E, }, }");
        Assert.That(_probe.Values, Is.EqualTo(new[] { 7, 2, 7 }));
    }

    [Test] public async Task generic_functions_infer_types_and_evaluate_arguments_once_in_source_order()
    {
        await Run("""
            record(identity(next()))
            record(identity<Int>(next()))
            let pair = make_pair(right: next(), left: next())
            record(pair.left) record(pair.right)
            match wrap(identity("text")) { Some { value } { if value == "text" { record(9) } } None {} {} }
            """, """
            struct Pair<T> { left: T, right: T, }
            fn identity<T>(value: T): T = value
            fn make_pair<T>(left: T, right: T): Pair<T> = Pair<T>(left: left, right: right)
            fn wrap<T>(value: T): Option<T> { return Option<T>.Some(value: identity(value)) }
            """);
        Assert.That(_probe.Calls, Is.EqualTo(4));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 2, 4, 3, 9 }));
    }

    [Test] public async Task generic_functions_support_collection_arguments_and_option_payloads()
    {
        await Run("""
            let list = append([2, 3], 4)
            match first(list) { Some { value } { record(value) } None {} { record(0) } }
            match first(List<Int>()) { Some { value } { record(value) } None {} { record(8) } }
            match Option<List<Int>>.Some(value: list) { Some { value } { record(value.count()) } None {} {} }
            record(lookup(Dictionary<String, Int>().with("x", 6), "x", 0))
            """, """
            fn append<T>(values: List<T>, value: T): List<T> = values.append(value)
            fn first<T>(values: List<T>): Option<T> {
                if values.count() > 0 { return Option<T>.Some(value: values[0]) }
                return Option<T>.None()
            }
            fn lookup<K, V>(values: Dictionary<K, V>, key: K, fallback: V): V = values.get(key, fallback)
            """);
        Assert.That(_probe.Values, Is.EqualTo(new[] { 2, 8, 3, 6 }));
    }

    [Test] public async Task generic_impl_methods_specialize_self_and_associated_constructors()
    {
        await Run("""
            let box = Box<Int>.create(next())
            record(box.get()) record(box.get())
            match box.optional() { Some { value } { record(value) } None {} {} }
            let replacement = box.replace<String>("new")
            if replacement.get() == "new" { record(7) }
            """, """
            struct Box<T> { value: T, }
            impl<T> Box<T> {
                fn create(value: T): Self = Box<T>(value: value)
                fn get(self): T = self.value
                fn optional(self): Option<T> = Option<T>.Some(value: self.value)
                fn replace<U>(self, value: U): Box<U> = Box<U>(value: value)
            }
            """);
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 1, 1, 7 }));
        Assert.That(_probe.Calls, Is.EqualTo(1));
    }

    [Test] public async Task generic_inference_handles_nested_nominal_arguments()
    {
        await Run("let box = Box<Option<Int>>(value: Option<Int>.Some(value: 12)) match extract(box) { Some { value } { record(value) } None {} {} }", """
            struct Box<T> { value: T, }
            fn extract<T>(box: Box<Option<T>>): Option<T> = box.value
            """);
        Assert.That(_probe.Values, Is.EqualTo(new[] { 12 }));
    }

    [Test] public async Task some_records_presence_without_promising_object_lifetime()
    {
        await Run("""
            let target = checked_object(object_handle())
            destroy_object()
            match target {
                Some { value } { if object_exists(value) { record(1) } else { record(2) } }
                None {} { record(3) }
            }
            match checked_object(object_handle()) { Some { value } { record(4) } None {} { record(5) } }
            """, """
            fn checked_object(value: Object): Option<Object> {
                if object_exists(value) { return Option<Object>.Some(value: value) }
                return Option<Object>.None()
            }
            """);
        Assert.That(_probe.Values, Is.EqualTo(new[] { 2, 5 }));
    }

    [TestCase("struct Box<T> { value: T, }", "let x = Box<Int, String>(value: 1)", "GLYPH2004")]
    [TestCase("struct Box<T> { value: T, }", "let x = Box(value: 1)", "GLYPH2004")]
    [TestCase("struct Box<T> { value: T, }", "let x = Box<Int>(value: \"wrong\")", "GLYPH2004")]
    [TestCase("struct Box<T> { value: T, }", "var x = Box<Int>(value: 1) x = Box<String>(value: \"wrong\")", "GLYPH2004")]
    [TestCase("struct Box<T> { value: T, } impl Box<Int> {}", "", "GLYPH2031")]
    [TestCase("struct Box<T> { value: T, } impl<T> Box<T, Int> {}", "", "GLYPH2004")]
    [TestCase("struct Box<T, T> { value: T, }", "", "GLYPH2006")]
    [TestCase("struct Box<Int> { value: Int, }", "", "GLYPH2006")]
    [TestCase("struct Box<T> { value: Unknown, }", "", "GLYPH3002")]
    [TestCase("", "let x = Option<Void>.None()", "GLYPH2004")]
    [TestCase("", "let x = Int<String>()", "GLYPH2002")]
    [TestCase("", "let x = Option<Int>.Missing()", "GLYPH2010")]
    [TestCase("", "match Option<Int>.None() { Some { value } {} }", "GLYPH2010")]
    [TestCase("", "match Option<Int>.None() { Option<String>.Some { value } {} None {} {} }", "GLYPH2010")]
    [TestCase("fn identity<T>(value: T): T = value", "let x = identity<Int>(\"wrong\")", "GLYPH2004")]
    [TestCase("fn pair<T>(a: T, b: T): T = a", "let x = pair(1, \"wrong\")", "GLYPH2004")]
    [TestCase("fn empty<T>(): Option<T> = Option<T>.None()", "let x = empty()", "GLYPH2004")]
    [TestCase("fn identity<T>(value: T): T = value", "let x = identity<Int, String>(1)", "GLYPH2004")]
    [TestCase("fn identity<T>(value: T): T = 3", "", "GLYPH2004")]
    [TestCase("fn broken<T>(value: T): T = missing(value)", "", "GLYPH2002")]
    [TestCase("fn cycle<T>(value: T): T = cycle(value)", "", "GLYPH2012")]
    [TestCase("type Expanding<T> { Next { value: Expanding<Option<T>>, }, }", "", "GLYPH1007")]
    public void invalid_generics_report_diagnostics(string prelude, string body, string code)
    {
        var result = _runtime.Compiler.Compile(Script(body, prelude));
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Any(d => d.Code == code), Is.True, string.Join("\n", result.Diagnostics));
    }

    [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]
    public void older_versions_keep_option_names_and_reject_generic_declarations(int version)
    {
        Assert.That(_runtime.Compiler.Compile(Script("var x = Option(value: 3) record(x.value)", "struct Option { value: Int, }"), new(LanguageVersion: version)).Success, Is.True);
        Assert.That(_runtime.Compiler.Compile(Script("", "struct Box<T> { value: T, }"), new(LanguageVersion: version)).Diagnostics.Any(d => d.Code == "GLYPH1013"), Is.True);
    }

    [Test] public async Task imported_generic_types_and_functions_resolve_their_lexical_module()
    {
        var module = GlyphModuleRevision.Create("boxes", """
            mod boxes {
                pub struct Box<T> { value: T, }
                pub fn wrap<T>(value: T): Box<Option<T>> = Box<Option<T>>(value: Option<T>.Some(value: value))
                impl<T> Box<T> { pub fn get(self): T = self.value }
            }
            """);
        var validation = _runtime.Compiler.CompileModule(module);
        Assert.That(validation.Diagnostics, Is.Empty, string.Join("\n", validation.Diagnostics));
        _runtime.Compiler.Modules.Replace(new([module]));
        await Run("let box = boxes.wrap(6) match box.get() { Some { value } { record(value) } None {} {} }", "using boxes");
        Assert.That(_probe.Values, Is.EqualTo(new[] { 6 }));
    }

    [Test] public void public_generic_signatures_cannot_hide_private_types_in_arguments()
    {
        var module = GlyphModuleRevision.Create("hidden", "mod hidden { struct Secret {} pub fn expose<T>(value: T): Option<Secret> = Option<Secret>.None() }");
        var result = _runtime.Compiler.CompileModule(module);
        Assert.That(result.Diagnostics.Any(d => d.Code == "GLYPH2026"), Is.True, string.Join("\n", result.Diagnostics));
    }

    [Test] public void generic_type_arguments_obey_module_privacy_at_constructor_calls()
    {
        var module = GlyphModuleRevision.Create("hidden", "mod hidden { struct Secret {} pub const VALUE = 1 }");
        _runtime.Compiler.Modules.Replace(new([module]));
        var result = _runtime.Compiler.Compile(Script("let x = Option<hidden.Secret>.None()", "using hidden"));
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Any(d => d.Code == "GLYPH2021"), Is.True, string.Join("\n", result.Diagnostics));
    }

    [Test] public void generic_metadata_preserves_template_parameters_and_nominal_names()
    {
        var metadata = GlyphModuleMetadata.Create(_runtime.Compiler, Script("", "struct Box<T> { value: T, } fn wrap<T>(value: T): Box<Option<T>> = Box<Option<T>>(value: Option<T>.Some(value: value))"));
        Assert.That(metadata.Diagnostics, Is.Empty, string.Join("\n", metadata.Diagnostics));
        Assert.That(metadata.Types, Does.Contain("Box<T>"));
        Assert.That(metadata.Aggregates.Single(a => a.Name == "Box").TypeParameters, Is.EqualTo(new[] { "T" }));
        Assert.That(metadata.Functions.Single(f => f.Name == "wrap").ReturnType, Is.EqualTo("Box<Option<T>>"));
        Assert.That(_runtime.LanguageMetadata.Types, Does.Contain("Option<T>"));
        Assert.That(_runtime.LanguageMetadata.Functions.Single(f => f.Name == "Option.Some").TypeParameters, Is.EqualTo(new[] { "T" }));
    }

    private sealed class ProbeModule : IGlyphModule
    {
        public List<int> Values { get; } = [];
        public int Calls;
        private bool _exists = true;
        public void Configure(GlyphModuleBuilder glyph)
        {
            glyph.Add(new Probe("record", [Pins.InInt("value", "Value")], null, async cx => { Values.Add(await cx.InInt("value")); return null; }, true));
            glyph.Add(new Probe("next", [], Pins.Out("value", "Value", GlyphDataType.Int), _ => Task.FromResult<object?>(++Calls), true));
            glyph.Add(new Probe("object_handle", [], Pins.Out("value", "Value", GlyphDataType.NwObject), _ => Task.FromResult<object?>(42u)));
            glyph.Add(new Probe("object_exists", [Pins.In("object", "Object", GlyphDataType.NwObject)], Pins.Out("value", "Value", GlyphDataType.Bool), _ => Task.FromResult<object?>(_exists)));
            glyph.Add(new Probe("destroy_object", [], null, _ => { _exists = false; return Task.FromResult<object?>(null); }, true));
        }
    }
    private sealed class Probe(string name, List<GlyphPin> inputs, GlyphPin? result, Func<GlyphNodeContext, Task<object?>> run, bool action = false) : GlyphNodeBase
    {
        public override string TypeId => "generic_probe." + name;
        public override GlyphNodeDefinition CreateDefinition() => new GlyphIntrinsicDescriptor
        { TypeId = TypeId, DisplayName = name, Category = "Tests", Archetype = action ? GlyphNodeArchetype.Action : GlyphNodeArchetype.PureFunction,
            Parameters = inputs, Results = result == null ? [] : [result], Exports = [new(name, result?.Id)] }.CreateDefinition();
        public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
        {
            object? output = await run(cx);
            return result == null ? GlyphNodeResult.Continue("exec_out") : action
                ? new GlyphNodeResult { NextExecPinId = "exec_out", OutputValues = new() { [result.Id] = output } } : GlyphNodeResult.Data(new() { [result.Id] = output });
        }
    }
}
