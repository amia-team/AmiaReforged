using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

[GlyphNode]
public sealed class ContinueExecutor : GlyphNodeBase
{
    public override string TypeId => "flow.continue";
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = "Continue", Category = "Flow Control", Archetype = GlyphNodeArchetype.FlowControl,
        InputPins = [Pins.ExecIn()]
    };
    public override Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx) => Task.FromResult(GlyphNodeResult.ContinueLoop());
}

/// <summary>The body begins with the lowered condition prelude and branch. A false
/// condition breaks this frame; normal body termination re-enters the prelude.</summary>
[GlyphNode]
public sealed class WhileExecutor : GlyphNodeBase
{
    public override string TypeId => "flow.while";
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = "While", Category = "Flow Control", Archetype = GlyphNodeArchetype.FlowControl,
        InputPins = [Pins.ExecIn()], OutputPins = [Pins.ExecOut("loop_body", "Condition and body"), Pins.ExecOut("completed", "Completed")]
    };
    public override Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx) => Task.FromResult(GlyphNodeResult.LoopBody("loop_body", []));
}

[GlyphNode]
public sealed class ForRangeExecutor : GlyphNodeBase
{
    private sealed record RangeState(long Value, int End, int Step, bool Inclusive);
    public override string TypeId => "flow.for_range";
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = "For Range", Category = "Flow Control", Archetype = GlyphNodeArchetype.FlowControl,
        InputPins = [Pins.ExecIn(), Pins.InInt("start", "Start"), Pins.InInt("end", "End"), Pins.InInt("step", "Step", "1"), Pins.InBool("inclusive", "Inclusive")],
        OutputPins = [Pins.ExecOut("loop_body", "Body"), Pins.ExecOut("completed", "Completed"), Pins.Out("element", "Value", GlyphDataType.Int)]
    };
    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        RangeState state;
        if (cx.Execution.LoopStates.TryGetValue(cx.Node.InstanceId, out object? existing))
        {
            var previous = (RangeState)existing;
            state = previous with { Value = previous.Value + previous.Step };
        }
        else
        {
            int start = await cx.InInt("start"), end = await cx.InInt("end");
            int step = cx.Prop("auto_step", "false") == "true" ? start <= end ? 1 : -1 : await cx.InInt("step");
            if (step == 0) throw new InvalidOperationException("GLYPH2014: A range step cannot be zero.");
            state = new(start, end, step, await cx.InBool("inclusive"));
        }
        // Long arithmetic makes crossing either Int boundary terminate without wrapping.
        bool within = state.Value >= int.MinValue && state.Value <= int.MaxValue &&
            (state.Step > 0 ? state.Inclusive ? state.Value <= state.End : state.Value < state.End
                           : state.Inclusive ? state.Value >= state.End : state.Value > state.End);
        if (!within)
        {
            cx.Execution.LoopStates.Remove(cx.Node.InstanceId);
            return GlyphNodeResult.Continue("completed");
        }
        cx.Execution.LoopStates[cx.Node.InstanceId] = state;
        return GlyphNodeResult.LoopBody("loop_body", new() { ["element"] = (int)state.Value });
    }
}

[GlyphNode]
public sealed class IntegerMathExecutor : GlyphPureNode
{
    public override string TypeId => "math.int_op";
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = "Integer arithmetic", Category = "Compiler",
        InputPins = [Pins.InInt("a", "A"), Pins.InInt("b", "B"), Pins.InString("operator", "Operator", "+")],
        OutputPins = [Pins.Out("result", "Result", GlyphDataType.Int)]
    };
    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx)
    {
        int a = await cx.InInt("a"), b = await cx.InInt("b");
        int value = (await cx.InString("operator", "+")) switch
        {
            "+" => unchecked(a + b), "-" => unchecked(a - b), "*" => unchecked(a * b),
            "%" => b == 0 || b == -1 ? 0 : a % b, _ => 0
        };
        return new() { ["result"] = value };
    }
}

[GlyphNode]
public sealed class AggregateNewExecutor : GlyphPureNode
{
    public override string TypeId => "aggregate.new";
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = "Aggregate constructor", Category = "Compiler",
        OutputPins = [Pins.Out("value", "Value", GlyphDataType.Aggregate)]
    };
    protected override Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx) =>
        Task.FromResult(new Dictionary<string, object?> { ["value"] = new GlyphAggregateValue(cx.Prop("type", ""), cx.Prop("variant", "") is { Length: > 0 } variant ? variant : null) });
}

[GlyphNode]
public sealed class AggregateVariantExecutor : GlyphPureNode
{
    public override string TypeId => "aggregate.is_variant";
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = "Variant test", Category = "Compiler",
        InputPins = [Pins.In("aggregate", "Aggregate", GlyphDataType.Aggregate)], OutputPins = [Pins.Out("result", "Result", GlyphDataType.Bool)]
    };
    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx)
    {
        var value = await cx.Raw("aggregate") as GlyphAggregateValue;
        return new() { ["result"] = value?.TypeName == cx.Prop("type", "") && value.VariantName == cx.Prop("variant", "") };
    }
}
