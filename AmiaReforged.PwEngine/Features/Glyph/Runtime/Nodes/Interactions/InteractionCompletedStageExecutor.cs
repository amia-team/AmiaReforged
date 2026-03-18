using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pipeline stage node for the "Completed" phase of an interaction pipeline.
/// Final stage in the pipeline: Attempted → Started → Tick → Completed.
/// Fires when all required rounds finish, before the data-driven response system.
/// Route to <c>interaction.fail</c> to cancel the session at completion time.
/// </summary>
public class InteractionCompletedStageExecutor : InteractionStageExecutorBase
{
    public const string NodeTypeId = "stage.interaction_completed";

    public override string TypeId => NodeTypeId;

    protected override void AddStageOutputs(Dictionary<string, object?> outputs, GlyphExecutionContext context)
    {
        outputs["session_id"] = context.InteractionSessionId.ToString();
        outputs["proficiency"] = context.InteractionProficiency ?? string.Empty;
        outputs["response_tag"] = context.InteractionResponseTag ?? string.Empty;
    }

    protected override (string TypeId, string DisplayName, string Description, List<GlyphPin> ExtraOutputPins) CreateStageDefinition() =>
    (
        NodeTypeId,
        "4. Completed",
        "Final stage in the interaction pipeline. Fires when all rounds finish, " +
        "before the data-driven response system. Route to Fail Interaction to cancel.",
        [
            new GlyphPin { Id = "session_id", Name = "Session ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "proficiency", Name = "Proficiency", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "response_tag", Name = "Response Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
        ]
    );
}
