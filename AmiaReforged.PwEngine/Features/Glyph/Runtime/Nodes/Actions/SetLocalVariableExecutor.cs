using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Sets a local variable on an NWN object. Supports int, string, and float types.
/// </summary>
public class SetLocalVariableExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.set_local_variable";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? objectValue = await resolveInput("target");
        object? varNameValue = await resolveInput("var_name");
        object? varValue = await resolveInput("var_value");
        object? varTypeValue = await resolveInput("var_type");

        uint target = Convert.ToUInt32(objectValue);
        string varName = varNameValue?.ToString() ?? string.Empty;
        string varType = varTypeValue?.ToString()?.ToUpperInvariant() ?? "INT";

        if (target == NWScript.OBJECT_INVALID || string.IsNullOrEmpty(varName))
            return GlyphNodeResult.Continue("exec_out");

        switch (varType)
        {
            case "INT":
                NWScript.SetLocalInt(target, varName, Convert.ToInt32(varValue));
                break;
            case "STRING":
                NWScript.SetLocalString(target, varName, varValue?.ToString() ?? string.Empty);
                break;
            case "FLOAT":
                NWScript.SetLocalFloat(target, varName, Convert.ToSingle(varValue));
                break;
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Set Local Variable",
        Category = "Actions",
        Description = "Sets a local variable (int, string, or float) on an NWN object.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "target", Name = "Target", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "var_name", Name = "Variable Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "" },
            new GlyphPin { Id = "var_value", Name = "Value", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "0" },
            new GlyphPin { Id = "var_type", Name = "Type (INT/STRING/FLOAT)", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "INT" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
