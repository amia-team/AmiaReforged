using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Constants;

/// <summary>
/// Outputs a constant integer value configured via PropertyOverrides.
/// </summary>
public class IntConstantExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "constant.int";
    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        object? val = await resolveInput("value");
        int result = Convert.ToInt32(val ?? 0);

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["out"] = result
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Int Constant",
        Category = "Constants",
        Description = "Outputs a constant integer value. Set the value in the property panel.",
        ColorClass = "node-getter",
        InputPins =
        [
            new GlyphPin { Id = "value", Name = "Value", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Input, DefaultValue = "0" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "out", Name = "Value", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
