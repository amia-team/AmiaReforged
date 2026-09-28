using System.Collections.Concurrent;
namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;

public sealed record GlyphProgramVersion(Guid VersionId, Guid DefinitionId, DateTime ActivatedAt,
    Guid? PreviousVersionId, GlyphExecutable Executable)
{
    public Core.GlyphGraph CreateExecutionGraph()
    {
        var graph = Executable.CreateExecutionGraph();
        graph.DefinitionId = DefinitionId; graph.VersionId = VersionId;
        return graph;
    }
}

/// <summary>
/// One authoritative hot-swap boundary. Writers serialize per definition; readers capture
/// an immutable state reference and never wait for persistence or compilation.
/// </summary>
public sealed class GlyphRuntimeRegistry
{
    private sealed record State(GlyphProgramVersion? Active, IReadOnlyList<GlyphProgramVersion> History);
    private sealed class Slot
    {
        public readonly SemaphoreSlim Writer = new(1, 1);
        public State State = new(null, Array.Empty<GlyphProgramVersion>());
    }
    private readonly ConcurrentDictionary<Guid, Slot> _slots = new();
    public GlyphProgramVersion? GetActive(Guid id) => _slots.TryGetValue(id, out var slot) ? Volatile.Read(ref slot.State).Active : null;
    public IReadOnlyList<GlyphProgramVersion> GetVersions(Guid id) => _slots.TryGetValue(id, out var slot) ? Volatile.Read(ref slot.State).History : [];
    public async Task<GlyphProgramVersion> ActivateAsync(Guid id, GlyphExecutable candidate,
        Func<IReadOnlyList<GlyphProgramVersion>, Task>? persist = null)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        Slot slot = _slots.GetOrAdd(id, _ => new());
        await slot.Writer.WaitAsync();
        try { return await Replace(slot, id, candidate, persist); }
        finally { slot.Writer.Release(); }
    }
    private static async Task<GlyphProgramVersion> Replace(Slot slot, Guid id, GlyphExecutable candidate,
        Func<IReadOnlyList<GlyphProgramVersion>, Task>? persist)
    {
        State before = slot.State;
        if ((before.Active ?? before.History.LastOrDefault()) is { } active && active.Executable.EventType != candidate.EventType)
            throw new InvalidOperationException("An activated definition cannot change event type; create a new definition.");
        GlyphProgramVersion version = new(Guid.NewGuid(), id, DateTime.UtcNow, (before.Active ?? before.History.LastOrDefault())?.VersionId, candidate);
        IReadOnlyList<GlyphProgramVersion> history = Array.AsReadOnly(before.History.Append(version).ToArray());
        if (persist != null) await persist(history);
        Volatile.Write(ref slot.State, new(version, history));
        return version;
    }
    public async Task<GlyphProgramVersion?> RollbackAsync(Guid id, Func<IReadOnlyList<GlyphProgramVersion>, Task>? persist = null)
    {
        if (!_slots.TryGetValue(id, out var slot)) return null;
        await slot.Writer.WaitAsync();
        try
        {
            State before = slot.State;
            GlyphProgramVersion? previous = before.History.FirstOrDefault(v => v.VersionId == before.Active?.PreviousVersionId);
            return previous == null ? null : await Replace(slot, id, previous.Executable, persist);
        }
        finally { slot.Writer.Release(); }
    }
    public void RestoreIfAbsent(Guid id, IReadOnlyList<GlyphProgramVersion> versions, bool active = true)
    {
        if (versions.Count == 0) return;
        // Startup/cache refresh may race activation. Never overwrite a published slot.
        _slots.TryAdd(id, new Slot { State = new(active ? versions[^1] : null, Array.AsReadOnly(versions.ToArray())) });
    }
    public async Task DeactivateAsync(Guid id)
    {
        Slot slot = _slots.GetOrAdd(id, _ => new());
        await slot.Writer.WaitAsync();
        try { Volatile.Write(ref slot.State, new(null, slot.State.History)); }
        finally { slot.Writer.Release(); }
    }
}
