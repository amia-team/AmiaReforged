using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Modifies the spawn count for the current group. Only effective during
/// <see cref="GlyphEventType.BeforeGroupSpawn"/> graph execution.
/// </summary>
[GlyphNode]
public partial class ModifySpawnCountExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.modify_spawn_count";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? newCountValue = await resolveInput(Inputs.NewCount);
        int newCount = Convert.ToInt32(newCountValue);

        // Clamp to a reasonable range
        context.SpawnCount = System.Math.Clamp(newCount, 0, 100);

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("spawn.modify_count", null)
        ],
        DisplayName = "Modify Spawn Count",
        Category = "Actions",
        Description = "Changes the number of creatures that will spawn for this group. " +
                      "Only works in BeforeGroupSpawn graphs.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        RestrictToEventType = GlyphEventType.BeforeGroupSpawn,
        Parameters =
        [
            Pins.In("new_count", "New Count", GlyphDataType.Int, "1")
        ]
    };
}
