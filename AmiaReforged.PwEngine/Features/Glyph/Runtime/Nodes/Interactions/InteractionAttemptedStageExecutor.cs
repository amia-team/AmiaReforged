using AmiaReforged.PwEngine.Features.Glyph.Core;

using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pipeline stage node for the "Attempted" phase of an interaction pipeline.
/// First stage in the pipeline: Attempted → Started → Tick → Completed.
/// Fires before precondition checks. Downstream nodes can inspect context and
/// route to <c>interaction.fail</c> to block the interaction from starting.
/// </summary>
[GlyphNode]
public partial class InteractionAttemptedStageExecutor : InteractionStageExecutorBase
{
    public static GlyphEventDescriptor Event { get; } = new("interaction", GlyphEventType.InteractionPipeline,
        GlyphScriptCategory.Interaction, Stages: [
            new("attempted", NodeTypeId),
            new("started", InteractionStartedStageExecutor.NodeTypeId),
            new("tick", InteractionTickStageExecutor.NodeTypeId),
            new("completed", InteractionCompletedStageExecutor.NodeTypeId)
        ], Capabilities: [typeof(InteractionGlyphContext), typeof(GlyphCharacterContext)]);

    public const string NodeTypeId = "stage.interaction_attempted";

    public override string TypeId => NodeTypeId;

    public override string SourceDisplayName => "Attempted";

    protected override void AddStageContextPins(List<ContextPinDescriptor> pins)
    {
        pins.Add(new("target_mode", "Target Mode", GlyphDataType.String,
            ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionTargetMode ?? string.Empty : string.Empty));
        pins.Add(new("proficiency", "Proficiency", GlyphDataType.String,
            ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionProficiency ?? string.Empty : string.Empty));
    }

    protected override (string TypeId, string DisplayName, string Description) CreateStageDefinition() =>
    (
        NodeTypeId,
        "1. Attempted",
        "First stage in the interaction pipeline. Fires when a character attempts to start " +
        "an interaction, before precondition checks. Route to Fail Interaction to block it."
    );
}
