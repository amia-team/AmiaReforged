namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;

public sealed record GlyphTrace(Guid DefinitionId, Guid VersionId, DateTime RecordedAt, string? Stage, int Steps, IReadOnlyList<string> Entries);
/// <summary>Bounded process-local inspection history; source locations are retained in trace entries.</summary>
public sealed class GlyphTraceStore
{
    private readonly object _gate = new();
    private readonly Queue<GlyphTrace> _traces = new();
    public void Record(GlyphExecutionContext context)
    {
        if (!context.EnableTracing || context.Graph.DefinitionId == Guid.Empty) return;
        var trace = new GlyphTrace(context.Graph.DefinitionId, context.Graph.VersionId, DateTime.UtcNow,
            context.CurrentPipelineStage, context.ExecutionStepCount, Array.AsReadOnly(context.TraceLog.Take(2000).ToArray()));
        lock (_gate)
        {
            _traces.Enqueue(trace);
            while (_traces.Count > 64) _traces.Dequeue();
        }
    }
    public IReadOnlyList<GlyphTrace> Get(Guid definitionId)
    { lock (_gate) return _traces.Where(t => t.DefinitionId == definitionId).ToArray(); }
}
