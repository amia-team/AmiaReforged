using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Action node that writes a key-value pair to the interaction session's metadata dictionary.
/// Metadata persists for the life of the session and can be read by other nodes/handlers.
/// Operates on the live <see cref="GlyphExecutionContext.Session"/>.
/// </summary>
public class SetMetadataExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.set_metadata";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? keyValue = await resolveInput("key");
        object? valueValue = await resolveInput("value");
        string key = keyValue?.ToString() ?? string.Empty;
        string value = valueValue?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(key))
        {
            return GlyphNodeResult.Continue("exec_out");
        }

        // Prefer writing to the live session so data persists across stages.
        // In the Attempted stage the session doesn't exist yet, so fall back to
        // the context's metadata dictionary (still visible within this execution).
        if (context.Session != null)
        {
            context.Session.Metadata ??= new Dictionary<string, object>();
            context.Session.Metadata[key] = value;
        }
        else
        {
            context.InteractionMetadata ??= new Dictionary<string, object>();
            context.InteractionMetadata[key] = value;
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Set Metadata",
        Category = "Interactions",
        Description = "Writes a key-value pair to the interaction session's metadata. " +
                      "Metadata persists for the session's lifetime and can be read by " +
                      "the Get Metadata node or other interaction handlers.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "key", Name = "Key", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "value", Name = "Value", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
