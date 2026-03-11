using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Skips the data-driven mutation pipeline (TryApplyMutation) for the current creature.
/// Only effective during <see cref="GlyphEventType.OnCreatureSpawn"/> graph execution.
/// </summary>
public class SkipMutationsExecutor : IGlyphNodeExecutor
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

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Skip Mutations",
        Category = "Actions",
        Description = "Prevents the data-driven mutation pipeline from being applied to this creature. " +
                      "Only works in OnCreatureSpawn graphs. Use this when the Glyph graph applies " +
                      "its own custom mutations or you want the creature unmodified.",
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
