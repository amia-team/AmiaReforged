using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Cancels the current group spawn entirely. Only effective during
/// <see cref="GlyphEventType.BeforeGroupSpawn"/> graph execution.
/// Sets <see cref="GlyphExecutionContext.ShouldCancelSpawn"/> to true.
/// </summary>
public class CancelSpawnExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.cancel_spawn";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        context.ShouldCancelSpawn = true;
        return Task.FromResult(GlyphNodeResult.Continue("exec_out"));
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Cancel Spawn",
        Category = "Actions",
        Description = "Prevents the current spawn group from spawning. Only works in BeforeGroupSpawn graphs.",
        ColorClass = "node-action",
        RestrictToEventType = GlyphEventType.BeforeGroupSpawn,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
