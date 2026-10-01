using System.Text.Json;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

namespace AmiaReforged.PwEngine.Features.Glyph.Documentation;

public static class GlyphLexicon
{
    private static readonly Lazy<GlyphDocumentationPackDto> Pack = new(() =>
    {
        using var stream = typeof(GlyphLexicon).Assembly.GetManifestResourceStream("Glyph.Lexicon.json")
            ?? throw new InvalidOperationException("The packaged Glyph Lexicon is missing.");
        return JsonSerializer.Deserialize<GlyphDocumentationPackDto>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("The packaged Glyph Lexicon is invalid.");
    });

    // Share one article between aliases and typed members; publish only sources actually registered.
    public static GlyphDocumentationPackDto ForSources(IEnumerable<string?> sources) => Pack.Value with
    {
        Functions = sources.OfType<string>().Distinct(StringComparer.Ordinal)
            .Where(Pack.Value.Functions.ContainsKey)
            .ToDictionary(s => s, s => Pack.Value.Functions[s], StringComparer.Ordinal)
    };
}
