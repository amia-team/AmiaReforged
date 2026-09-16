using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Math;

/// <summary>
/// Performs a boolean logic operation (AND, OR, XOR) on two boolean inputs.
/// </summary>
public sealed class BooleanOpExecutor : GlyphPureNode
{
    public const string NodeTypeId = "math.boolean_op";
    public override string TypeId => NodeTypeId;

    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx)
    {
        bool a = await cx.InBool("a");
        bool b = await cx.InBool("b");
        string op = (await cx.InString("operator", "AND")).ToUpperInvariant();

        bool result = op switch
        {
            "AND" => a && b,
            "OR" => a || b,
            "XOR" => a ^ b,
            _ => false
        };

        return new Dictionary<string, object?>
        {
            ["result"] = result
        };
    }

    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Boolean Op",
        Category = "Math / Logic",
        Description = "Performs a boolean logic operation (AND, OR, XOR) on two inputs.",
        ColorClass = "node-math",
        InputPins =
        [
            Pins.InBool("a", "A"),
            Pins.InBool("b", "B"),
            Pins.InString("operator", "Operator", "AND"),
        ],
        OutputPins =
        [
            Pins.Out("result", "Result", GlyphDataType.Bool),
        ]
    };
}
