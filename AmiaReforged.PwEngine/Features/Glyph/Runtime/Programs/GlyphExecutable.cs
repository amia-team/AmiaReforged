using System.Collections.ObjectModel;
using System.Text.Json;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;

/// <summary>
/// Validated executable snapshot. The serialized IR is private, immutable derived data.
/// Every execution receives a private working copy, so even a misbehaving executor cannot
/// mutate another execution or a retained rollback version.
/// </summary>
public sealed class GlyphExecutable
{
    private readonly string _ir;
    public string SourceText { get; }
    public string SourceHash { get; }
    public int LanguageVersion { get; }
    public GlyphEventType EventType { get; }
    public string Name { get; }
    public IReadOnlyDictionary<Guid, SourceSpan> SourceMap { get; }
    internal GlyphExecutable(GlyphGraph ir, string source, string hash, IReadOnlyDictionary<Guid, SourceSpan> map, int languageVersion)
    {
        _ir = JsonSerializer.Serialize(ir, GlyphJsonDefaults.Options);
        SourceText = source; SourceHash = hash; EventType = ir.EventType; Name = ir.Name; LanguageVersion = languageVersion;
        SourceMap = new ReadOnlyDictionary<Guid, SourceSpan>(new Dictionary<Guid, SourceSpan>(map));
    }
    public GlyphGraph CreateExecutionGraph()
    {
        GlyphGraph graph = JsonSerializer.Deserialize<GlyphGraph>(_ir, GlyphJsonDefaults.Options)!;
        graph.SourceMap = SourceMap;
        return graph;
    }
}
