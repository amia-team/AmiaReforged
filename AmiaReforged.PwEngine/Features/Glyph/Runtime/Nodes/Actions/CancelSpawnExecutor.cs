using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Cancels the current group spawn entirely. Only effective during
/// <see cref="GlyphEventType.BeforeGroupSpawn"/> graph execution.
/// Sets <see cref="GlyphExecutionContext.ShouldCancelSpawn"/> to true.
/// </summary>
[GlyphNode]
public partial class CancelSpawnExecutor : IGlyphNodeExecutor
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

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("spawn.cancel", null)
        ],
        DisplayName = "Cancel Spawn",
        Category = "Actions",
        Description = "Prevents the current spawn group from spawning. Only works in BeforeGroupSpawn graphs.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        RestrictToEventType = GlyphEventType.BeforeGroupSpawn,
        Parameters =
        [
        ]
    };
}
