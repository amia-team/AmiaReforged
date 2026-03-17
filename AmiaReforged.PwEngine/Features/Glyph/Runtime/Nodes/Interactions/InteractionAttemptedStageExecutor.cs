using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pipeline stage node for the "Attempted" phase of an interaction pipeline.
/// This is the first stage in the causal chain: Attempted → Started → Tick → Completed.
/// Fires before precondition checks. Downstream nodes can inspect context and
/// route to <c>interaction.fail</c> to block the interaction from starting.
/// </summary>
public class InteractionAttemptedStageExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "stage.interaction_attempted";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        Dictionary<string, object?> outputs = new()
        {
            ["character_id"] = context.CharacterId ?? string.Empty,
            ["creature"] = context.InteractionCreature,
            ["interaction_tag"] = context.InteractionTag ?? string.Empty,
            ["target_id"] = context.InteractionTargetId.ToString(),
            ["target_mode"] = context.InteractionTargetMode ?? string.Empty,
            ["area_resref"] = context.InteractionAreaResRef ?? string.Empty,
            ["proficiency"] = context.InteractionProficiency ?? string.Empty
        };

        return Task.FromResult(new GlyphNodeResult
        {
            NextExecPinId = "exec_out",
            OutputValues = outputs
        });
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "1. Attempted",
        Category = "Pipeline Stages",
        Description = "First stage in the interaction pipeline. Fires when a character attempts to start " +
                      "an interaction, before precondition checks. Route to Fail Interaction to block it.",
        ColorClass = "node-stage",
        Archetype = GlyphNodeArchetype.PipelineStage,
        IsSingleton = true,
        RestrictToEventType = GlyphEventType.InteractionPipeline,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "character_id", Name = "Character ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "interaction_tag", Name = "Interaction Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "target_id", Name = "Target ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "target_mode", Name = "Target Mode", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "area_resref", Name = "Area ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "proficiency", Name = "Proficiency", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output }
        ]
    };
}
