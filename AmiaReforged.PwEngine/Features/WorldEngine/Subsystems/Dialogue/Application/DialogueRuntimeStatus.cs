using System.Collections.Concurrent;
using AmiaReforged.Shared.Dialogue;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;

[ServiceBinding(typeof(DialogueRuntimeStatus))]
public sealed class DialogueRuntimeStatus
{
    private readonly ConcurrentDictionary<string, DialogueRuntimeStatusDto> _status = new();

    // PostgreSQL stores microseconds, while .NET timestamps can include finer ticks.
    private static DateTime Revision(DateTime value) => new(value.Ticks - value.Ticks % 10, value.Kind);

    public void Applied(string treeId, DateTime revision, int matchedNpcs) => Set(treeId, new()
    {
        State = "Applied", SavedRevisionUtc = Revision(revision), AppliedRevisionUtc = Revision(revision), MatchedNpcCount = matchedNpcs
    });

    public void Failed(string treeId, DateTime revision, string error) => Set(treeId, new()
    {
        State = "Failed", SavedRevisionUtc = Revision(revision), Error = error
    });

    public void Remove(string treeId, DateTime? throughRevision = null)
    {
        if (!_status.TryGetValue(treeId, out DialogueRuntimeStatusDto? existing)) return;
        if (throughRevision is null || existing.SavedRevisionUtc <= Revision(throughRevision.Value))
            ((ICollection<KeyValuePair<string, DialogueRuntimeStatusDto>>)_status).Remove(new(treeId, existing));
    }

    private void Set(string treeId, DialogueRuntimeStatusDto status) => _status.AddOrUpdate(treeId, status, (_, existing) =>
        existing.SavedRevisionUtc > status.SavedRevisionUtc ? existing : status.State == "Failed" ? status with { AppliedRevisionUtc = existing.AppliedRevisionUtc } : status);

    public void UpdateNpcCount(string treeId, int count)
    {
        if (_status.TryGetValue(treeId, out DialogueRuntimeStatusDto? status))
            _status.TryUpdate(treeId, status with { MatchedNpcCount = count }, status);
    }

    public DialogueRuntimeStatusDto Get(string treeId, DateTime savedRevision)
    {
        savedRevision = Revision(savedRevision);
        _status.TryGetValue(treeId, out DialogueRuntimeStatusDto? status);
        return status?.SavedRevisionUtc == savedRevision ? status
            : new() { State = "Pending", SavedRevisionUtc = savedRevision, AppliedRevisionUtc = status?.AppliedRevisionUtc };
    }
}
