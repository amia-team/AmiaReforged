using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> for Glyph graph serialization/deserialization.
/// Used by hook services, the API controller, and tests to ensure consistent behavior.
/// </summary>
public static class GlyphJsonDefaults
{
    /// <summary>
    /// Standard JSON options for Glyph graph (de)serialization.
    /// Case-insensitive property names, enums serialized as strings.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
