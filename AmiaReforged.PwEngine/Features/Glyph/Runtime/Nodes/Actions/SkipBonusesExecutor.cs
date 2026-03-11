using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Skips the data-driven bonus pipeline (ApplyBonuses) for the current creature.
/// Only effective during <see cref="GlyphEventType.OnCreatureSpawn"/> or
/// <see cref="GlyphEventType.OnBossSpawn"/> graph execution.
/// </summary>
public class SkipBonusesExecutor : IGlyphNodeExecutor
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

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Skip Bonuses",
        Category = "Actions",
        Description = "Prevents the data-driven bonus pipeline from being applied to this creature. " +
                      "Only works in OnCreatureSpawn and OnBossSpawn graphs. Use this when the Glyph " +
                      "graph applies its own custom bonuses.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
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
