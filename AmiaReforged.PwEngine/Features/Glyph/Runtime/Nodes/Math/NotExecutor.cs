using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Math;

/// <summary>
/// Inverts a boolean value. True becomes False, False becomes True.
/// </summary>
public sealed class NotExecutor : GlyphPureNode
{
    public const string NodeTypeId = "math.not";
    public override string TypeId => NodeTypeId;

    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx) =>
        new()
        {
            ["result"] = !await cx.InBool("value"),
        };

    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "NOT",
        Category = "Math / Logic",
        Description = "Inverts a boolean value.",
        ColorClass = "node-math",
        InputPins = [Pins.InBool("value", "Value")],
        OutputPins = [Pins.Out("result", "Result", GlyphDataType.Bool)],
    };
}
