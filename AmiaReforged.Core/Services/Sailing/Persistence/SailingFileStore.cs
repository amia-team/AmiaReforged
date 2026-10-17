using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Sailing-owned durable files. No database/schema changes or native calls occur during I/O.</summary>
[ServiceBinding(typeof(SailingFileStore))]
public sealed class SailingFileStore
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private string? _rootDirectory;
    public string RootDirectory => _rootDirectory ??
        throw new InvalidOperationException("Sailing file storage is not initialized until the module is loaded.");

    public SailingFileStore(ShipPersistenceOptions options)
    {
        // Explicit/test paths and non-file backends require no native server.
        if (options.DataDirectory != null || options.Backend != SailingPersistenceBackend.Files)
            _rootDirectory = Path.GetFullPath(options.DataDirectory ?? Path.Combine(AppContext.BaseDirectory, "sailing"));
    }

    internal void InitializeModule()
    {
        // Resolve once on the module-load main thread, never on a file worker.
        _rootDirectory ??= Path.GetFullPath(Path.Combine(NwServer.Instance.UserDirectory, "sailing"));
    }

    private static string Key(string identity) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    private string ShipPath(string identity) => Path.Combine(RootDirectory, "ships", Key(identity) + ".json");
    private string ChartDirectory(string account) => Path.Combine(RootDirectory, "charts", Key(account));

    public async Task<SavedShipState?> LoadShip(string identity)
    {
        SavedShipState? saved = await Read<SavedShipState>(ShipPath(identity)).ConfigureAwait(false);
        if (saved != null && saved.ShipId != identity)
            throw new InvalidDataException("Ship file identity does not match its storage key.");
        return saved;
    }

    public Task SaveShip(SavedShipState snapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshot.ShipId);
        return Write(ShipPath(snapshot.ShipId), snapshot);
    }

    public async Task<IReadOnlyCollection<SavedShipState>> LoadShips()
    {
        string directory = Path.Combine(RootDirectory, "ships");
        string[] paths;
        try { paths = Directory.GetFiles(directory, "*.json"); }
        catch (DirectoryNotFoundException) { return []; }
        List<SavedShipState> saved = [];
        foreach (string path in paths)
        {
            try
            {
                SavedShipState? ship = await Read<SavedShipState>(path).ConfigureAwait(false);
                if (ship == null || string.IsNullOrWhiteSpace(ship.ShipId) || ShipPath(ship.ShipId) != path)
                    throw new InvalidDataException("Invalid saved ship identity.");
                saved.Add(ship);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException)
            {
                // Known fleet ships are subsequently loaded by identity and remain
                // gated if corrupt. One unknown corrupt contact must not block them all.
                Log.Error(ex, "Invalid sailing ship file {0}; file was left untouched.", Path.GetFileName(path));
            }
        }
        return saved;
    }

    public Task<MaritimeEconomyState?> LoadEconomy() => Read<MaritimeEconomyState>(Path.Combine(RootDirectory, "economy", "maritime-v1.json"));
    public Task SaveEconomy(MaritimeEconomyState state) => Write(Path.Combine(RootDirectory, "economy", "maritime-v1.json"), state);

    public Task SaveChart(SavedChartDiscovery snapshot) => Write(
        Path.Combine(ChartDirectory(snapshot.AccountKey), Key(snapshot.AreaResRef.ToLowerInvariant()) + ".json"), snapshot);

    public async Task<IReadOnlyCollection<SavedChartDiscovery>> LoadCharts(string account)
    {
        string directory = ChartDirectory(account);
        string[] paths;
        try { paths = Directory.GetFiles(directory, "*.json"); }
        catch (DirectoryNotFoundException) { return []; }
        List<SavedChartDiscovery> saved = [];
        foreach (string path in paths)
        {
            SavedChartDiscovery? chart = await Read<SavedChartDiscovery>(path).ConfigureAwait(false);
            if (chart == null || chart.AccountKey != account || !chart.IsValid() ||
                path != Path.Combine(directory, Key(chart.AreaResRef.ToLowerInvariant()) + ".json"))
                throw new InvalidDataException("Invalid chart discovery file; existing discovery will not be overwritten.");
            saved.Add(chart);
        }
        return saved;
    }

    private static async Task<T?> Read<T>(string path) where T : class
    {
        // Open first so access errors cannot masquerade as an absent snapshot.
        FileStream stream;
        try { stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 4096, true); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        await using (stream)
            return await JsonSerializer.DeserializeAsync<T>(stream).ConfigureAwait(false)
                ?? throw new InvalidDataException("Saved sailing data is empty.");
    }

    private static Task Write<T>(string path, T snapshot)
    {
        // Own the complete bytes before scheduling disk work.
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(snapshot);
        return Task.Run(() =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (FileStream stream = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes);
                    stream.Flush(flushToDisk: true);
                }
                // Same-directory rename publishes a whole snapshot. Failed writes
                // leave the last committed destination intact.
                File.Move(temporary, path, overwrite: true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        });
    }
}
