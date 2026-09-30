using AmiaReforged.PwEngine.Features.Glyph.Core;

using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Constants;

/// <summary>
/// Outputs a constant floating-point value configured via PropertyOverrides.
/// </summary>
[GlyphNode]
public sealed partial class FloatConstantExecutor : GlyphPureNode
{
    public const string NodeTypeId = "constant.float";
    public override string TypeId => NodeTypeId;

    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx) =>
        new()
        {
            ["out"] = await cx.InFloat("value"),
        };

    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Float Constant",
        Category = "Constants",
        Description = "Outputs a constant floating-point value. Set the value in the property panel.",
        ColorClass = "node-getter",
        InputPins = [Pins.InFloat("value", "Value", "0.0")],
        OutputPins = [Pins.Out("out", "Value", GlyphDataType.Float)],
    };
}
