using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Math;

/// <summary>
/// Performs a basic arithmetic operation on two numeric values.
/// Supports +, -, *, / operators.
/// </summary>
public sealed class MathOpExecutor : GlyphPureNode
{
    public const string NodeTypeId = "math.math_op";
    public override string TypeId => NodeTypeId;

    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx)
    {
        double a = await cx.InFloat("a");
        double b = await cx.InFloat("b");
        string op = await cx.InString("operator", "+");

        double result = op switch
        {
            "+" => a + b,
            "-" => a - b,
            "*" => a * b,
            "/" => b != 0 ? a / b : 0,
            "%" => b != 0 ? a % b : 0,
            _ => 0
        };

        return new Dictionary<string, object?>
        {
            ["result"] = result
        };
    }

    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Math Op",
        Category = "Math / Logic",
        Description = "Performs arithmetic on two numbers. Supports +, -, *, /, % operators.",
        ColorClass = "node-math",
        InputPins =
        [
            Pins.InFloat("a", "A"),
            Pins.InFloat("b", "B"),
            Pins.InString("operator", "Operator", "+"),
        ],
        OutputPins =
        [
            Pins.Out("result", "Result", GlyphDataType.Float),
        ]
    };
}
