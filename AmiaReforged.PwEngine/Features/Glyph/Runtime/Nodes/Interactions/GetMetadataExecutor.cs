using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pure function node that reads a value from the interaction session's metadata dictionary.
/// Returns the value as a string and whether the key exists.
/// </summary>
[GlyphNode]
public partial class GetMetadataExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.get_metadata";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? keyValue = await resolveInput(Inputs.Key);
        string key = keyValue?.ToString() ?? string.Empty;

        bool exists = false;
        string value = string.Empty;

        // Try session metadata first, fall back to context metadata
        Dictionary<string, object>? metadata = context.Session?.Metadata ?? context.InteractionMetadata;
        if (metadata != null && !string.IsNullOrEmpty(key) && metadata.TryGetValue(key, out object? raw))
        {
            exists = true;
            value = raw?.ToString() ?? string.Empty;
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["value"] = value,
            ["exists"] = exists
        });
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("metadata", "value", Indexer: new("metadata", "metadata", "set_metadata"))
        ],
        DisplayName = "Get Metadata",
        Category = "Interactions",
        Description = "Reads a value from the interaction session's metadata dictionary. " +
                      "Returns the value as a string and whether the key was found.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        ScriptCategory = GlyphScriptCategory.Interaction,
        Parameters =
        [
            Pins.In("key", "Key", GlyphDataType.String)
        ],
        Results =
        [
            Pins.Out("value", "Value", GlyphDataType.String),
            Pins.Out("exists", "Exists", GlyphDataType.Bool)
        ]
    };
}
