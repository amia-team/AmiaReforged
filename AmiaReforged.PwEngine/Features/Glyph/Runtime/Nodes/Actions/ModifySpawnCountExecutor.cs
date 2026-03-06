using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Modifies the spawn count for the current group. Only effective during
/// <see cref="GlyphEventType.BeforeGroupSpawn"/> graph execution.
/// </summary>
public class ModifySpawnCountExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.modify_spawn_count";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? newCountValue = await resolveInput("new_count");
        int newCount = Convert.ToInt32(newCountValue);

        // Clamp to a reasonable range
        context.SpawnCount = System.Math.Clamp(newCount, 0, 100);

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Modify Spawn Count",
        Category = "Actions",
        Description = "Changes the number of creatures that will spawn for this group. " +
                      "Only works in BeforeGroupSpawn graphs.",
        ColorClass = "node-action",
        RestrictToEventType = GlyphEventType.BeforeGroupSpawn,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "new_count", Name = "New Count", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Input, DefaultValue = "1" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
