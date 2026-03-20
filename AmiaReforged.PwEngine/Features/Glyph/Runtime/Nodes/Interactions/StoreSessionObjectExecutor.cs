using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Action node that stores an NwObject (uint object ID) in the interaction session's metadata
/// dictionary under a string key. The value persists across pipeline stages (Started → Tick → Completed)
/// so that later stages can retrieve the same NwObject via <see cref="RetrieveSessionObjectExecutor"/>.
///
/// In the Attempted stage (where no session exists yet), the value is written to the context's
/// local <see cref="GlyphExecutionContext.InteractionMetadata"/> dictionary instead.
/// </summary>
public class StoreSessionObjectExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.store_session_object";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? keyValue = await resolveInput("key");
        object? objectValue = await resolveInput("object");

        string key = keyValue?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(key))
        {
            return GlyphNodeResult.Continue("exec_out");
        }

        // Convert value to uint (NwObject ID). Accept uint, int, long, or string representations.
        uint objectId = CoerceToUint(objectValue);

        // Prefer writing to the live session so data persists across stages.
        // In the Attempted stage the session doesn't exist yet, so fall back to
        // the context's metadata dictionary (still visible within this execution).
        if (context.Session != null)
        {
            context.Session.Metadata ??= new Dictionary<string, object>();
            context.Session.Metadata[key] = objectId;
        }
        else
        {
            context.InteractionMetadata ??= new Dictionary<string, object>();
            context.InteractionMetadata[key] = objectId;
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    /// <summary>
    /// Coerces a boxed value to a <c>uint</c> NwObject ID.  Handles the common
    /// runtime representations: <c>uint</c>, <c>int</c>, <c>long</c>, and parseable strings.
    /// Returns 0x7F000000 (NWScript.OBJECT_INVALID) when the value cannot be converted.
    /// </summary>
    internal static uint CoerceToUint(object? value)
    {
        const uint objectInvalid = 0x7F000000;

        return value switch
        {
            uint u => u,
            int i => (uint)i,
            long l => (uint)l,
            string s when uint.TryParse(s, out uint parsed) => parsed,
            _ => objectInvalid
        };
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Store Session Object",
        Category = "Interactions",
        Description = "Stores an NwObject (object ID) in the interaction session under a string key. " +
                      "The stored object persists across pipeline stages and can be retrieved with " +
                      "the Retrieve Session Object node.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "key", Name = "Key", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "object", Name = "Object", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
