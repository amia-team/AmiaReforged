using AmiaReforged.PwEngine.Features.Glyph.Core;

using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pipeline stage node for the "Completed" phase of an interaction pipeline.
/// Final stage in the pipeline: Attempted → Started → Tick → Completed.
/// Fires when all required rounds finish, before the data-driven response system.
/// Route to <c>interaction.fail</c> to cancel the session at completion time.
/// </summary>
[GlyphNode]
public partial class InteractionCompletedStageExecutor : InteractionStageExecutorBase
{
    public const string NodeTypeId = "stage.interaction_completed";

    public override string TypeId => NodeTypeId;

    public override string SourceDisplayName => "Completed";

    protected override void AddStageContextPins(List<ContextPinDescriptor> pins)
    {
        pins.Add(new("session_id", "Session ID", GlyphDataType.String,
            ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionSessionId.ToString() : Guid.Empty.ToString()));
        pins.Add(new("proficiency", "Proficiency", GlyphDataType.String,
            ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionProficiency ?? string.Empty : string.Empty));
        pins.Add(new("response_tag", "Response Tag", GlyphDataType.String,
            ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionResponseTag ?? string.Empty : string.Empty));
    }

    protected override (string TypeId, string DisplayName, string Description) CreateStageDefinition() =>
    (
        NodeTypeId,
        "4. Completed",
        "Final stage in the interaction pipeline. Fires when all rounds finish, " +
        "before the data-driven response system. Route to Fail Interaction to cancel."
    );
}
