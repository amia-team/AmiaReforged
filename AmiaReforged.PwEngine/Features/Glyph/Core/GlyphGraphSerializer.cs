using System.Text.Json;
namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>Developer/test serialization of derived executable IR, never authored program state.</summary>
public static class GlyphGraphSerializer
{
    public static string Serialize(GlyphGraph graph) => JsonSerializer.Serialize(graph, GlyphJsonDefaults.Options);
}
