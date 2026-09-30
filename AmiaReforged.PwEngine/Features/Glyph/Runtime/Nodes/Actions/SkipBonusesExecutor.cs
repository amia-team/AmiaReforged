using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Skips the data-driven bonus pipeline (ApplyBonuses) for the current creature.
/// Only effective during <see cref="GlyphEventType.OnCreatureSpawn"/> or
/// <see cref="GlyphEventType.OnBossSpawn"/> graph execution.
/// </summary>
[GlyphNode]
public partial class SkipBonusesExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.skip_bonuses";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        context.ShouldSkipBonuses = true;
        return Task.FromResult(GlyphNodeResult.Continue("exec_out"));
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("spawn.skip_bonuses", null)
        ],
        DisplayName = "Skip Bonuses",
        Category = "Actions",
        Description = "Prevents the data-driven bonus pipeline from being applied to this creature. " +
                      "Only works in OnCreatureSpawn and OnBossSpawn graphs. Use this when the Glyph " +
                      "graph applies its own custom bonuses.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        Parameters =
        [
        ]
    };
}
