using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ChartDiscoveryPersistenceService))]
public sealed class ChartDiscoveryPersistenceService(SailingPersistenceContextFactory factory, ShipPersistenceOptions options,
    SailingFileStore? files = null) : IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly CoalescingSaveQueue<(string, string), SavedChartDiscovery> _saves = new();
    private readonly SailingFileStore? _files = files ?? (options.Enabled && options.Backend == SailingPersistenceBackend.Files
        ? new SailingFileStore(options) : null);
    public bool IsEnabled => options.Enabled && options.Backend != SailingPersistenceBackend.Disabled;

    public async Task<IReadOnlyCollection<SavedChartDiscovery>> Load(string account)
    {
        if (!IsEnabled) return [];
        if (options.Backend == SailingPersistenceBackend.Files) return await _files!.LoadCharts(account).ConfigureAwait(false);
        await using SailingPersistenceDbContext context = factory.CreateDbContext();
        return await context.Set<SavedChartDiscovery>().AsNoTracking().Where(s => s.AccountKey == account).ToListAsync().ConfigureAwait(false);
    }
    public Task<bool> Save(SavedChartDiscovery snapshot)
    {
        if (!IsEnabled || !snapshot.IsValid()) return Task.FromResult(false);
        // NWN resrefs are case-insensitive; PostgreSQL text equality is not.
        // Use the same canonical area key in the queue and the stored row.
        SavedChartDiscovery owned = new()
        {
            AccountKey = snapshot.AccountKey,
            AreaResRef = snapshot.AreaResRef.ToLowerInvariant(),
            Cells = snapshot.Cells
        };
        return _saves.Enqueue((owned.AccountKey, owned.AreaResRef), owned, Write);
    }
    private async Task<bool> Write(SavedChartDiscovery snapshot)
    {
        try
        {
            if (options.Backend == SailingPersistenceBackend.Files)
            { await _files!.SaveChart(snapshot).ConfigureAwait(false); return true; }
            await using SailingPersistenceDbContext context = factory.CreateDbContext();
            SavedChartDiscovery? existing = await context.Set<SavedChartDiscovery>().FirstOrDefaultAsync(s =>
                s.AccountKey == snapshot.AccountKey && s.AreaResRef == snapshot.AreaResRef).ConfigureAwait(false);
            if (existing == null) context.Set<SavedChartDiscovery>().Add(snapshot);
            else existing.Cells = snapshot.Cells;
            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) { Log.Error(ex, "Chart discovery save failed."); return false; }
    }
    public Task<bool> FlushAsync(CancellationToken cancellation = default) => _saves.FlushAsync(cancellation);
    public void Dispose()
    {
        try
        {
            if (!FlushAsync().Wait(TimeSpan.FromSeconds(10))) Log.Error("Timed out flushing sailing chart saves during shutdown.");
        }
        catch (Exception ex) { Log.Error(ex, "Sailing chart shutdown flush failed."); }
    }
}
