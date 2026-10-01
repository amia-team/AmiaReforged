using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

[GlyphNode]
public sealed class FunctionExecutor : GlyphNodeBase
{
    public override string TypeId => "flow.function";
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = "Function body", Category = "Compiler", Archetype = GlyphNodeArchetype.FlowControl,
        InputPins = [Pins.ExecIn()], OutputPins = [Pins.ExecOut("body", "Body"), Pins.ExecOut("completed", "Completed")]
    };
    public override Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx) =>
        Task.FromResult(new GlyphNodeResult { NextExecPinId = "body", IsFunction = true });
}

[GlyphNode]
public sealed class ReturnExecutor : GlyphNodeBase
{
    public override string TypeId => "flow.return";
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = "Return", Category = "Compiler", Archetype = GlyphNodeArchetype.FlowControl,
        InputPins = [Pins.ExecIn()]
    };
    public override Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx) =>
        Task.FromResult(new GlyphNodeResult { IsReturn = true });
}
