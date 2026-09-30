using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Skips the data-driven mutation pipeline (TryApplyMutation) for the current creature.
/// Only effective during <see cref="GlyphEventType.OnCreatureSpawn"/> graph execution.
/// </summary>
[GlyphNode]
public partial class SkipMutationsExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.skip_mutations";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        context.ShouldSkipMutations = true;
        return Task.FromResult(GlyphNodeResult.Continue("exec_out"));
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("spawn.skip_mutations", null)
        ],
        DisplayName = "Skip Mutations",
        Category = "Actions",
        Description = "Prevents the data-driven mutation pipeline from being applied to this creature. " +
                      "Only works in OnCreatureSpawn graphs. Use this when the Glyph graph applies " +
                      "its own custom mutations or you want the creature unmodified.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        Parameters =
        [
        ]
    };
}
