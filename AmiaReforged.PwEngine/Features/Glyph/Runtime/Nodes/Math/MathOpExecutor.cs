using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Math;

/// <summary>
/// Performs a basic arithmetic operation on two numeric values.
/// Supports +, -, *, / operators.
/// </summary>
public class MathOpExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "math.math_op";
    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        object? aValue = await resolveInput("a");
        object? bValue = await resolveInput("b");
        object? opValue = await resolveInput("operator");

        double a = Convert.ToDouble(aValue);
        double b = Convert.ToDouble(bValue);
        string op = opValue?.ToString() ?? "+";

        double result = op switch
        {
            "+" => a + b,
            "-" => a - b,
            "*" => a * b,
            "/" => b != 0 ? a / b : 0,
            "%" => b != 0 ? a % b : 0,
            _ => 0
        };

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["result"] = result
        });
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Math Op",
        Category = "Math / Logic",
        Description = "Performs arithmetic on two numbers. Supports +, -, *, /, % operators.",
        ColorClass = "node-math",
        InputPins =
        [
            new GlyphPin { Id = "a", Name = "A", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Input, DefaultValue = "0" },
            new GlyphPin { Id = "b", Name = "B", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Input, DefaultValue = "0" },
            new GlyphPin { Id = "operator", Name = "Operator", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "+" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "result", Name = "Result", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Output }
        ]
    };
}
