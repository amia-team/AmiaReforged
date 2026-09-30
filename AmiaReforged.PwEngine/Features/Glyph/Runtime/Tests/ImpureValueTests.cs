using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;
using NUnit.Framework;
namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

[TestFixture]
public sealed class ImpureValueTests
{
    private sealed class Module(List<string> calls) : IGlyphModule
    {
        public void Configure(GlyphModuleBuilder builder)
        {
            builder.Add(() => new Operation("test.create", calls, GlyphDataType.NwObject, true));
            builder.Add(() => new Operation("test.check", calls, GlyphDataType.Bool, true));
            builder.Add(() => new Operation("test.consume", calls, null, true));
            builder.Add(() => new Operation("test.objects", calls, GlyphDataType.List, false));
        }
    }
    private sealed class Operation(string id, List<string> calls, GlyphDataType? resultType, bool action) : GlyphNodeBase
    {
        public override string TypeId => id;
        public override GlyphNodeDefinition CreateDefinition() => new GlyphIntrinsicDescriptor
        {
            TypeId = id, DisplayName = id, Category = "Test", Archetype = action ? GlyphNodeArchetype.Action : GlyphNodeArchetype.PureFunction,
            Parameters = id == "test.consume" ? [Pins.InObject("value", "Value")] : [],
            Results = resultType == null ? [] : [Pins.Out("value", "Value", resultType.Value)],
            Exports = [new(id, resultType == null ? null : "value")]
        }.CreateDefinition();
        public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
        {
            if (id == "test.consume") calls.Add("consume:" + await cx.InObject("value"));
            else calls.Add(id);
            object? value = id switch { "test.create" => (uint)calls.Count, "test.check" => true, "test.objects" => new uint[] { 1, 2, 3 }, _ => null };
            return new() { NextExecPinId = action ? "exec_out" : null, OutputValues = resultType == null ? [] : new() { ["value"] = value } };
        }
    }
    private static async Task<(List<string> Calls, GlyphExecutable Program)> Run(string body, string globals = "")
    {
        List<string> calls = [];
        GlyphBootstrap runtime = new(new GlyphNodeDefinitionRegistry(), [new Module(calls)]);
        var result = runtime.Compiler.Compile(globals + " glyph t : encounter.before_group_spawn { " + body + " }");
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
        var cx = new GlyphExecutionContext { Graph = result.Executable!.CreateExecutionGraph(), EnableTracing = true };
        Assert.That(await runtime.Interpreter.ExecuteAsync(cx), Is.True);
        Assert.That(cx.TraceLog.Any(t => t.Contains("ERROR")), Is.False, string.Join("\n", cx.TraceLog));
        return (calls, result.Executable!);
    }
    [Test] public async Task Unused_creation_executes_at_the_let_and_consumers_share_one_result()
    {
        var (calls, _) = await Run("let unused = test.create() let created = test.create() test.consume(created) test.consume(created)");
        Assert.That(calls, Is.EqualTo(new[] { "test.create", "test.create", "consume:2", "consume:2" }));
    }
    [Test] public async Task Nested_actions_preserve_order_and_do_not_execute_in_skipped_branches()
    {
        var (calls, _) = await Run("test.consume(test.create()) if false { let skipped = test.create() } test.consume(test.create())");
        Assert.That(calls, Is.EqualTo(new[] { "test.create", "consume:1", "test.create", "consume:3" }));
    }
    [Test] public async Task Creation_runs_once_per_iteration_and_outer_results_survive_the_loop()
    {
        var (calls, _) = await Run("let outer = test.create() foreach item in test.objects() { let inside = test.create() test.consume(inside) test.consume(inside) test.consume(outer) } test.consume(outer)");
        Assert.That(calls.Count(c => c == "test.create"), Is.EqualTo(4));
        Assert.That(calls.Count(c => c == "consume:1"), Is.EqualTo(4));
        foreach (var created in new[] { "consume:3", "consume:7", "consume:11" }) Assert.That(calls.Count(c => c == created), Is.EqualTo(2));
    }
    [Test] public async Task Short_circuiting_skips_impure_values_and_evaluates_required_values_once()
    {
        var (calls, _) = await Run("if false && test.check() { test.consume(test.create()) } if true || test.check() { } if true && test.check() { } if false || test.check() { }");
        Assert.That(calls, Is.EqualTo(new[] { "test.check", "test.check" }));
    }
    [Test] public async Task Aggregates_capture_impure_fields_at_the_let_even_when_unused()
    {
        var (calls, _) = await Run("let unused = Holder(test.create()) let holder = Holder(test.create()) test.consume(holder.actor) test.consume(holder.actor)", "struct Holder { actor: Object }");
        Assert.That(calls, Is.EqualTo(new[] { "test.create", "test.create", "consume:2", "consume:2" }));
    }
    [Test] public async Task Scalar_equality_compares_strings_bools_and_object_handles()
    {
        var (calls, _) = await Run("if \"quest_item\" == \"quest_item\" && true != false && OBJECT.INVALID == OBJECT.INVALID { test.consume(test.create()) } if \"key\" == \"other\" { test.consume(test.create()) }");
        Assert.That(calls, Is.EqualTo(new[] { "test.create", "consume:1" }));
    }
    [Test] public async Task Global_function_arguments_with_side_effects_are_evaluated_once()
    {
        var (calls, _) = await Run("if same(test.create()) { } let nested = identity(identity(test.create())) test.consume(nested)",
            "fn same(obj: Object): Bool = obj == obj fn identity(obj: Object): Object = obj");
        Assert.That(calls, Is.EqualTo(new[] { "test.create", "test.create", "consume:2" }));
    }
}
