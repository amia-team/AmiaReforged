using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Math;

/// <summary>
/// Compares two values (A and B) using a specified operator and outputs a boolean result.
/// Supports ==, !=, &lt;, &gt;, &lt;=, &gt;= operators.
/// </summary>
public sealed class CompareExecutor : GlyphPureNode
{
    public const string NodeTypeId = "math.compare";
    public override string TypeId => NodeTypeId;

    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx)
    {
        double a = await cx.InFloat("a");
        double b = await cx.InFloat("b");
        string op = await cx.InString("operator", "==");

        bool result = op switch
        {
            "==" => System.Math.Abs(a - b) < 0.0001,
            "!=" => System.Math.Abs(a - b) >= 0.0001,
            "<" => a < b,
            ">" => a > b,
            "<=" => a <= b,
            ">=" => a >= b,
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
        DisplayName = "Compare",
        Category = "Math / Logic",
        Description = "Compares two numeric values using the specified operator. Returns a boolean result.",
        ColorClass = "node-math",
        InputPins =
        [
            Pins.InFloat("a", "A"),
            Pins.InFloat("b", "B"),
            Pins.InString("operator", "Operator", "=="),
        ],
        OutputPins =
        [
            Pins.Out("result", "Result", GlyphDataType.Bool),
        ]
    };
}
