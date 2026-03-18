using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Constants;

/// <summary>
/// Outputs a constant string value configured via PropertyOverrides.
/// </summary>
public class StringConstantExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "constant.string";
    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        object? val = await resolveInput("value");
        string result = val?.ToString() ?? string.Empty;

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["out"] = result
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "String Constant",
        Category = "Constants",
        Description = "Outputs a constant string value. Set the value in the property panel.",
        ColorClass = "node-getter",
        InputPins =
        [
            new GlyphPin { Id = "value", Name = "Value", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "out", Name = "Value", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output }
        ]
    };
}
