using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Constants;

/// <summary>
/// Outputs a constant boolean value configured via PropertyOverrides.
/// </summary>
public class BoolConstantExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "constant.bool";
    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        object? val = await resolveInput("value");
        bool result = val switch
        {
            bool b => b,
            string s => s.Equals("true", StringComparison.OrdinalIgnoreCase),
            _ => Convert.ToBoolean(val ?? false)
        };

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["out"] = result
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Bool Constant",
        Category = "Constants",
        Description = "Outputs a constant boolean value (true/false). Set the value in the property panel.",
        ColorClass = "node-getter",
        InputPins =
        [
            new GlyphPin { Id = "value", Name = "Value", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Input, DefaultValue = "false" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "out", Name = "Value", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output }
        ]
    };
}
