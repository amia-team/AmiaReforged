using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Base class for interaction pipeline stage executors. Encapsulates the shared
/// passthrough-overridable input pattern and common output pins. Concrete stages
/// declare stage-specific context values once
/// and <see cref="CreateStageDefinition"/> to describe the node.
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

    private GlyphContextSchema? _schema;
    public GlyphContextSchema Schema => _schema ??= new(CreateContextPins());
    public List<ContextPinDescriptor> GetContextPins() => Schema.Fields.ToList();

    private List<ContextPinDescriptor> CreateContextPins()
    {
        // Common pins shared by all interaction stages
        List<ContextPinDescriptor> pins =
        [
            new("character_id", "Character ID", GlyphDataType.String,
                ctx => ctx.Get<GlyphCharacterContext>() is { } data ? data.CharacterId ?? string.Empty : string.Empty, AllowInputOverride: true),
            new("creature", "Creature", GlyphDataType.NwObject,
                ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionCreature : 0u, Aliases: ["player"], AllowInputOverride: true),
            new("interaction_tag", "Interaction Tag", GlyphDataType.String,
                ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionTag ?? string.Empty : string.Empty, AllowInputOverride: true),
            new("target_id", "Target ID", GlyphDataType.String,
                ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionTargetId.ToString() : Guid.Empty.ToString(), AllowInputOverride: true),
            new("area_resref", "Area ResRef", GlyphDataType.String,
                ctx => ctx.Get<InteractionGlyphContext>() is { } data ? data.InteractionAreaResRef ?? string.Empty : string.Empty),
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
        Dictionary<string, object?> outputs = Schema.Read(context);
        foreach (var field in Schema.Fields.Where(f => f.AllowInputOverride))
        {
            object? value = await resolveInput(field.PinId);
            if (value != null) outputs[field.PinId] = field.DataType == GlyphDataType.String ? value.ToString() : value;
        }

        return new GlyphNodeResult
        {
            NextExecPinId = "exec_out",
            OutputValues = outputs
        };
    }

    public GlyphNodeDefinition CreateDefinition()
    {
        (string typeId, string displayName, string description) = CreateStageDefinition();

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
            InputPins = Schema.Fields.Where(f => f.AllowInputOverride)
                .Select(f => Pins.In(f.PinId, f.DisplayName, f.DataType)).ToList(),
            OutputPins = [Pins.ExecOut("exec_out", "Then"), .. Schema.CreateOutputPins()],
            ContextSchema = Schema,
        };
    }

    /// <summary>
    /// Override to provide the stage's TypeId, display name, description, metadata. Output pins are derived from the context schema.
    /// </summary>
    protected abstract (string TypeId, string DisplayName, string Description) CreateStageDefinition();
}
