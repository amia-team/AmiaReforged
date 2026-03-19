using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Context;

/// <summary>
/// A parameterised "wireless" getter node that reads a single value directly from the
/// <see cref="GlyphExecutionContext"/> without any input pins or wires.
/// <para>
/// One instance is created per <see cref="ContextPinDescriptor"/> returned by an
/// <see cref="IContextNodeProvider"/> during bootstrap. The node's <see cref="TypeId"/>
/// is derived from the source node and pin (e.g. <c>"context.stage.interaction_attempted.character_id"</c>).
/// </para>
/// <para>
/// Uses the <see cref="GlyphNodeArchetype.ContextGetter"/> archetype — lazily evaluated and
/// cached like a <see cref="GlyphNodeArchetype.PureFunction"/>, but semantically distinct
/// to allow future interpreter specialisation.
/// </para>
/// </summary>
public class ContextGetterExecutor : IGlyphNodeExecutor
{
    private readonly string _sourceTypeId;
    private readonly string _sourceDisplayName;
    private readonly ContextPinDescriptor _pin;
    private readonly GlyphEventType? _restrictToEventType;
    private readonly GlyphScriptCategory? _scriptCategory;

    public ContextGetterExecutor(
        string sourceTypeId,
        string sourceDisplayName,
        ContextPinDescriptor pin,
        GlyphEventType? restrictToEventType,
        GlyphScriptCategory? scriptCategory)
    {
        _sourceTypeId = sourceTypeId;
        _sourceDisplayName = sourceDisplayName;
        _pin = pin;
        _restrictToEventType = restrictToEventType;
        _scriptCategory = scriptCategory;
    }

    public string TypeId => $"context.{_sourceTypeId}.{_pin.PinId}";

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? value = _pin.Accessor(context);

        return Task.FromResult(new GlyphNodeResult
        {
            OutputValues = new Dictionary<string, object?>
            {
                ["value"] = value,
            },
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId,
        DisplayName = $"{_sourceDisplayName}: {_pin.DisplayName}",
        Category = $"Context: {_sourceDisplayName}",
        Description = $"Reads the {_pin.DisplayName} value from the {_sourceDisplayName} context. " +
                      "No wires needed — this node reads directly from the execution context.",
        ColorClass = "node-context",
        Archetype = GlyphNodeArchetype.ContextGetter,
        ContextSourceTypeId = _sourceTypeId,
        RestrictToEventType = _restrictToEventType,
        ScriptCategory = _scriptCategory,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin
            {
                Id = "value",
                Name = _pin.DisplayName,
                DataType = _pin.DataType,
                Direction = GlyphPinDirection.Output,
            },
        ],
    };
}
