using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Math;

/// <summary>
/// Compares two values (A and B) using a specified operator and outputs a boolean result.
/// Supports ==, !=, &lt;, &gt;, &lt;=, &gt;= operators.
/// </summary>
public class CompareExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "math.compare";
    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        object? aValue = await resolveInput("a");
        object? bValue = await resolveInput("b");
        object? opValue = await resolveInput("operator");

        double a = Convert.ToDouble(aValue);
        double b = Convert.ToDouble(bValue);
        string op = opValue?.ToString() ?? "==";

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

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["result"] = result
        });
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Compare",
        Category = "Math / Logic",
        Description = "Compares two numeric values using the specified operator. Returns a boolean result.",
        ColorClass = "node-math",
        InputPins =
        [
            new GlyphPin { Id = "a", Name = "A", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Input, DefaultValue = "0" },
            new GlyphPin { Id = "b", Name = "B", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Input, DefaultValue = "0" },
            new GlyphPin { Id = "operator", Name = "Operator", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "==" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "result", Name = "Result", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output }
        ]
    };
}
