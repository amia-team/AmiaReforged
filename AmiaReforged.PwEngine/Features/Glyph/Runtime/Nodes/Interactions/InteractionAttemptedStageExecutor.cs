using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pipeline stage node for the "Attempted" phase of an interaction pipeline.
/// First stage in the pipeline: Attempted → Started → Tick → Completed.
/// Fires before precondition checks. Downstream nodes can inspect context and
/// route to <c>interaction.fail</c> to block the interaction from starting.
/// </summary>
public class InteractionAttemptedStageExecutor : InteractionStageExecutorBase
{
    public const string NodeTypeId = "stage.interaction_attempted";

    public override string TypeId => NodeTypeId;

    public override string SourceDisplayName => "Attempted";

    protected override void AddStageContextPins(List<ContextPinDescriptor> pins)
    {
        pins.Add(new("target_mode", "Target Mode", GlyphDataType.String,
            ctx => ctx.InteractionTargetMode ?? string.Empty));
        pins.Add(new("proficiency", "Proficiency", GlyphDataType.String,
            ctx => ctx.InteractionProficiency ?? string.Empty));
    }

    protected override void AddStageOutputs(Dictionary<string, object?> outputs, GlyphExecutionContext context)
    {
        outputs["target_mode"] = context.InteractionTargetMode ?? string.Empty;
        outputs["proficiency"] = context.InteractionProficiency ?? string.Empty;
    }

    protected override (string TypeId, string DisplayName, string Description, List<GlyphPin> ExtraOutputPins) CreateStageDefinition() =>
    (
        NodeTypeId,
        "1. Attempted",
        "First stage in the interaction pipeline. Fires when a character attempts to start " +
        "an interaction, before precondition checks. Route to Fail Interaction to block it.",
        [
            new GlyphPin { Id = "target_mode", Name = "Target Mode", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "proficiency", Name = "Proficiency", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
        ]
    );
}
