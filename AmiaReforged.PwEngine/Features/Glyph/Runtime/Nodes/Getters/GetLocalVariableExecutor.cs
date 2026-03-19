using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets a local variable from an NWN object. Supports int, string, and float types.
/// </summary>
public class GetLocalVariableExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.local_variable";
    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        object? objectValue = await resolveInput("target");
        object? varNameValue = await resolveInput("var_name");
        object? varTypeValue = await resolveInput("var_type");

        uint target = Convert.ToUInt32(objectValue ?? 0);
        string varName = varNameValue?.ToString() ?? string.Empty;
        string varType = varTypeValue?.ToString()?.ToUpperInvariant() ?? "INT";

        if (target == NWScript.OBJECT_INVALID || string.IsNullOrEmpty(varName))
        {
            return new GlyphNodeResult
            {
                NextExecPinId = "exec_out",
                OutputValues = new Dictionary<string, object?>
                {
                    ["int_value"] = 0,
                    ["string_value"] = string.Empty,
                    ["float_value"] = 0.0f
                }
            };
        }

        int intVal = 0;
        string strVal = string.Empty;
        float floatVal = 0.0f;

        switch (varType)
        {
            case "INT":
                intVal = NWScript.GetLocalInt(target, varName);
                break;
            case "STRING":
                strVal = NWScript.GetLocalString(target, varName);
                break;
            case "FLOAT":
                floatVal = NWScript.GetLocalFloat(target, varName);
                break;
        }

        return new GlyphNodeResult
        {
            NextExecPinId = "exec_out",
            OutputValues = new Dictionary<string, object?>
            {
                ["int_value"] = intVal,
                ["string_value"] = strVal,
                ["float_value"] = (double)floatVal
            }
        };
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Local Variable",
        Category = "Getters",
        Description = "Gets a local variable (int, string, or float) from an NWN game object.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.Action,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "target", Name = "Target", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "var_name", Name = "Variable Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "" },
            new GlyphPin { Id = "var_type", Name = "Type (INT/STRING/FLOAT)", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "INT" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "int_value", Name = "Int Value", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "string_value", Name = "String Value", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "float_value", Name = "Float Value", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Output }
        ]
    };
}
