using AmiaReforged.PwEngine.Features.Glyph.Core;

using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pipeline stage node for the "Started" phase of an interaction pipeline.
/// Second stage in the pipeline: Attempted → Started → Tick → Completed.
/// Fires after the interaction session has been created. Provides session details
/// and allows setup logic. Route to <c>interaction.fail</c> to cancel the session.
/// </summary>
[GlyphNode]
public partial class InteractionStartedStageExecutor : InteractionStageExecutorBase
{
    public const string NodeTypeId = "stage.interaction_started";

    public override string TypeId => NodeTypeId;

    public override string SourceDisplayName => "Started";

    protected override void AddStageContextPins(List<ContextPinDescriptor> pins)
    {
        pins.Add(new("target_mode", "Target Mode", GlyphDataType.String,
            ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionTargetMode ?? string.Empty : string.Empty));
        pins.Add(new("session_id", "Session ID", GlyphDataType.String,
            ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionSessionId.ToString() : Guid.Empty.ToString()));
        pins.Add(new("required_rounds", "Required Rounds", GlyphDataType.Int,
            ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionRequiredRounds : 0));
        pins.Add(new("proficiency", "Proficiency", GlyphDataType.String,
            ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionProficiency ?? string.Empty : string.Empty));
    }

    protected override (string TypeId, string DisplayName, string Description) CreateStageDefinition() =>
    (
        NodeTypeId,
        "2. Started",
        "Second stage in the interaction pipeline. Fires after the interaction session " +
        "is created. Use for setup logic, VFX, or messages. Route to Fail Interaction to cancel."
    );
}
