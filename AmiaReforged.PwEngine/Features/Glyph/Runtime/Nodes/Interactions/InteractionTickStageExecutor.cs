using AmiaReforged.PwEngine.Features.Glyph.Core;

using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pipeline stage node for the "Tick" phase of an interaction pipeline.
/// Third stage in the pipeline: Attempted → Started → Tick → Completed.
/// Fires each round/tick of an active interaction. Provides progress info.
/// Route to <c>interaction.fail</c> to cancel the interaction mid-progress.
/// </summary>
[GlyphNode]
public partial class InteractionTickStageExecutor : InteractionStageExecutorBase
{
    public const string NodeTypeId = "stage.interaction_tick";

    public override string TypeId => NodeTypeId;

    public override string SourceDisplayName => "Tick";

    protected override void AddStageContextPins(List<ContextPinDescriptor> pins)
    {
        pins.Add(new("session_id", "Session ID", GlyphDataType.String,
            ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionSessionId.ToString() : Guid.Empty.ToString()));
        pins.Add(new("progress", "Progress", GlyphDataType.Int,
            ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionProgress : 0));
        pins.Add(new("required_rounds", "Required Rounds", GlyphDataType.Int,
            ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionRequiredRounds : 0));
        pins.Add(new("proficiency", "Proficiency", GlyphDataType.String,
            ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionProficiency ?? string.Empty : string.Empty));
    }

    protected override (string TypeId, string DisplayName, string Description) CreateStageDefinition() =>
    (
        NodeTypeId,
        "3. Tick",
        "Third stage in the interaction pipeline. Fires each round of an active interaction. " +
        "Route to Fail Interaction to cancel mid-progress."
    );
}
