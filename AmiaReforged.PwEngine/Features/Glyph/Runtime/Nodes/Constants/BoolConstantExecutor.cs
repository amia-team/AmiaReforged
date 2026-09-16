using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Constants;

/// <summary>
/// Outputs a constant boolean value configured via PropertyOverrides.
/// </summary>
public sealed class BoolConstantExecutor : GlyphPureNode
{
    public const string NodeTypeId = "constant.bool";
    public override string TypeId => NodeTypeId;

    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx) =>
        new()
        {
            ["out"] = await cx.InBool("value"),
        };

    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Bool Constant",
        Category = "Constants",
        Description = "Outputs a constant boolean value (true/false). Set the value in the property panel.",
        ColorClass = "node-getter",
        InputPins = [Pins.InBool("value", "Value")],
        OutputPins = [Pins.Out("out", "Value", GlyphDataType.Bool)],
    };
}
