using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pipeline stage node for the "Tick" phase of an interaction pipeline.
/// Third stage in the chain: Attempted → Started → Tick → Completed.
/// Fires each round/tick of an active interaction. Provides progress info.
/// Route to <c>interaction.fail</c> to cancel the interaction mid-progress.
/// </summary>
public class InteractionTickStageExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "stage.interaction_tick";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        // Resolve passthrough-overridable inputs (wired value wins, else context)
        object? charIn = await resolveInput("character_id");
        object? creatureIn = await resolveInput("creature");
        object? tagIn = await resolveInput("interaction_tag");
        object? targetIn = await resolveInput("target_id");

        Dictionary<string, object?> outputs = new()
        {
            ["character_id"] = charIn?.ToString() ?? context.CharacterId ?? string.Empty,
            ["creature"] = creatureIn ?? context.InteractionCreature,
            ["interaction_tag"] = tagIn?.ToString() ?? context.InteractionTag ?? string.Empty,
            ["target_id"] = targetIn?.ToString() ?? context.InteractionTargetId.ToString(),
            ["area_resref"] = context.InteractionAreaResRef ?? string.Empty,
            ["session_id"] = context.InteractionSessionId.ToString(),
            ["progress"] = context.InteractionProgress,
            ["required_rounds"] = context.InteractionRequiredRounds,
            ["proficiency"] = context.InteractionProficiency ?? string.Empty
        };

        return new GlyphNodeResult
        {
            NextExecPinId = "exec_out",
            OutputValues = outputs
        };
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "3. Tick",
        Category = "Pipeline Stages",
        Description = "Third stage in the interaction pipeline. Fires each round of an active interaction. " +
                      "Route to Fail Interaction to cancel mid-progress.",
        ColorClass = "node-stage",
        Archetype = GlyphNodeArchetype.PipelineStage,
        IsSingleton = true,
        RestrictToEventType = GlyphEventType.InteractionPipeline,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "character_id", Name = "Character ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "interaction_tag", Name = "Interaction Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "target_id", Name = "Target ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "character_id", Name = "Character ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "interaction_tag", Name = "Interaction Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "target_id", Name = "Target ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "area_resref", Name = "Area ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "session_id", Name = "Session ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "progress", Name = "Progress", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "required_rounds", Name = "Required Rounds", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "proficiency", Name = "Proficiency", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output }
        ]
    };
}
