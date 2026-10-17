namespace AmiaReforged.Core.Services.Sailing;

/// <summary>One writer per identity, newest waiting snapshot replaces superseded snapshots.</summary>
public sealed class CoalescingSaveQueue<TKey, T> where TKey : notnull where T : class
{
    private sealed class Work(T latest)
    {
        public T? Latest { get; set; } = latest;
        public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    private readonly object _gate = new();
    private readonly Dictionary<TKey, Work> _pending = new();
    public Task<bool> Enqueue(TKey identity, T snapshot, Func<T, Task<bool>> write)
    {
        Work work;
        lock (_gate)
        {
            if (_pending.TryGetValue(identity, out Work? existing))
            { existing.Latest = snapshot; return existing.Completion.Task; }
            _pending[identity] = work = new(snapshot);
        }
        _ = Drain(identity, work, write);
        return work.Completion.Task;
    }
    private async Task Drain(TKey identity, Work work, Func<T, Task<bool>> write)
    {
        bool succeeded = true;
        while (true)
        {
            T snapshot;
            lock (_gate) { snapshot = work.Latest!; work.Latest = null; }
            try { succeeded &= await write(snapshot).ConfigureAwait(false); } catch { succeeded = false; }
            lock (_gate)
            {
                if (work.Latest != null) continue;
                _pending.Remove(identity);
                work.Completion.SetResult(succeeded);
                return;
            }
        }
    }

    public async Task<bool> FlushAsync(CancellationToken cancellation = default)
    {
        bool succeeded = true;
        while (true)
        {
            Task<bool>[] pending;
            lock (_gate) pending = _pending.Values.Select(work => work.Completion.Task).ToArray();
            if (pending.Length == 0) return succeeded;
            bool[] results = await Task.WhenAll(pending).WaitAsync(cancellation).ConfigureAwait(false);
            succeeded &= results.All(result => result);
        }
    }
}
