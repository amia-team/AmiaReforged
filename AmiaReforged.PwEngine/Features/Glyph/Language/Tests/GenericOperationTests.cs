using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Language.Modules;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public sealed class GenericOperationTests
{
    private GlyphBootstrap _runtime = null!;
    private ProbeModule _probe = null!;
    [SetUp] public void Setup() { _probe = new(); _runtime = new(new GlyphNodeDefinitionRegistry(), [_probe]); }
    private static string Script(string body, string declarations = "") => declarations + " glyph t : encounter.before_group_spawn { " + body + " }";
    private async Task<GlyphExecutionContext> Run(string body, string declarations = "", bool success = true, int size = 10000, int allocations = 100000, int steps = 10000)
    {
        var result = _runtime.Compiler.Compile(Script(body, declarations));
        Assert.That(result.Diagnostics, Is.Empty, string.Join("\n", result.Diagnostics));
        var context = new GlyphExecutionContext { Graph = result.Executable!.CreateExecutionGraph(), EnableTracing = true,
            MaxCollectionSize = size, MaxCollectionAllocations = allocations, MaxExecutionSteps = steps };
        Assert.That(await _runtime.Interpreter.ExecuteAsync(context), Is.EqualTo(success), string.Join("\n", context.TraceLog));
        return context;
    }
    [Test] public async Task strings_split_preserving_empty_entries_and_use_ordinal_membership()
    {
        await Run("""
            let parts = " a,,B,".split(",")
            record_int(parts.count())
            for part in parts { record_string(part) }
            record_int("😀".length())
            record_bool("Alpha".contains("alpha"))
            record_bool("".contains(""))
            record_int("".split(",").count())
            record_int("a--b--".split("--").count())
            """);
        Assert.That(_probe.Strings, Is.EqualTo(new[] { " a", "", "B", "" }));
        Assert.That(_probe.Ints, Is.EqualTo(new[] { 4, 2, 1, 3 }));
        Assert.That(_probe.Bools, Is.EqualTo(new[] { false, true }));
    }
    [Test] public async Task set_operations_deduplicate_preserve_order_and_leave_sources_immutable()
    {
        await Run("""
            let a = [3, 1, 1, 2]
            let b = [2, 4, 4, 3]
            for n in a.union(b) { record_int(n) }
            for n in a.intersection(b) { record_int(n) }
            for n in a.difference(b) { record_int(n) }
            for n in a.complement(b) { record_int(n) }
            record_int(a.count()) record_int(b.count())
            record_bool(a.any(1)) record_bool(a.any(9))
            record_bool(List<Int>().any())
            """);
        Assert.That(_probe.Ints, Is.EqualTo(new[] { 3, 1, 2, 4, 3, 2, 1, 4, 4, 4 }));
        Assert.That(_probe.Bools, Is.EqualTo(new[] { true, false, false }));
    }
    [Test] public async Task pipelines_infer_map_result_and_capture_values_at_construction()
    {
        await Run("""
            var minimum = 1
            var source = ["a", "bb", "ccc"]
            let pipeline = source.iter().filter(|s| s.length() > minimum).map(|s| s.length())
            minimum = 9
            source = ["changed"]
            for n in pipeline.collect() { record_int(n) }
            for n in pipeline.collect() { record_int(n) }
            for n in pipeline { record_int(n) }
            """);
        Assert.That(_probe.Ints, Is.EqualTo(new[] { 2, 3, 2, 3, 2, 3 }));
    }
    [Test] public async Task any_short_circuits_and_map_is_lazy_until_a_terminal()
    {
        await Run("""
            let pipeline = [1, 2, 3].iter().map(|n| inspect(n))
            record_int(reads())
            record_bool(pipeline.any(|n| n == 2))
            record_int(reads())
            record_bool(pipeline.any())
            record_int(reads())
            """);
        Assert.That(_probe.Ints, Is.EqualTo(new[] { 0, 2, 3 }));
        Assert.That(_probe.Bools, Is.EqualTo(new[] { true, true }));
    }
    [Test] public async Task nested_aggregate_and_dictionary_lists_use_structural_equality()
    {
        await Run("""
            let a = [Item(value: 1), Item(value: 1), Item(value: 2)]
            let b = [Item(value: 2), Item(value: 3)]
            record_int(a.union(b).count())
            record_bool(a.contains(Item(value: 1)))
            for item in a.intersection(b) { record_int(item.value) }
            let lists = [[1, 2], [1, 2], [2, 1]]
            record_int(lists.union([[1, 2]]).count())
            record_bool(lists.contains([1, 2]))
            for list in lists.iter().filter(|list| list.contains(1)) { record_int(list.count()) }
            let d = Dictionary<String, Int>().with("a", 1).with("b", 2)
            let same = Dictionary<String, Int>().with("b", 2).with("a", 1)
            record_int([d].union([same]).count())
            record_bool([d].contains(same))
            let options = [Option<Int>.Some(value: 1), Option<Int>.None()]
            record_bool(options.contains(Option<Int>.Some(value: 1)))
            """, "struct Item { value: Int, }");
        Assert.That(_probe.Ints, Is.EqualTo(new[] { 3, 2, 2, 2, 2, 2, 1 }));
        Assert.That(_probe.Bools, Is.EqualTo(new[] { true, true, true, true }));
    }
    [Test] public async Task generic_helpers_support_sets_and_type_changing_pipelines()
    {
        await Run("""
            record_int(combine([1, 2], [2, 3]).count())
            let lengths = lengths(["a", "bb"])
            for n in lengths { record_int(n) }
            record_int(identity([Item(value: 5)])[0].value)
            """, """
            struct Item { value: Int, }
            fn combine<T>(a: List<T>, b: List<T>): List<T> = a.union(b)
            fn identity<T>(a: List<T>): List<T> = a.iter().collect()
            fn lengths(a: List<String>): List<Int> = a.iter().map(|s| s.length()).collect()
            """);
        Assert.That(_probe.Ints, Is.EqualTo(new[] { 3, 1, 2, 5 }));
    }
    [Test] public async Task collect_inside_outer_loops_does_not_reuse_cached_results()
    {
        await Run("for i in 1..4 { let values = [i].iter().map(|n| n).collect() record_int(values[0]) }");
        Assert.That(_probe.Ints, Is.EqualTo(new[] { 1, 2, 3 }));
    }
    [TestCase("let a = [1].contains(1.0)")]
    [TestCase("let a = [1].any(1.0)")]
    [TestCase("let a = [1].map<String>(|n| n).collect()")]
    [TestCase("let a = [1].union<String>([2])")]
    [TestCase("let a = [1].union([1.0])")]
    [TestCase("let a = [1, 2.0]")]
    [TestCase("let a = List<Void>()")]
    [TestCase("let a = [1].filter(|n| n)")]
    [TestCase("let a = [1].map(|n| touch())")]
    [TestCase("let a = [1].any(|n| touch() > 0)")]
    [TestCase("let a = [1].map(|n| hidden())")]
    [TestCase("let a = |n| n")]
    [TestCase("var a = [1].iter()")]
    [TestCase("let a = [1].union([true])")]
    [TestCase("let a = [Item(value: 1)].union([Other(value: 1)])")]
    [TestCase("let a = [[Item(value: 1)]].union([[Other(value: 1)]])")]
    public void invalid_types_and_impure_callbacks_are_rejected(string body)
    {
        var result = _runtime.Compiler.Compile(Script(body, "struct Item { value: Int, } struct Other { value: Int, } fn hidden(): Int = touch()"));
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics, Is.Not.Empty);
    }
    [Test] public void version_five_preserves_numeric_collection_coercion_but_rejects_lambdas()
    {
        Assert.That(_runtime.Compiler.Compile(Script("let a = [1].contains(1.0)"), new(LanguageVersion: 5)).Success, Is.True);
        Assert.That(_runtime.Compiler.Compile(Script("let a = [1].map(|n| n).collect()"), new(LanguageVersion: 5)).Success, Is.False);
    }
    [Test] public async Task invalid_delimiters_and_collection_or_execution_budgets_halt_with_traces()
    {
        var split = await Run("let a = \"a\".split(\"\")", success: false);
        Assert.That(split.TraceLog.Any(t => t.Contains("nonempty delimiter")), Is.True);
        await Run("let a = \"a,b,c\".split(\",\")", success: false, size: 2);
        await Run("let a = [1, 2].union([3, 4])", success: false, size: 3);
        await Run("let a = [1, 2, 3].iter().map(|n| n).collect()", success: false, allocations: 10);
        await Run("let a = [1, 2, 3].iter().filter(|n| false).collect()", success: false, steps: 15);
    }
    [Test] public void nominal_list_mismatches_are_rejected_at_the_ir_boundary()
    {
        var result = _runtime.Compiler.Compile(Script("let a = [Item(value: 1)].union([Item(value: 2)])", "struct Item { value: Int, }"));
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
        var graph = result.Executable!.CreateExecutionGraph();
        var node = graph.Nodes.Single(n => n.TypeId == "collection.union_list_aggregate");
        node.PropertyOverrides["collection_type"] = "List<Other>";
        Assert.That(new GlyphIrValidator(_runtime.Compiler.Catalog.Registry).Validate(graph).Any(d => d.Code == "GLYPH4010"), Is.True);
    }
    [TestCase("Object", "OBJECT.INVALID")]
    [TestCase("String", "\"text\"")]
    [TestCase("Int", "7")]
    [TestCase("Float", "7.0")]
    [TestCase("Bool", "true")]
    [TestCase("Location", "location_value()")]
    [TestCase("Effect", "effect_value()")]
    public async Task all_scalar_list_types_support_sets_and_empty_operands(string type, string literal)
    {
        await Run($"let a = List<{type}>().append({literal}) let empty = List<{type}>() record_int(a.union(a).count()) record_int(a.intersection(empty).count()) record_int(a.difference(empty).count()) record_int(a.complement(empty).count()) record_bool(a.contains({literal}))");
        Assert.That(_probe.Ints, Is.EqualTo(new[] { 1, 0, 1, 0 }));
        Assert.That(_probe.Bools, Is.EqualTo(new[] { true }));
    }
    [Test] public async Task nested_lambda_free_variables_are_captured_at_outer_recipe_construction()
    {
        await Run("var minimum = 2 let pipeline = [[1], [3]].filter(|items| items.any(|n| n > minimum)) minimum = 9 for items in pipeline { record_int(items[0]) }");
        Assert.That(_probe.Ints, Is.EqualTo(new[] { 3 }));
    }
    [Test] public async Task collect_uses_linear_buffer_allocations_and_cleans_up_on_halt()
    {
        await Run("let pipeline = [1, 2, 3, 4].map(|n| n) let list = pipeline.collect() record_int(list.count())", allocations: 18);
        var halted = await Run("let a = [1, 2, 3].map(|n| n).collect()", success: false, allocations: 8);
        Assert.That(halted.CollectionBuilders, Is.Empty);
    }
    [Test] public async Task equality_preserves_float_precision_and_string_case()
    {
        await Run("record_int([1.0].union([1.00000001]).count()) record_int([\"A\"].union([\"a\"]).count()) record_bool([1.0].contains(1.00000001))");
        Assert.That(_probe.Ints, Is.EqualTo(new[] { 2, 2 }));
        Assert.That(_probe.Bools, Is.EqualTo(new[] { false }));
    }
    [Test] public void pipelines_in_published_modules_validate_and_specialize()
    {
        var revision = GlyphModuleRevision.Create("list_helpers", "mod list_helpers { pub fn unique<T>(a: List<T>): List<T> = a.union(a) pub fn lengths(a: List<String>): List<Int> = a.map(|s| s.length()).collect() }");
        var module = _runtime.Compiler.CompileModule(revision);
        Assert.That(module.Diagnostics, Is.Empty, string.Join("\n", module.Diagnostics));
        _runtime.Compiler.Modules.Replace(new GlyphModuleSnapshot([revision]));
        var result = _runtime.Compiler.Compile("using list_helpers " + Script("let values = unique([1, 2]) let counts = lengths([\"a\", \"bb\"])") );
        Assert.That(result.Diagnostics, Is.Empty, string.Join("\n", result.Diagnostics));
    }
    private sealed class ProbeModule : IGlyphModule
    {
        public List<int> Ints { get; } = []; public List<string> Strings { get; } = []; public List<bool> Bools { get; } = [];
        private int _reads;
        public void Configure(GlyphModuleBuilder glyph)
        {
            glyph.Add(new Probe("record_int", GlyphDataType.Int, null, async cx => { Ints.Add(await cx.InInt("value")); return null; }));
            glyph.Add(new Probe("record_string", GlyphDataType.String, null, async cx => { Strings.Add(await cx.InString("value")); return null; }));
            glyph.Add(new Probe("record_bool", GlyphDataType.Bool, null, async cx => { Bools.Add(await cx.InBool("value")); return null; }));
            glyph.Add(new Probe("inspect", GlyphDataType.Int, GlyphDataType.Int, async cx => { _reads++; return await cx.InInt("value"); }));
            glyph.Add(new Probe("reads", null, GlyphDataType.Int, _ => Task.FromResult<object?>(_reads)));
            glyph.Add(new Probe("location_value", null, GlyphDataType.Location, _ => Task.FromResult<object?>(new Nwn.GlyphNwnLocation(new IntPtr(7)))));
            glyph.Add(new Probe("effect_value", null, GlyphDataType.Effect, _ => Task.FromResult<object?>(new Nwn.GlyphNwnEffect(new IntPtr(7)))));
            glyph.Add(new Probe("touch", null, GlyphDataType.Int, _ => Task.FromResult<object?>(1), true));
        }
    }
    private sealed class Probe(string name, GlyphDataType? input, GlyphDataType? output, Func<GlyphNodeContext, Task<object?>> run, bool action = false) : GlyphNodeBase
    {
        public override string TypeId => "generic_probe." + name;
        public override GlyphNodeDefinition CreateDefinition() => new GlyphIntrinsicDescriptor
        {
            TypeId = TypeId, DisplayName = name, Category = "Tests", Archetype = output == null || action ? GlyphNodeArchetype.Action : GlyphNodeArchetype.PureFunction,
            Parameters = input is { } type ? [Pins.In("value", "Value", type)] : [],
            Results = output is { } result ? [Pins.Out("value", "Value", result)] : [], Exports = [new(name, output == null ? null : "value")]
        }.CreateDefinition();
        public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
        {
            object? value = await run(cx);
            return new() { NextExecPinId = output == null || action ? "exec_out" : null, OutputValues = output == null ? [] : new() { ["value"] = value } };
        }
    }
}
