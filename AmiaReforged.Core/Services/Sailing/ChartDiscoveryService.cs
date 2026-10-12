using System.Security.Cryptography;
using System.Text;
using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ChartDiscoveryService))]
public sealed class ChartDiscoveryService : IDisposable
{
    private const int GridSize = 16;
    private readonly Dictionary<string, Dictionary<string, bool[,]>> _discovery = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _revisions = new(StringComparer.Ordinal);
    private readonly HashSet<string> _loaded = new(StringComparer.Ordinal);
    private readonly HashSet<string> _loading = new(StringComparer.Ordinal);
    private readonly ChartDiscoveryPersistenceService? _persistence;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public ChartDiscoveryService(ChartDiscoveryPersistenceService? persistence = null) => _persistence = persistence;
    // Account-wide charts match the available player-oriented design. Never store the raw CD key.
    private static string Account(NwPlayer player) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(player.CDKey)));
    public long GetRevision(NwPlayer player) => _revisions.GetValueOrDefault(Account(player));
    public bool IsDiscovered(NwPlayer player, string area, int x, int y) => IsDiscovered(Account(player), area, x, y);
    public bool IsDiscovered(string account, string area, int x, int y) =>
        x >= 0 && y >= 0 && x < GridSize && y < GridSize && _discovery.TryGetValue(account, out var areas) &&
        areas.TryGetValue(area, out var grid) && grid[x, y];

    public void RevealAroundShip(NwPlayer player, ShipState ship)
    {
        string account = Account(player);
        if (_persistence?.IsEnabled == true && !_loaded.Contains(account) && _loading.Add(account))
            _ = RestoreAccount(account);
        if (_persistence?.IsEnabled != true) _loaded.Add(account);
        if (RevealAroundShip(account, ship) && _loaded.Contains(account)) _ = SaveAccount(account);
    }
    public bool RevealAroundShip(string account, ShipState ship)
    {
        if (!ShipActionRules.IsActive(ship)) return false;
        bool[,] grid = Grid(account, ship.AreaResRef);
        int centerX = Math.Clamp((int)(ship.X / 40f), 0, 15);
        int centerY = 15 - Math.Clamp((int)(ship.Y / 40f), 0, 15);
        bool changed = false;
        for (int x = Math.Max(centerX - 2, 0); x <= Math.Min(centerX + 2, 15); x++)
        for (int y = Math.Max(centerY - 2, 0); y <= Math.Min(centerY + 2, 15); y++)
            if (!grid[x, y]) { grid[x, y] = true; changed = true; }
        if (changed) _revisions[account] = _revisions.GetValueOrDefault(account) + 1;
        return changed;
    }
    private async Task RestoreAccount(string account)
    {
        try
        {
            IReadOnlyCollection<SavedChartDiscovery> saved = await _persistence!.Load(account);
            await NwTask.SwitchToMainThread();
            Restore(account, saved); // Merge exploration made while the load was pending.
            _loaded.Add(account);
            await SaveAccount(account);
        }
        catch (Exception ex) { Log.Error(ex, "Chart discovery restoration failed; existing discovery will not be overwritten."); }
        finally
        {
            await NwTask.SwitchToMainThread();
            _loading.Remove(account);
        }
    }
    public void Restore(string account, IEnumerable<SavedChartDiscovery> saved)
    {
        bool changed = false;
        foreach (SavedChartDiscovery record in saved)
        {
            if (record.AccountKey != account || !record.IsValid()) continue;
            bool[,] grid = Grid(account, record.AreaResRef);
            for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
                if (record.Cells[x * 16 + y] == '1' && !grid[x, y]) { grid[x, y] = true; changed = true; }
        }
        if (changed) _revisions[account] = _revisions.GetValueOrDefault(account) + 1;
    }
    public IReadOnlyCollection<SavedChartDiscovery> Capture(string account)
    {
        if (!_discovery.TryGetValue(account, out var areas)) return [];
        return areas.Select(area =>
        {
            char[] cells = new char[256];
            for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++) cells[x * 16 + y] = area.Value[x, y] ? '1' : '0';
            return new SavedChartDiscovery { AccountKey = account, AreaResRef = area.Key, Cells = new string(cells) };
        }).ToArray();
    }
    public async Task<bool> SaveAccount(string account)
    {
        if (_persistence?.IsEnabled != true) return false;
        SavedChartDiscovery[] snapshots = Capture(account).ToArray();
        bool[] saved = await Task.WhenAll(snapshots.Select(_persistence.Save));
        return saved.All(success => success);
    }
    private bool[,] Grid(string account, string area)
    {
        if (!_discovery.TryGetValue(account, out var areas)) _discovery[account] = areas = new(StringComparer.OrdinalIgnoreCase);
        if (!areas.TryGetValue(area, out var grid)) areas[area] = grid = new bool[16, 16];
        return grid;
    }

    public void Dispose()
    {
        if (_persistence?.IsEnabled != true) return;
        try
        {
            // Only loaded accounts are safe to publish. Capture contains no
            // player/native handles, so all shutdown continuations are pure I/O.
            Task<bool>[] pending = _loaded.SelectMany(Capture).Select(_persistence.Save).ToArray();
            Task<bool[]> flush = Task.WhenAll(pending);
            if (!flush.Wait(TimeSpan.FromSeconds(10))) Log.Error("Timed out writing final sailing chart discovery.");
            else if (flush.Result.Any(result => !result)) Log.Error("Some final sailing chart saves failed.");
        }
        catch (Exception ex) { Log.Error(ex, "Sailing chart shutdown checkpoint failed."); }
    }
}
