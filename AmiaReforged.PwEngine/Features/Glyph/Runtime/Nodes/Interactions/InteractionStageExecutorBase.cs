using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Base class for interaction pipeline stage executors. Encapsulates the shared
/// passthrough-overridable input pattern and common output pins. Concrete stages
/// override <see cref="AddStageOutputs"/> to append stage-specific context values
/// and <see cref="CreateStageDefinition"/> to describe the node's type info and extra pins.
/// <para>
/// Pipeline stage nodes have <b>no exec_in pin</b> — they are independent entry points
/// triggered by the runtime via <see cref="GlyphInterpreter.ExecuteStageAsync"/>.
/// Each stage's "Then" output is entirely user-directed.
/// </para>
/// <para>
/// Also implements <see cref="IContextNodeProvider"/> so that each stage automatically
/// generates wireless context getter nodes for its output pins.
/// </para>
/// </summary>
public abstract class InteractionStageExecutorBase : IGlyphNodeExecutor, IContextNodeProvider
{
    public abstract string TypeId { get; }

    // ── IContextNodeProvider ─────────────────────────────────────────────

    public string SourceTypeId => TypeId;

    public abstract string SourceDisplayName { get; }

    public GlyphEventType? SourceEventType => GlyphEventType.InteractionPipeline;

    public GlyphScriptCategory? SourceScriptCategory => GlyphScriptCategory.Interaction;

    public List<ContextPinDescriptor> GetContextPins()
    {
        // Common pins shared by all interaction stages
        List<ContextPinDescriptor> pins =
        [
            new("character_id", "Character ID", GlyphDataType.String,
                ctx => ctx.CharacterId ?? string.Empty),
            new("creature", "Creature", GlyphDataType.NwObject,
                ctx => ctx.InteractionCreature),
            new("interaction_tag", "Interaction Tag", GlyphDataType.String,
                ctx => ctx.InteractionTag ?? string.Empty),
            new("target_id", "Target ID", GlyphDataType.String,
                ctx => ctx.InteractionTargetId.ToString()),
            new("area_resref", "Area ResRef", GlyphDataType.String,
                ctx => ctx.InteractionAreaResRef ?? string.Empty),
        ];

        // Let subclasses append stage-specific context pins
        AddStageContextPins(pins);

        return pins;
    }

    /// <summary>
    /// Override to add stage-specific context pin descriptors beyond the shared set.
    /// </summary>
    protected virtual void AddStageContextPins(List<ContextPinDescriptor> pins) { }

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
        };

        AddStageOutputs(outputs, context);

        return new GlyphNodeResult
        {
            NextExecPinId = "exec_out",
            OutputValues = outputs
        };
    }

    /// <summary>
    /// Override to add stage-specific output values (e.g. session_id, progress, response_tag).
    /// </summary>
    protected abstract void AddStageOutputs(Dictionary<string, object?> outputs, GlyphExecutionContext context);

    public GlyphNodeDefinition CreateDefinition()
    {
        (string typeId, string displayName, string description, List<GlyphPin> extraOutputPins) = CreateStageDefinition();

        // Common data input pins (passthrough-overridable identity).
        // No exec_in — stages are entry points triggered independently by the runtime.
        List<GlyphPin> inputPins =
        [
            new GlyphPin { Id = "character_id", Name = "Character ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "interaction_tag", Name = "Interaction Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "target_id", Name = "Target ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
        ];

        // Common output pins shared by every stage
        List<GlyphPin> outputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "character_id", Name = "Character ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "interaction_tag", Name = "Interaction Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "target_id", Name = "Target ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "area_resref", Name = "Area ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
        ];

        outputPins.AddRange(extraOutputPins);

        return new GlyphNodeDefinition
        {
            TypeId = typeId,
            DisplayName = displayName,
            Category = "Pipeline Stages",
            Description = description,
            ColorClass = "node-stage",
            Archetype = GlyphNodeArchetype.PipelineStage,
            IsSingleton = true,
            RestrictToEventType = GlyphEventType.InteractionPipeline,
            ScriptCategory = GlyphScriptCategory.Interaction,
            InputPins = inputPins,
            OutputPins = outputPins,
        };
    }

    /// <summary>
    /// Override to provide the stage's TypeId, display name, description, and any extra output pins
    /// beyond the shared set (exec_out, character_id, creature, interaction_tag, target_id, area_resref).
    /// </summary>
    protected abstract (string TypeId, string DisplayName, string Description, List<GlyphPin> ExtraOutputPins) CreateStageDefinition();
}
