namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Awaits every arrival before publishing membership or helm changes.</summary>
public static class ShipTransferTransaction
{
    public static async Task<bool> ExecuteAsync(
        IReadOnlyList<(Func<Task> Move, Func<Task> Rollback, Action Validate)> transfers, Action commit,
        Action<Exception>? report = null, Func<Task>? prepareCommit = null, Func<Task>? abortCommit = null)
    {
        int attempted = 0;
        try
        {
            foreach (var transfer in transfers)
            {
                attempted++; // A failed move may already have changed native state.
                await transfer.Move();
            }
            // Earlier participants can disconnect or move while later arrivals are pending.
            foreach (var transfer in transfers) transfer.Validate();
            if (prepareCommit != null) await prepareCommit();
            foreach (var transfer in transfers) transfer.Validate();
            commit();
            return true;
        }
        catch (Exception ex)
        {
            if (abortCommit != null)
                try { await abortCommit(); } catch (Exception abort) { report?.Invoke(abort); }
            // Never teleport participants whose move was not attempted.
            for (int index = attempted - 1; index >= 0; index--)
                try { await transfers[index].Rollback(); } catch (Exception rollback) { report?.Invoke(rollback); }
            report?.Invoke(ex);
            return false;
        }
    }

    /// <summary>Queuing an NWN action is not arrival. Always yield at least one engine frame.</summary>
    public static async Task AwaitArrivalAsync(Func<Task> nextFrame, Func<bool> arrived,
        Action validate, int maximumFrames = 300, TimeSpan? timeout = null)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        TimeSpan limit = timeout ?? TimeSpan.FromSeconds(10);
        for (int frame = 0; frame < maximumFrames; frame++)
        {
            await nextFrame();
            validate();
            if (arrived()) return;
            if (clock.Elapsed >= limit) break;
        }
        throw new TimeoutException("Transfer did not reach its destination area after engine processing.");
    }
}

/// <summary>Main-thread reservations cover the entire party, including its rollback.</summary>
public sealed class ShipTransferReservations
{
    private readonly HashSet<string> _players = new(StringComparer.Ordinal);
    public bool Contains(string player) => _players.Contains(player);

    public IDisposable? TryReserve(IReadOnlyList<string> players)
    {
        if (players.Distinct(StringComparer.Ordinal).Count() != players.Count || players.Any(_players.Contains)) return null;
        foreach (string player in players) _players.Add(player);
        return new Reservation(_players, players.ToArray());
    }

    private sealed class Reservation(HashSet<string> players, string[] names) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (string name in names) players.Remove(name);
        }
    }
}
