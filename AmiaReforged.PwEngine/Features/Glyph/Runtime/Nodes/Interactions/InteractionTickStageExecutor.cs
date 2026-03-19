using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pipeline stage node for the "Tick" phase of an interaction pipeline.
/// Third stage in the pipeline: Attempted → Started → Tick → Completed.
/// Fires each round/tick of an active interaction. Provides progress info.
/// Route to <c>interaction.fail</c> to cancel the interaction mid-progress.
/// </summary>
public class InteractionTickStageExecutor : InteractionStageExecutorBase
{
    public const string NodeTypeId = "stage.interaction_tick";

    public override string TypeId => NodeTypeId;

    public override string SourceDisplayName => "Tick";

    protected override void AddStageContextPins(List<ContextPinDescriptor> pins)
    {
        pins.Add(new("session_id", "Session ID", GlyphDataType.String,
            ctx => ctx.InteractionSessionId.ToString()));
        pins.Add(new("progress", "Progress", GlyphDataType.Int,
            ctx => ctx.InteractionProgress));
        pins.Add(new("required_rounds", "Required Rounds", GlyphDataType.Int,
            ctx => ctx.InteractionRequiredRounds));
        pins.Add(new("proficiency", "Proficiency", GlyphDataType.String,
            ctx => ctx.InteractionProficiency ?? string.Empty));
    }

    protected override void AddStageOutputs(Dictionary<string, object?> outputs, GlyphExecutionContext context)
    {
        outputs["session_id"] = context.InteractionSessionId.ToString();
        outputs["progress"] = context.InteractionProgress;
        outputs["required_rounds"] = context.InteractionRequiredRounds;
        outputs["proficiency"] = context.InteractionProficiency ?? string.Empty;
    }

    protected override (string TypeId, string DisplayName, string Description, List<GlyphPin> ExtraOutputPins) CreateStageDefinition() =>
    (
        NodeTypeId,
        "3. Tick",
        "Third stage in the interaction pipeline. Fires each round of an active interaction. " +
        "Route to Fail Interaction to cancel mid-progress.",
        [
            new GlyphPin { Id = "session_id", Name = "Session ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "progress", Name = "Progress", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "required_rounds", Name = "Required Rounds", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "proficiency", Name = "Proficiency", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
        ]
    );
}
