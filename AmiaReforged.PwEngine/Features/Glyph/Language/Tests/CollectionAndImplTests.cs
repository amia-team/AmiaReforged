using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Language.Modules;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public class CollectionAndImplTests
{
    private GlyphBootstrap _runtime = null!;
    private ProbeModule _probe = null!;
    [SetUp] public void Setup() { _probe = new(); _runtime = new(new GlyphNodeDefinitionRegistry(), [_probe]); }
    private static string Script(string body, string prelude = "") => prelude + " glyph t : encounter.before_group_spawn { " + body + " }";
    private async Task<GlyphExecutionContext> Run(string source, bool success = true, int size = 10_000, int allocations = 100_000)
    {
        var compilation = _runtime.Compiler.Compile(source);
        Assert.That(compilation.Diagnostics, Is.Empty, string.Join("\n", compilation.Diagnostics));
        var context = new GlyphExecutionContext { Graph = compilation.Executable!.CreateExecutionGraph(), EnableTracing = true,
            MaxCollectionSize = size, MaxCollectionAllocations = allocations };
        Assert.That(await _runtime.Interpreter.ExecuteAsync(context), Is.EqualTo(success), string.Join("\n", context.TraceLog));
        return context;
    }
    [Test] public async Task lists_and_dictionaries_are_immutable_and_support_iteration()
    {
        await Run(Script("""
            let original = [1, 2]
            let expanded = original.append(3)
            let replaced = expanded.with(0, 9).remove_at(1)
            for n in original { record(n) }
            for n in replaced { record(n) }
            let empty = Dictionary<String, Int>()
            let scores = empty.with("Alice", 10).with("Bob", 20)
            record(empty.count()) record(scores["Alice"])
            record(scores.get("missing", fallback: 7))
            record(scores.without("Alice").count())
            if scores.contains_key("Alice") && original.contains(2) { record(99) }
            for key in scores.keys() { record(scores[key]) }
            for value in scores.values() { record(value) }
            """));
        Assert.That(_probe.Values.Take(9), Is.EqualTo(new[] { 1, 2, 9, 3, 0, 10, 7, 1, 99 }));
        Assert.That(_probe.Values.Skip(9).Take(2), Is.EquivalentTo(new[] { 10, 20 }));
        Assert.That(_probe.Values.Skip(11), Is.EquivalentTo(new[] { 10, 20 }));
    }
    private static readonly string[] Types = ["Object", "String", "Int", "Float", "Bool"];
    private static string Literal(string type) => type switch { "Object" => "OBJECT.INVALID", "String" => "\"value\"", "Int" => "5", "Float" => "5.5", _ => "true" };
    public static IEnumerable<TestCaseData> DictionaryCases => Types.SelectMany(key => Types.Select(value => new TestCaseData(key, value)));
    [TestCaseSource(nameof(DictionaryCases))] public async Task all_basic_dictionary_combinations_preserve_types(string key, string value)
    {
        await Run(Script($"let d = identity(Dictionary<{key}, {value}>().with({Literal(key)}, {Literal(value)})) if d.contains_key({Literal(key)}) {{ record(d.count()) }} let v = d[{Literal(key)}] let list = d.values() record(list.count())", $"fn identity(d: Dictionary<{key}, {value}>): Dictionary<{key}, {value}> = d"));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 1 }));
    }
    [TestCase("Object")][TestCase("String")][TestCase("Int")][TestCase("Float")][TestCase("Bool")]
    public async Task all_basic_list_types_support_construction_and_readback(string type)
    {
        await Run(Script($"let list = List<{type}>().append({Literal(type)}) let value = list[0] if list.contains(value) {{ record(list.count()) }} for item in list {{ if list.contains(item) {{ record(1) }} }}"));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 1 }));
    }
    [Test] public async Task collection_bindings_snapshot_and_reassignment_invalidates_reads()
    {
        await Run(Script("var n = 1 let list = [n] n = 8 record(list[0]) var more = list let snapshot = more more = more.append(2) record(snapshot.count()) record(more.count())"));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 1, 2 }));
    }
    [Test] public async Task native_lists_are_snapshotted_once_before_host_mutation()
    {
        await Run(Script("let list = native_numbers() mutate_native() record(list[0]) record(list.count())"));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 2 })); Assert.That(_probe.NativeReads, Is.EqualTo(1));
    }
    [Test] public async Task collection_inputs_evaluate_once_in_written_order()
    {
        await Run(Script("let list = [next(), next()] record(list[0]) record(list[1]) let d = Dictionary<Int, Int>().with(value: next(), key: next()) record(d[4])"));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 2, 3 })); Assert.That(_probe.Calls, Is.EqualTo(4));
    }
    [Test] public async Task identical_expressions_as_dictionary_key_and_value_are_both_bound()
    {
        await Run(Script("var n = 4 let d = Dictionary<Int, Int>().with(n, n) record(d[n])"));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 4 }));
    }
    [Test] public async Task collections_work_in_functions_struct_fields_and_adt_payloads()
    {
        await Run(Script("""
            var holder = Holder(values: [2, 3], scores: Dictionary<String, Int>().with("x", 7))
            record(holder.values[1]) record(holder.scores["x"])
            let result = Outcome.Values(values: add(holder.values))
            match result { Values { values } { record(values.count()) } Missing {} {} }
            """, """
            struct Holder { values: List<Int>, scores: Dictionary<String, Int> }
            type Outcome { Values { values: List<Int> } Missing {} }
            fn add(values: List<Int>): List<Int> { return values.append(4) }
            """));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 3, 7, 3 }));
    }
    [Test] public async Task struct_and_adt_methods_support_self_associated_functions_and_chaining()
    {
        await Run(Script("let item = Item.create(4) record(item.amount()) record(item.increased(3).amount()) record(item.amount()) let result = Outcome.Found(value: 8) record(result.value_or(0)) record(Outcome.Missing().value_or(2))", """
            struct Item { value: Int }
            impl Item {
                fn amount(self): Int = self.value
                fn increased(self, delta: Int): Self { return Item(value: self.value + delta) }
                fn create(value: Int): Self = Item(value: value)
            }
            type Outcome { Found { value: Int } Missing {} }
            impl Outcome {
                fn value_or(self, fallback: Int): Int {
                    match self { Found { value } { return value } Missing {} { return fallback } }
                }
            }
            """));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 4, 7, 4, 8, 2 }));
    }
    [Test] public async Task expression_methods_evaluate_receiver_and_unused_arguments_once()
    {
        await Run(Script("record(query_item().doubled(next()))", "struct Item { value: Int } impl Item { fn doubled(self, unused: Int): Int = self.value + self.value }"));
        Assert.That(_probe.ItemReads, Is.EqualTo(1));
        Assert.That(_probe.Calls, Is.EqualTo(1));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 8 }));
    }
    [Test] public async Task method_results_support_field_access_and_capture_once()
    {
        await Run(Script("let value = query_item().increased(2).value record(value) record(value)",
            "struct Item { value: Int } impl Item { fn increased(self, amount: Int): Self = Item(value: self.value + amount) }"));
        Assert.That(_probe.ItemReads, Is.EqualTo(1));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 6, 6 }));
    }
    [Test] public async Task native_foreach_snapshots_obey_collection_size_limits()
        => await Run(Script("for n in native_numbers() { record(n) }"), success: false, size: 1);
    [TestCase("let x = [1].append(unknown: 2)")]
    [TestCase("let x = [1].append()")]
    [TestCase("let x = [1].append(true)")]
    [TestCase("let x = []")]
    [TestCase("let x = [1, true]")]
    [TestCase("let x = List<Location>()")]
    [TestCase("let x = Dictionary<String, Effect>()")]
    [TestCase("let x = [1] x[0] = 2")]
    [TestCase("let x = Dictionary<String, Int>() x[\"a\"] = 2")]
    [TestCase("var x = [1] x = [true]")]
    [TestCase("var x = Dictionary<String, Int>() x = Dictionary<Int, Int>()")]
    public void invalid_collection_programs_produce_diagnostics(string body)
        => Assert.That(_runtime.Compiler.Compile(Script(body)).Success, Is.False);
    [TestCase("impl Unknown {}")]
    [TestCase("impl Unknown { fn f(self): Int = 1 }")]
    [TestCase("struct Item { value: Int } impl Item { fn value(self): Int = 1 }")]
    [TestCase("struct Item {} impl Item { fn f(self): Void { self = Item() } }")]
    [TestCase("struct Item { value: Int } impl Item { fn f(self): Void { self.value = 3 } }")]
    [TestCase("struct Item {} impl Item { fn f(self): Int = self.f() }")]
    public void invalid_methods_produce_diagnostics(string prelude)
        => Assert.That(_runtime.Compiler.Compile(Script("", prelude)).Success, Is.False);
    [TestCase("record([1][2])", "List index")]
    [TestCase("record(Dictionary<String, Int>()[\"missing\"])", "key was not found")]
    public async Task invalid_access_halts_with_source_aware_trace(string body, string error)
    {
        var context = await Run(Script(body), success: false);
        Assert.That(context.TraceLog.Any(t => t.Contains(error)), Is.True);
        Assert.That(context.TraceLog.Any(t => t.Contains("source.glyph:")), Is.True);
    }
    [Test] public async Task collection_budgets_halt_execution()
    {
        var context = await Run(Script("var list = List<Int>() for i in 0..10 { list = list.append(i) }"), success: false, size: 2);
        Assert.That(context.TraceLog.Any(t => t.Contains("allocation budget")), Is.True);
        await Run(Script("var list = List<Int>() for i in 0..10 { list = list.append(i) }"), success: false, allocations: 3);
    }
    [Test] public async Task dictionary_float_keys_are_exact_and_string_keys_are_ordinal()
    {
        await Run(Script("let d = Dictionary<Float, Int>().with(1.0, 2).with(1.00000001, 3) record(d.count()) record(d[1.0]) let s = Dictionary<String, Int>().with(\"A\", 1).with(\"a\", 2) record(s.count())"));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 2, 2, 2 }));
    }
    [TestCase(1)][TestCase(2)][TestCase(3)] public void older_versions_reject_new_syntax_and_preserve_impl_identifier(int version)
    {
        Assert.That(_runtime.Compiler.Compile(Script("let x = [1]"), new(LanguageVersion: version)).Success, Is.False);
        Assert.That(_runtime.Compiler.Compile(Script("", "struct Item {} impl Item { fn f(self): Int = 1 }"), new(LanguageVersion: version)).Success, Is.False);
        Assert.That(_runtime.Compiler.Compile(Script("let impl = 1 record(impl)"), new(LanguageVersion: version)).Success, Is.True);
    }
    [TestCase(1)][TestCase(2)][TestCase(3)] public void older_versions_keep_collection_names_as_identifiers_and_types(int version)
    {
        var result = _runtime.Compiler.Compile(Script("var Dictionary = 1 if Dictionary < 2 { let item = List(value: 4) record(item.value) }", "struct List { value: Int }"), new(LanguageVersion: version));
        Assert.That(result.Diagnostics, Is.Empty);
        if (version >= 2)
        {
            var module = GlyphModuleRevision.Create("List", "mod List { pub struct Dictionary {} }") with { LanguageVersion = version };
            Assert.That(_runtime.Compiler.CompileModule(module).Success, Is.True);
        }
    }
    [Test] public async Task imported_methods_respect_visibility_and_module_ownership()
    {
        var revision = GlyphModuleRevision.Create("items", """
            mod items {
                pub struct Item { value: Int }
                impl Item { pub fn amount(self): Int = self.hidden() fn hidden(self): Int = self.value }
                pub fn create(value: Int): Item = Item(value: value)
            }
            """);
        Assert.That(_runtime.Compiler.CompileModule(revision).Success, Is.True);
        _runtime.Compiler.Modules.Replace(new GlyphModuleSnapshot([revision]));
        await Run("using items " + Script("record(create(7).amount())"));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 7 }));
        Assert.That(_runtime.Compiler.Compile("using items " + Script("record(create(7).hidden())")).Success, Is.False);
        Assert.That(_runtime.Compiler.Compile("using items " + Script("record(Item.hidden(create(7)))")).Success, Is.False);
        var foreign = GlyphModuleRevision.Create("foreign", "mod foreign { using items impl Item { pub fn extra(self): Int = 1 } }");
        Assert.That(_runtime.Compiler.CompileModule(foreign).Success, Is.False);
        Assert.That(_runtime.Compiler.Compile("using items " + Script(""), new(LanguageVersion: 3)).Success, Is.False);
    }
    private sealed class ProbeModule : IGlyphModule
    {
        public List<int> Values { get; } = [];
        public List<int> Native { get; } = [1, 2];
        public int Calls, NativeReads, ItemReads;
        public void Configure(GlyphModuleBuilder glyph)
        {
            glyph.Add(new Probe("query_item", [], Pins.Out("value", "Value", GlyphDataType.Aggregate) with { AggregateTypeName = "Item" }, _ =>
            {
                ItemReads++;
                return Task.FromResult<object?>(new GlyphAggregateValue("Item", null, new Dictionary<string, GlyphAggregateFieldValue> { ["value"] = new(GlyphDataType.Int, "Int", 4) }));
            }));
            glyph.Add(new Probe("record", [Pins.InInt("value", "Value")], null, async cx => { Values.Add(await cx.InInt("value")); return null; }, true));
            glyph.Add(new Probe("next", [], Pins.Out("value", "Value", GlyphDataType.Int), _ => Task.FromResult<object?>(++Calls), true));
            glyph.Add(new Probe("native_numbers", [], Pins.Out("value", "Value", GlyphDataType.List) with { ElementType = GlyphDataType.Int }, _ => { NativeReads++; return Task.FromResult<object?>(Native); }));
            glyph.Add(new Probe("mutate_native", [], null, _ => { Native[0] = 9; Native.Add(3); return Task.FromResult<object?>(null); }, true));
        }
    }
    private sealed class Probe(string name, List<GlyphPin> inputs, GlyphPin? result, Func<GlyphNodeContext, Task<object?>> run, bool action = false) : GlyphNodeBase
    {
        public override string TypeId => "collection_probe." + name;
        public override GlyphNodeDefinition CreateDefinition() => new GlyphIntrinsicDescriptor
        { TypeId = TypeId, DisplayName = name, Category = "Tests", Archetype = action ? GlyphNodeArchetype.Action : GlyphNodeArchetype.PureFunction,
            Parameters = inputs, Results = result == null ? [] : [result], Exports = [new(name, result?.Id)] }.CreateDefinition();
        public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
        {
            object? output = await run(cx);
            return new() { NextExecPinId = action ? "exec_out" : null, OutputValues = result == null ? [] : new() { ["value"] = output } };
        }
    }
}
