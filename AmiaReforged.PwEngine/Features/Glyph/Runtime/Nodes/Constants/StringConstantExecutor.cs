using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Constants;

/// <summary>
/// Outputs a constant string value configured via PropertyOverrides.
/// </summary>
public sealed class StringConstantExecutor : GlyphPureNode
{
    public const string NodeTypeId = "constant.string";
    public override string TypeId => NodeTypeId;

    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx) =>
        new()
        {
            ["out"] = await cx.InString("value"),
        };

    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "String Constant",
        Category = "Constants",
        Description = "Outputs a constant string value. Set the value in the property panel.",
        ColorClass = "node-getter",
        InputPins = [Pins.InString("value", "Value", "")],
        OutputPins = [Pins.Out("out", "Value", GlyphDataType.String)],
    };
}
