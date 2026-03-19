using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pipeline stage node for the "Started" phase of an interaction pipeline.
/// Second stage in the pipeline: Attempted → Started → Tick → Completed.
/// Fires after the interaction session has been created. Provides session details
/// and allows setup logic. Route to <c>interaction.fail</c> to cancel the session.
/// </summary>
public class InteractionStartedStageExecutor : InteractionStageExecutorBase
{
    public const string NodeTypeId = "stage.interaction_started";

    public override string TypeId => NodeTypeId;

    public override string SourceDisplayName => "Started";

    protected override void AddStageContextPins(List<ContextPinDescriptor> pins)
    {
        pins.Add(new("target_mode", "Target Mode", GlyphDataType.String,
            ctx => ctx.InteractionTargetMode ?? string.Empty));
        pins.Add(new("session_id", "Session ID", GlyphDataType.String,
            ctx => ctx.InteractionSessionId.ToString()));
        pins.Add(new("required_rounds", "Required Rounds", GlyphDataType.Int,
            ctx => ctx.InteractionRequiredRounds));
        pins.Add(new("proficiency", "Proficiency", GlyphDataType.String,
            ctx => ctx.InteractionProficiency ?? string.Empty));
    }

    protected override void AddStageOutputs(Dictionary<string, object?> outputs, GlyphExecutionContext context)
    {
        outputs["target_mode"] = context.InteractionTargetMode ?? string.Empty;
        outputs["session_id"] = context.InteractionSessionId.ToString();
        outputs["required_rounds"] = context.InteractionRequiredRounds;
        outputs["proficiency"] = context.InteractionProficiency ?? string.Empty;
    }

    protected override (string TypeId, string DisplayName, string Description, List<GlyphPin> ExtraOutputPins) CreateStageDefinition() =>
    (
        NodeTypeId,
        "2. Started",
        "Second stage in the interaction pipeline. Fires after the interaction session " +
        "is created. Use for setup logic, VFX, or messages. Route to Fail Interaction to cancel.",
        [
            new GlyphPin { Id = "target_mode", Name = "Target Mode", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "session_id", Name = "Session ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "required_rounds", Name = "Required Rounds", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "proficiency", Name = "Proficiency", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
        ]
    );
}
