using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Language.Parsing;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public class ImperativeLanguageTests
{
    private GlyphBootstrap _runtime = null!;
    private ProbeModule _probe = null!;
    private GlyphNodeDefinitionRegistry _registry = null!;

    [SetUp]
    public void Setup()
    {
        _probe = new(); _registry = new();
        _runtime = new(_registry, [_probe]);
    }

    private GlyphExecutable Compile(string source)
    {
        var result = _runtime.Compiler.Compile(source);
        Assert.That(result.Diagnostics, Is.Empty, string.Join("\n", result.Diagnostics));
        return result.Executable!;
    }
    private static string Script(string body) => "glyph test : encounter.before_group_spawn { " + body + " }";
    private async Task<GlyphExecutionContext> Run(string body, int limit = 10_000, string prelude = "")
    {
        var program = Compile(prelude + "\n" + Script(body));
        var context = new GlyphExecutionContext { Graph = program.CreateExecutionGraph(), MaxExecutionSteps = limit, EnableTracing = true, SpawnCount = 3 };
        Assert.That(await _runtime.Interpreter.ExecuteAsync(context), Is.True, string.Join("\n", context.TraceLog));
        Assert.That(context.LoopStates, Is.Empty);
        Assert.That(context.TraceLog.Any(t => t.Contains("ERROR")), Is.False);
        return context;
    }

    [Test]
    public void lexer_distinguishes_ranges_floats_and_keywords()
    {
        var lexer = new GlyphLexer("var while for continue step 1.5 1..5 1..=5 *= /=");
        var tokens = lexer.Lex();
        Assert.That(lexer.Diagnostics, Is.Empty);
        Assert.That(tokens.Select(t => t.Kind), Is.EqualTo(new[] { "var", "while", "for", "continue", "step", "float", "integer", "..", "integer", "integer", "..=", "integer", "*=", "/=", "eof" }));
        Assert.That(tokens[5].Value, Is.EqualTo(1.5));
    }

    [Test]
    public void parser_builds_typed_control_flow_and_pattern_syntax()
    {
        var parser = new GlyphParser(new GlyphLexer(Script("""
            var n = 0
            while n < 2 { n += 1 continue }
            for item in objects() { break }
            for i in 0..=3 step 2 { }
            match n { 0 {} OBJECT_TYPE.CREATURE {} _ {} }
            match result { Found { target, text } {} Missing {} {} }
            """)).Lex());
        var tree = parser.Parse()!;
        Assert.That(parser.Diagnostics, Is.Empty);
        Assert.That(tree.Body.Statements[0], Is.TypeOf<VarStatementSyntax>());
        Assert.That(tree.Body.Statements[1], Is.TypeOf<WhileStatementSyntax>());
        Assert.That(tree.Body.Statements[2], Is.TypeOf<ForeachStatementSyntax>());
        Assert.That(tree.Body.Statements[3], Is.TypeOf<ForRangeStatementSyntax>());
        var scalar = (MatchStatementSyntax)tree.Body.Statements[4];
        Assert.That(scalar.Arms[0].Pattern, Is.TypeOf<ValuePatternSyntax>());
        Assert.That(scalar.Arms[2].Pattern, Is.TypeOf<WildcardPatternSyntax>());
        Assert.That(((MatchStatementSyntax)tree.Body.Statements[5]).Arms[0].Pattern, Is.TypeOf<VariantPatternSyntax>());
    }

    [TestCase("while 1 {}", "GLYPH2004")]
    [TestCase("for item in 4 {}", "GLYPH2004")]
    [TestCase("for i in 0.0..3 {}", "GLYPH2004")]
    [TestCase("for i in 0..3.0 {}", "GLYPH2004")]
    [TestCase("for i in 0..3 step 1.0 {}", "GLYPH2004")]
    [TestCase("for i in 0..3 step 0 {}", "GLYPH2014")]
    [TestCase("for i in 0..3 step 1 - 1 {}", "GLYPH2014")]
    [TestCase("continue", "GLYPH3004")]
    [TestCase("break", "GLYPH3004")]
    [TestCase("var x = 1 var x = 2", "GLYPH2006")]
    [TestCase("let x = 1 var x = 2", "GLYPH2006")]
    [TestCase("var x = 1 x = \"hello\"", "GLYPH2004")]
    [TestCase("let x = 1 x = 2", "GLYPH2008")]
    [TestCase("if true { var y = 2 } record(y)", "GLYPH3002")]
    [TestCase("match 1 { \"hello\" {} }", "GLYPH2004")]
    [TestCase("match 1 { _ {} _ {} }", "GLYPH2010")]
    [TestCase("match 1 { _ {} 1 {} }", "GLYPH2010")]
    [TestCase("match 1 { 1 {} 1 {} }", "GLYPH2010")]
    [TestCase("match 1 { read_count() {} }", "GLYPH2010")]
    public void invalid_programs_have_structured_diagnostics(string body, string code)
    {
        var result = _runtime.Compiler.Compile(Script(body));
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Any(d => d.Code == code), Is.True, string.Join("\n", result.Diagnostics));
    }

    [Test]
    public async Task while_observes_mutations_and_nested_scopes()
    {
        await Run("""
            var n = 0
            while n < 10 { record(n) n += 1 }
            if true { var n = 40 n += 2 record(n) }
            record(n)
            n *= 2 n /= 2 n -= 1 record(n)
            """);
        Assert.That(_probe.Values, Is.EqualTo(Enumerable.Range(0, 10).Concat(new[] { 42, 10, 9 })));
    }

    [Test]
    public async Task local_initializer_and_rhs_execute_once_and_eagerly()
    {
        await Run("var n = next_value() record(n) record(n) n += next_value() record(n) record(n)");
        Assert.That(_probe.Calls, Is.EqualTo(2));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 1, 3, 3 }));
    }

    [Test]
    public async Task lazy_dependencies_of_mutable_reads_are_invalidated_on_write()
    {
        await Run("var n = 1 let doubled = n * 2 record(doubled) n = 3 record(doubled)");
        Assert.That(_probe.Values, Is.EqualTo(new[] { 2, 6 }));
    }

    [Test]
    public async Task existing_let_pure_queries_remain_lazy_and_impure_values_remain_captured()
    {
        await Run("let unused = read_count() let captured = next_value() record(captured) record(captured)");
        Assert.That(_probe.Reads, Is.Zero);
        Assert.That(_probe.Calls, Is.EqualTo(1));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 1 }));
    }

    [Test]
    public async Task while_condition_queries_and_short_circuit_preludes_reevaluate()
    {
        await Run("while read_count() > 0 && next_value() < 10 { spawn.modify_count(read_count() - 1) }");
        Assert.That(_probe.Calls, Is.EqualTo(3));
        // One query before every iteration, plus the distinct query inside each body.
        Assert.That(_probe.Reads, Is.EqualTo(7));
    }

    [TestCase("0..5", new[] { 0, 1, 2, 3, 4 })]
    [TestCase("0..=5", new[] { 0, 1, 2, 3, 4, 5 })]
    [TestCase("5..0", new[] { 5, 4, 3, 2, 1 })]
    [TestCase("5..0 step -2", new[] { 5, 3, 1 })]
    [TestCase("0..5 step 2", new[] { 0, 2, 4 })]
    [TestCase("0..5 step -1", new int[0])]
    [TestCase("1..1", new int[0])]
    [TestCase("1..=1", new[] { 1 })]
    [TestCase("2147483646..=2147483647", new[] { 2147483646, 2147483647 })]
    [TestCase("-2147483647..=-2147483648", new[] { -2147483647, -2147483648 })]
    public async Task range_semantics(string range, int[] expected)
    {
        await Run("for i in " + range + " { record(i) }");
        Assert.That(_probe.Values, Is.EqualTo(expected));
    }

    [Test]
    public async Task range_bounds_and_step_are_snapshots_at_loop_entry()
    {
        await Run("var end = 5 var stride = 1 for i in next_value()..end step stride { record(i) end = 2 stride = 3 }");
        Assert.That(_probe.Calls, Is.EqualTo(1));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public async Task break_continue_apply_to_nearest_loop_and_reinitialize_inner_state()
    {
        await Run("""
            for outer in 0..3 {
                for inner in 0..5 {
                    if inner == 1 { continue }
                    if inner == 3 { break }
                    record(outer * 10 + inner)
                }
            }
            var n = 0
            while true {
                n += 1
                if n == 2 { continue }
                if n == 4 { break }
                record(n)
            }
            """);
        Assert.That(_probe.Values, Is.EqualTo(new[] { 0, 2, 10, 12, 20, 22, 1, 3 }));
    }

    [Test]
    public async Task for_each_alias_and_all_typed_lists_share_loop_execution()
    {
        var a = Compile(Script("for item in objects() { if item == OBJECT.INVALID { continue } record(read_count()) break }"));
        var b = Compile(Script("foreach item in objects() { if item == OBJECT.INVALID { continue } record(read_count()) break }"));
        Assert.That(GlyphGraphSerializer.Serialize(a.CreateExecutionGraph()), Is.EqualTo(GlyphGraphSerializer.Serialize(b.CreateExecutionGraph())));
        await Run("for value in numbers() { if value == 2 { continue } record(value) }");
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 3 }));
        Compile("glyph effects : interaction { completed { for effect in nwn.effects(player) { nwn.remove_effect(player, effect) continue } } }");
    }

    [TestCase("for")]
    [TestCase("foreach")]
    public async Task object_list_break_continue_and_outer_capture_survive(string keyword)
    {
        await Run("let saved = next_value() " + keyword + " item in objects() { match item { OBJECT.INVALID { continue } _ {} } if read_count() == 1 { break } record(saved) spawn.modify_count(read_count() - 1) } record(saved)");
        Assert.That(_probe.Calls, Is.EqualTo(1));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 1, 1 }));
    }

    [Test]
    public async Task effect_list_continue_break_and_eager_list_storage_execute()
    {
        await Run("var list = effect_values() var n = 0 for effect_value in list { n += 1 if n == 1 { continue } record(n) break } record(n)");
        Assert.That(_probe.Values, Is.EqualTo(new[] { 2, 2 }));
    }

    [Test]
    public async Task runtime_struct_results_nested_fields_and_numeric_conversion_keep_declared_types()
    {
        await Run("""
            let fetched = query_request()
            record(fetched.amount)
            record(query_request().amount)
            var request = Request(amount: 2.6, text: "stored")
            record(request.amount)
            var outer = Outer(request: request)
            record(outer.request.amount)
            var result = Box.Present(request: outer.request)
            match result { Present { request } { record(request.amount) record_text(request.text) } }
            """, prelude: "struct Request { amount: Int text: String } struct Outer { request: Request } type Box { Present { request: Request } }");
        Assert.That(_probe.Values, Is.EqualTo(new[] { 3, 3, 3, 3, 3 }));
        Assert.That(_probe.Texts, Is.EqualTo(new[] { "stored" }));
    }

    [Test]
    public async Task ordinary_arithmetic_and_division_retain_existing_float_semantics()
    {
        await Run("var value = 1 + 1 value = 2.5 record(value) var large = 2147483647 + 1 if large > 0 { record(9) } var divided = 5 / 2 record(divided)");
        Assert.That(_probe.Values, Is.EqualTo(new[] { 2, 9, 2 }));
        await Run("for i in 1 + 1..3 + 2 step +1 { record(i) }");
        Assert.That(_probe.Values.Skip(3), Is.EqualTo(new[] { 2, 3, 4 }));
    }

    [Test]
    public void aggregate_api_arguments_and_ir_validation_preserve_nominal_identity()
    {
        var good = Compile(ResultType + Script("var result = identity_result(query_result()) match result { Found {} {} Missing {} {} }"));
        var graph = good.CreateExecutionGraph();
        var reader = graph.Nodes.First(n => n.TypeId == "local.read_aggregate");
        reader.PropertyOverrides["nominal"] = "OtherResult";
        Assert.That(new GlyphIrValidator(_registry).Validate(graph).Any(d => d.Code == "GLYPH4010"), Is.True);
        var bad = _runtime.Compiler.Compile(ResultType + "struct Other { n: Int }" + Script("let result = identity_result(Other(n: 1))"));
        Assert.That(bad.Diagnostics.Any(d => d.Code == "GLYPH2004"), Is.True);
    }

    [Test]
    public async Task nested_loop_caches_do_not_reuse_outer_iteration_queries()
    {
        await Run("for outer in 0..3 { spawn.modify_count(outer) for inner in 0..2 { record(read_count()) } }");
        Assert.That(_probe.Values, Is.EqualTo(new[] { 0, 0, 1, 1, 2, 2 }));
    }

    [Test]
    public async Task scalar_match_captures_once_and_joins_selected_arm()
    {
        await Run("""
            match next_value() { 0 { record(99) } 1 { record(1) } _ { record(98) } }
            match 50 { 1 { record(97) } }
            match "door" { "creature" { record(96) } "door" { record(2) } _ { record(95) } }
            match false { true { record(94) } false { record(3) } }
            match 123 { 0 { record(93) } _ { record(4) } }
            match OBJECT.INVALID { OBJECT.INVALID { record(5) } }
            record(6)
            """);
        Assert.That(_probe.Calls, Is.EqualTo(1));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));
    }

    private const string ResultType = "type TestResult { Found { number: Int text: String target: Object } Missing { reason: String } }";

    [Test]
    public async Task dynamic_adt_from_runtime_function_passes_through_let_and_var()
    {
        await Run("""
            let result = query_result()
            match result {
                Found { number, text, target } { record(number) record_text(text) match target { OBJECT.INVALID { record(90) } _ {} } }
                Missing { reason } { record_text(reason) }
            }
            var result2 = query_result()
            spawn.modify_count(0)
            result2 = query_result()
            match result2 { Found {} { record(91) } Missing { reason } { record_text(reason) } }
            """, prelude: ResultType);
        Assert.That(_probe.AggregateCalls, Is.EqualTo(3));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 3, 90 }));
        Assert.That(_probe.Texts, Is.EqualTo(new[] { "runtime", "none" }));
    }

    [Test]
    public async Task constructors_runtime_struct_storage_and_global_functions_work()
    {
        await Run("""
            var request = Request(amount: next_value(), text: "saved")
            record(request.amount)
            var result = wrap(request)
            match result { Found { number, text } { record(number) record_text(text) } Missing {} {} }
            result = TestResult.Missing(reason: "changed")
            match result { Found {} {} _ { record(7) } }
            """, prelude: ResultType + "\nstruct Request { amount: Int text: String }\nfn wrap(request: Request): TestResult = TestResult.Found(number: request.amount, text: request.text, target: OBJECT.INVALID)");
        Assert.That(_probe.Calls, Is.EqualTo(1));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 1, 7 }));
        Assert.That(_probe.Texts, Is.EqualTo(new[] { "saved" }));
    }

    [Test]
    public async Task match_and_loops_can_nest_with_terminating_arms()
    {
        await Run("""
            for i in 0..4 {
                match i { 1 { continue } 3 { break } _ { while true { record(i) break } } }
                record(10 + i)
            }
            match 1 { 1 { for i in 0..2 { match i { 0 { record(20) } _ { record(21) } } } } }
            """);
        Assert.That(_probe.Values, Is.EqualTo(new[] { 0, 10, 2, 12, 20, 21 }));
    }

    [TestCase("match r { Found {} {} }")]
    [TestCase("match r { Found { nope } {} Missing {} {} }")]
    [TestCase("match r { Found { number, number } {} Missing {} {} }")]
    [TestCase("match r { Found {} {} Found {} {} Missing {} {} }")]
    [TestCase("match r { Nope {} {} _ {} }")]
    public void runtime_adt_validation_remains_compile_time(string match)
    {
        var result = _runtime.Compiler.Compile(ResultType + Script("let r = query_result() " + match));
        Assert.That(result.Diagnostics.Any(d => d.Code == "GLYPH2010"), Is.True);
    }

    [Test]
    public void nominal_aggregate_types_cannot_be_assigned_to_each_other()
    {
        var result = _runtime.Compiler.Compile("struct A { n: Int } struct B { n: Int }" + Script("var a = A(n: 1) a = B(n: 2)"));
        Assert.That(result.Diagnostics.Any(d => d.Code == "GLYPH2004"), Is.True);
    }

    [TestCase("while true {}", 10_000)]
    [TestCase("for i in 0..2 { while true { continue } }", 80)]
    public async Task infinite_loops_stop_at_the_existing_execution_limit(string body, int limit)
    {
        var context = new GlyphExecutionContext { Graph = Compile(Script(body)).CreateExecutionGraph(), EnableTracing = true, MaxExecutionSteps = limit };
        Assert.That(await _runtime.Interpreter.ExecuteAsync(context), Is.False);
        Assert.That(context.ExecutionStepCount, Is.EqualTo(limit));
        Assert.That(context.TraceLog.Any(t => t.Contains("step limit")), Is.True);
        Assert.That(context.LoopStates, Is.Empty);
        Assert.That(context.ActiveStack, Is.Null);
    }

    [Test]
    public async Task dynamic_zero_step_halts_safely_with_a_source_trace()
    {
        var context = new GlyphExecutionContext { Graph = Compile(Script("var stride = 0 for i in 0..10 step stride { record(i) }")).CreateExecutionGraph(), EnableTracing = true };
        Assert.That(await _runtime.Interpreter.ExecuteAsync(context), Is.False);
        Assert.That(context.TraceLog.Any(t => t.Contains("GLYPH2014")), Is.True);
        Assert.That(_probe.Values, Is.Empty);
    }

    [Test]
    public void lowering_retains_source_maps_and_expected_operations()
    {
        var program = Compile(Script("var n = 0 while n < 2 { n += 1 if n == 1 { continue } break } for i in 0..3 { record(i) } for item in objects() {} match n { 0 {} _ {} }"));
        var graph = program.CreateExecutionGraph();
        foreach (string type in new[] { "flow.while", "flow.for_each", "flow.for_range", "flow.break", "flow.continue", "flow.branch", "local.write_int" })
            Assert.That(graph.Nodes.Any(n => n.TypeId == type), Is.True, type);
        Assert.That(graph.Nodes.All(n => program.SourceMap.ContainsKey(n.InstanceId)), Is.True);
    }

    [TestCase("fn f(): Int { return \"bad\" }", "record(f())", "GLYPH2004")]
    [TestCase("fn f(): Int { return; }", "record(f())", "GLYPH2004")]
    [TestCase("fn f(): Void { return 1 }", "f()", "GLYPH2004")]
    [TestCase("fn f(): Int { if true { return 1 } }", "record(f())", "GLYPH2030")]
    [TestCase("fn f(): Int { while true { return 1 } }", "record(f())", "GLYPH2030")]
    [TestCase("fn f(): Int { for i in 0..3 { return i } }", "record(f())", "GLYPH2030")]
    [TestCase("fn f(): Int { match 1 { 1 { return 1 } } }", "record(f())", "GLYPH2030")]
    [TestCase("", "return 1", "GLYPH2029")]
    [TestCase("fn f(): Void { break }", "for i in 0..3 { f() }", "GLYPH3004")]
    [TestCase("fn f(): Void { continue }", "for i in 0..3 { f() }", "GLYPH3004")]
    [TestCase("fn f(): Int { return secret }", "var secret = 1 record(f())", "GLYPH3002")]
    [TestCase("fn f(value: Int): Int { value = 2 return value }", "record(f(1))", "GLYPH2008")]
    [TestCase("fn f(): Int { return f() }", "record(f())", "GLYPH2012")]
    [TestCase("fn f(): Void {}", "let value = f()", "GLYPH2004")]
    [TestCase("fn unused(): Int {}", "", "GLYPH2030")]
    [TestCase("fn unused(): Void { return 1 }", "", "GLYPH2004")]
    [TestCase("fn unused(value: Void): Int { return 1 }", "", "GLYPH2004")]
    [TestCase("fn unused(value: Int, value: Int): Int { return value }", "", "GLYPH2006")]
    [TestCase("fn f(value: Void): Int { return 1 } fn empty(): Void {}", "record(f(empty()))", "GLYPH2004")]
    public void statement_functions_reject_invalid_returns_and_scope_leaks(string declarations, string body, string code)
    {
        var result = _runtime.Compiler.Compile(declarations + Script(body));
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Any(d => d.Code == code), Is.True, string.Join("\n", result.Diagnostics));
    }

    [Test]
    public async Task functions_return_through_nested_loops_without_exiting_the_callers_loop()
    {
        await Run("for n in 0..3 { record(find(n)) record(n) }", prelude: """
            fn find(value: Int): Int {
                for outer in 0..3 {
                    for inner in 0..3 {
                        if inner == 1 { return value }
                    }
                }
                return 99
            }
            """);
        Assert.That(_probe.Values, Is.EqualTo(new[] { 0, 0, 1, 1, 2, 2 }));
    }

    [Test]
    public async Task block_function_arguments_are_eager_ordered_and_evaluated_once()
    {
        await Run("record(first(second: next_value(), first: next_value())) record(next_value())", prelude: """
            fn first(first: Int, second: Int): Int {
                record(first)
                record(first)
                record(second)
                return first
            }
            """);
        Assert.That(_probe.Calls, Is.EqualTo(3));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 2, 2, 1, 2, 3 }));
    }

    [Test]
    public async Task unused_parameters_are_evaluated_and_mutable_arguments_are_snapshots()
    {
        await Run("record(first(spawn.count, mutate())) record(spawn.count)", prelude: """
            fn first(value: Int, ignored: Int): Int {
                return value
            }
            fn mutate(): Int { spawn.modify_count(5) return next_value() }
            """);
        Assert.That(_probe.Calls, Is.EqualTo(1));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 3, 5 }));
    }

    [Test]
    public async Task void_helpers_support_early_returns_fallthrough_and_nested_calls()
    {
        await Run("emit(1) emit(2) wrapper() record(9)", prelude: """
            fn emit(value: Int): Void {
                if value == 1 { return; }
                record(value)
            }
            fn wrapper(): Void { emit(3) return; record(99) }
            """);
        Assert.That(_probe.Values, Is.EqualTo(new[] { 2, 3, 9 }));
    }

    [Test]
    public async Task nested_value_calls_and_let_bindings_execute_each_function_once()
    {
        await Run("let captured = outer() record(captured) record(captured)", prelude: """
            fn inner(): Int { return next_value() }
            fn outer(): Int { var value = inner() record(value) return value }
            """);
        Assert.That(_probe.Calls, Is.EqualTo(1));
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 1, 1 }));
    }

    [Test]
    public async Task return_values_survive_loop_cache_cleanup_and_calls_reinitialize_locals()
    {
        await Run("for item in numbers() { record(count(item)) }", prelude: """
            fn count(value: Int): Int {
                var result = 0
                while result < value { result += 1 }
                for item in numbers() { return result }
                return result
            }
            """);
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task exhaustive_matches_return_nominal_aggregate_values()
    {
        await Run("var r = wrap(2) record(extract(r))", prelude: ResultType + """
            fn wrap(value: Int): TestResult {
                return TestResult.Found(value, "value", OBJECT.INVALID)
            }
            fn extract(result: TestResult): Int {
                match result {
                    Found { number } { return number }
                    Missing {} { return 0 }
                }
            }
            """);
        Assert.That(_probe.Values, Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public async Task short_circuit_skips_function_bodies_and_function_loops_share_the_execution_budget()
    {
        await Run("if false && query() { record(99) } if true || query() { record(1) }", prelude: "fn query(): Bool { record(9) return true }");
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1 }));
        var program = Compile("fn spin(): Void { while true {} }" + Script("spin() record(99)"));
        var context = new GlyphExecutionContext { Graph = program.CreateExecutionGraph(), MaxExecutionSteps = 30 };
        Assert.That(await _runtime.Interpreter.ExecuteAsync(context), Is.False);
        Assert.That(context.ExecutionHalted, Is.True);
        Assert.That(_probe.Values, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task numeric_parameters_and_returns_use_the_declared_type()
    {
        await Run("record(identity(2.5))", prelude: "fn identity(value: Int): Int { return value }");
        Assert.That(_probe.Values, Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void older_versions_preserve_return_identifiers_and_reject_statement_bodies()
    {
        foreach (int version in new[] { 1, 2 })
        {
            var old = _runtime.Compiler.Compile("fn return(value: Int): Int = value " + Script("record(return(1))"), new(LanguageVersion: version));
            Assert.That(old.Success, Is.True, string.Join("\n", old.Diagnostics));
            var block = _runtime.Compiler.Compile("fn f(): Int { return 1 } " + Script("record(f())"), new(LanguageVersion: version));
            Assert.That(block.Diagnostics.Any(d => d.Code == "GLYPH1011"), Is.True);
        }
    }

    [Test]
    public async Task function_returns_in_while_conditions_reevaluate_on_each_iteration()
    {
        await Run("while query() { spawn.modify_count(spawn.count - 1) } record(spawn.count)",
            prelude: "fn query(): Bool { return spawn.count > 0 }");
        Assert.That(_probe.Values, Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public async Task cancelled_functions_do_not_resume_their_caller()
    {
        var executable = Compile("fn f(): Void { record(1) }" + Script("f() record(2)"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var context = new GlyphExecutionContext { Graph = executable.CreateExecutionGraph(), CancellationToken = cancellation.Token };
        Assert.That(await _runtime.Interpreter.ExecuteAsync(context), Is.False);
        Assert.That(context.ExecutionHalted, Is.True);
        Assert.That(_probe.Values, Is.Empty);
        Assert.That(context.LoopStates, Is.Empty);
    }

    [Test]
    public async Task fail_inside_a_function_preserves_interaction_failure_and_stops_the_caller()
    {
        var executable = Compile("fn reject(): Int { fail \"blocked\" } glyph t : interaction { attempted { for i in 0..3 { let n = reject() record(n) } } }");
        var context = new GlyphExecutionContext { Graph = executable.CreateExecutionGraph() };
        await _runtime.Interpreter.ExecuteStageAsync(context, "stage.interaction_attempted");
        Assert.That(context.ExecutionHalted, Is.False);
        Assert.That(context.ShouldBlockInteraction, Is.True);
        Assert.That(context.BlockInteractionMessage, Is.EqualTo("blocked"));
        Assert.That(_probe.Values, Is.Empty);
    }

    private sealed class ProbeModule : IGlyphModule
    {
        public List<int> Values { get; } = [];
        public List<string> Texts { get; } = [];
        public int Calls, Reads, AggregateCalls;
        public void Configure(GlyphModuleBuilder glyph)
        {
            glyph.Add(new ProbeNode("record", [Pins.InInt("value", "Value")], null, async cx => { Values.Add(await cx.InInt("value")); return null; }, action: true));
            glyph.Add(new ProbeNode("record_text", [Pins.InString("value", "Value")], null, async cx => { Texts.Add(await cx.InString("value")); return null; }, action: true));
            glyph.Add(new ProbeNode("next_value", [], Pins.Out("value", "Value", GlyphDataType.Int), cx => Task.FromResult<object?>(++Calls), action: true));
            glyph.Add(new ProbeNode("read_count", [], Pins.Out("value", "Value", GlyphDataType.Int), cx => { Reads++; return Task.FromResult<object?>(cx.Execution.SpawnCount); }));
            glyph.Add(new ProbeNode("objects", [], Pins.Out("value", "Value", GlyphDataType.List) with { ElementType = GlyphDataType.NwObject }, cx => Task.FromResult<object?>(new uint[] { NWN.Core.NWScript.OBJECT_INVALID, 1, 2, 3 })));
            glyph.Add(new ProbeNode("numbers", [], Pins.Out("value", "Value", GlyphDataType.List) with { ElementType = GlyphDataType.Int }, cx => Task.FromResult<object?>(new[] { 1, 2, 3 })));
            glyph.Add(new ProbeNode("effect_values", [], Pins.Out("value", "Value", GlyphDataType.List) with { ElementType = GlyphDataType.Effect }, cx => Task.FromResult<object?>(new[] { default(Nwn.GlyphNwnEffect), default(Nwn.GlyphNwnEffect), default(Nwn.GlyphNwnEffect) })));
            glyph.Add(new ProbeNode("query_request", [], Pins.Out("value", "Value", GlyphDataType.Aggregate) with { AggregateTypeName = "Request" }, cx => Task.FromResult<object?>(new GlyphAggregateValue("Request", null, new Dictionary<string, GlyphAggregateFieldValue>
            { ["amount"] = new(GlyphDataType.Int, "Int", 3), ["text"] = new(GlyphDataType.String, "String", "runtime") })), action: true));
            glyph.Add(new ProbeNode("identity_result", [Pins.In("result", "Result", GlyphDataType.Aggregate) with { AggregateTypeName = "TestResult" }], Pins.Out("value", "Value", GlyphDataType.Aggregate) with { AggregateTypeName = "TestResult" }, cx => cx.Raw("result")));
            glyph.Add(new ProbeNode("query_result", [], Pins.Out("value", "Value", GlyphDataType.Aggregate) with { AggregateTypeName = "TestResult" }, cx =>
            {
                AggregateCalls++;
                GlyphAggregateValue value = cx.Execution.SpawnCount > 0
                    ? new("TestResult", "Found", new Dictionary<string, GlyphAggregateFieldValue>
                    { ["number"] = new(GlyphDataType.Int, "Int", cx.Execution.SpawnCount), ["text"] = new(GlyphDataType.String, "String", "runtime"), ["target"] = new(GlyphDataType.NwObject, "Object", NWN.Core.NWScript.OBJECT_INVALID) })
                    : new("TestResult", "Missing", new Dictionary<string, GlyphAggregateFieldValue> { ["reason"] = new(GlyphDataType.String, "String", "none") });
                return Task.FromResult<object?>(value);
            }, action: true));
        }
    }
    private sealed class ProbeNode(string name, List<GlyphPin> parameters, GlyphPin? result, Func<GlyphNodeContext, Task<object?>> run, bool action = false) : GlyphNodeBase
    {
        public override string TypeId => "probe." + name;
        public override GlyphNodeDefinition CreateDefinition() => new GlyphIntrinsicDescriptor
        {
            TypeId = TypeId, DisplayName = name, Category = "Tests", Archetype = action ? GlyphNodeArchetype.Action : GlyphNodeArchetype.PureFunction,
            Parameters = parameters, Results = result == null ? [] : [result], Exports = [new(name, result?.Id)]
        }.CreateDefinition();
        public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
        {
            object? value = await run(cx);
            return new() { NextExecPinId = action ? "exec_out" : null, OutputValues = result == null ? [] : new() { ["value"] = value } };
        }
    }
}
