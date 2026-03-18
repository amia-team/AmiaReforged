using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Universal failure action node for interaction pipelines. Replaces the stage-specific
/// <c>interaction.block</c> and <c>interaction.cancel</c> nodes. Reads
/// <see cref="GlyphExecutionContext.CurrentPipelineStage"/> to determine failure behavior:
/// <list type="bullet">
///   <item><c>stage.interaction_attempted</c> → blocks the interaction from starting</item>
///   <item><c>stage.interaction_started</c> → cancels the session immediately</item>
///   <item><c>stage.interaction_tick</c> → cancels the session mid-progress</item>
///   <item><c>stage.interaction_completed</c> → cancels the session at completion</item>
/// </list>
/// Terminates the exec chain after setting the failure flags (returns <see cref="GlyphNodeResult.Done"/>).
/// </summary>
public class FailInteractionExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.fail";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? messageValue = await resolveInput("message");
        string message = messageValue?.ToString() ?? "Interaction failed";

        string stage = context.CurrentPipelineStage ?? string.Empty;

        if (stage == InteractionAttemptedStageExecutor.NodeTypeId)
        {
            // Attempted stage: block the interaction from starting
            context.ShouldBlockInteraction = true;
            context.BlockInteractionMessage = message;
        }
        else
        {
            // Started / Tick / Completed stages: cancel the session
            context.ShouldCancelInteraction = true;
            context.CancelInteractionMessage = message;
        }

        // Terminate the exec chain — failure is a dead end
        return GlyphNodeResult.Done();
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Fail Interaction",
        Category = "Interactions",
        Description = "Fails the interaction at the current pipeline stage. During Attempted, blocks the " +
                      "interaction from starting. During Started/Tick/Completed, cancels the session. " +
                      "Terminates the execution chain.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        RestrictToEventType = GlyphEventType.InteractionPipeline,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "message", Name = "Message", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "Interaction failed" }
        ],
        OutputPins = []
    };
}
