using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Constants;

/// <summary>
/// Outputs a constant floating-point value configured via PropertyOverrides.
/// </summary>
public class FloatConstantExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "constant.float";
    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        object? val = await resolveInput("value");
        double result = Convert.ToDouble(val ?? 0.0);

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["out"] = result
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Float Constant",
        Category = "Constants",
        Description = "Outputs a constant floating-point value. Set the value in the property panel.",
        ColorClass = "node-getter",
        InputPins =
        [
            new GlyphPin { Id = "value", Name = "Value", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Input, DefaultValue = "0.0" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "out", Name = "Value", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Output }
        ]
    };
}
