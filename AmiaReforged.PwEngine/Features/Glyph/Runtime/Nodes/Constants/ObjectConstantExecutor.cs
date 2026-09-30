using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Constants;
[GlyphNode]
public sealed class ObjectConstantExecutor : GlyphPureNode
{
    public override string TypeId => "constant.object";
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = "Object constant", Category = "Constants",
        InputPins = [Pins.In("value", "Value", GlyphDataType.NwObject)],
        OutputPins = [Pins.Out("out", "Object", GlyphDataType.NwObject)]
    };
    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx) =>
        new() { ["out"] = await cx.InObject("value") };
}
