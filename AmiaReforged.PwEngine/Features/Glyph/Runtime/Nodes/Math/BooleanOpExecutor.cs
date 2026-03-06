using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Math;

/// <summary>
/// Performs a boolean logic operation (AND, OR, XOR) on two boolean inputs.
/// </summary>
public class BooleanOpExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "math.boolean_op";
    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        object? aValue = await resolveInput("a");
        object? bValue = await resolveInput("b");
        object? opValue = await resolveInput("operator");

        bool a = Convert.ToBoolean(aValue);
        bool b = Convert.ToBoolean(bValue);
        string op = opValue?.ToString()?.ToUpperInvariant() ?? "AND";

        bool result = op switch
        {
            "AND" => a && b,
            "OR" => a || b,
            "XOR" => a ^ b,
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
        DisplayName = "Boolean Op",
        Category = "Math / Logic",
        Description = "Performs a boolean logic operation (AND, OR, XOR) on two inputs.",
        ColorClass = "node-math",
        InputPins =
        [
            new GlyphPin { Id = "a", Name = "A", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Input, DefaultValue = "false" },
            new GlyphPin { Id = "b", Name = "B", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Input, DefaultValue = "false" },
            new GlyphPin { Id = "operator", Name = "Operator", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "AND" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "result", Name = "Result", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output }
        ]
    };
}
