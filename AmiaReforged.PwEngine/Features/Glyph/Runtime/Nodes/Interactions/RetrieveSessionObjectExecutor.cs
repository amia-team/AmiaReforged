using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pure function node that retrieves an NwObject (uint object ID) previously stored in the
/// interaction session's metadata by <see cref="StoreSessionObjectExecutor"/>. Returns the
/// object ID and a boolean indicating whether the key was found.
///
/// Falls back to the context's <see cref="GlyphExecutionContext.InteractionMetadata"/> when
/// no session is available (e.g., the Attempted stage).
/// </summary>
public class RetrieveSessionObjectExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.retrieve_session_object";

    public string TypeId => NodeTypeId;

    /// <summary>OBJECT_INVALID in NWScript (0x7F000000).</summary>
    private const uint ObjectInvalid = 0x7F000000;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? keyValue = await resolveInput("key");
        string key = keyValue?.ToString() ?? string.Empty;

        bool exists = false;
        uint objectId = ObjectInvalid;

        // Try session metadata first, fall back to context metadata
        Dictionary<string, object>? metadata = context.Session?.Metadata ?? context.InteractionMetadata;
        if (metadata != null && !string.IsNullOrEmpty(key) && metadata.TryGetValue(key, out object? raw))
        {
            uint coerced = StoreSessionObjectExecutor.CoerceToUint(raw);
            if (coerced != ObjectInvalid)
            {
                exists = true;
                objectId = coerced;
            }
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["object"] = objectId,
            ["exists"] = exists
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Retrieve Session Object",
        Category = "Interactions",
        Description = "Retrieves an NwObject (object ID) previously stored in the interaction session " +
                      "with the Store Session Object node. Returns OBJECT_INVALID if the key is not found.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "key", Name = "Key", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "object", Name = "Object", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "exists", Name = "Exists", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output }
        ]
    };
}
