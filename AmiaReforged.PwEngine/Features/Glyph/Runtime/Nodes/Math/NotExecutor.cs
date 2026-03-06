using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Math;

/// <summary>
/// Inverts a boolean value. True becomes False, False becomes True.
/// </summary>
public class NotExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "math.not";
    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        object? value = await resolveInput("value");
        bool boolValue = Convert.ToBoolean(value);

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["result"] = !boolValue
        });
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "NOT",
        Category = "Math / Logic",
        Description = "Inverts a boolean value.",
        ColorClass = "node-math",
        InputPins =
        [
            new GlyphPin { Id = "value", Name = "Value", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Input, DefaultValue = "false" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "result", Name = "Result", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output }
        ]
    };
}
