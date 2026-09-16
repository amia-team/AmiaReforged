using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Constants;

/// <summary>
/// Outputs a constant integer value configured via PropertyOverrides.
/// </summary>
public sealed class IntConstantExecutor : GlyphPureNode
{
    public const string NodeTypeId = "constant.int";
    public override string TypeId => NodeTypeId;

    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx) =>
        new()
        {
            ["out"] = await cx.InInt("value"),
        };

    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Int Constant",
        Category = "Constants",
        Description = "Outputs a constant integer value. Set the value in the property panel.",
        ColorClass = "node-getter",
        InputPins = [Pins.InInt("value", "Value")],
        OutputPins = [Pins.Out("out", "Value", GlyphDataType.Int)],
    };
}
