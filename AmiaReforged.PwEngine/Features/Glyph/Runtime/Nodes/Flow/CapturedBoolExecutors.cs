using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

/// <summary>Compiler storage for short-circuit expressions whose branches may contain actions.</summary>
[GlyphNode]
public sealed class CaptureBoolExecutor : GlyphActionNode
{
    public override string TypeId => "flow.capture_bool";
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = "Capture boolean", Category = "Compiler", Archetype = GlyphNodeArchetype.Action,
        InputPins = [Pins.ExecIn(), Pins.InBool("value", "Value")], OutputPins = [Pins.ExecOut("exec_out", "Then")]
    };
    protected override async Task RunActionAsync(GlyphNodeContext cx) =>
        cx.Execution.Variables["__bool_" + cx.Prop("slot", "")] = await cx.InBool("value");
}
[GlyphNode]
public sealed class CapturedBoolExecutor : GlyphPureNode
{
    public override string TypeId => "getter.captured_bool";
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = "Captured boolean", Category = "Compiler",
        OutputPins = [Pins.Out("value", "Value", GlyphDataType.Bool)]
    };
    protected override Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx) =>
        Task.FromResult(new Dictionary<string, object?> { ["value"] = cx.Execution.Variables.GetValueOrDefault("__bool_" + cx.Prop("slot", "")) ?? false });
}
