using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipStatePersistenceService))]
public class ShipStatePersistenceService : IDisposable
{
    private readonly SailingPersistenceContextFactory _factory;
    private readonly ShipNavigationService? _navigation;
    private readonly SailingPersistenceBackend _backend;
    private readonly SailingFileStore? _files;
    private readonly CoalescingSaveQueue<string, SavedShipState> _saves = new();
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public bool IsEnabled { get; }
    public ShipStatePersistenceService(SailingPersistenceContextFactory ctxFactory,
        ShipNavigationService? navigation = null, ShipPersistenceOptions? options = null, SailingFileStore? files = null)
    {
        _factory = ctxFactory; _navigation = navigation;
        _backend = options?.Backend ?? SailingPersistenceBackend.Disabled;
        IsEnabled = options?.Enabled == true && _backend != SailingPersistenceBackend.Disabled;
        _files = files ?? (IsEnabled && _backend == SailingPersistenceBackend.Files ? new SailingFileStore(options!) : null);
    }

    public async Task<IReadOnlyCollection<SavedShipState>> LoadFleet()
    {
        if (!IsEnabled) return [];
        if (_backend == SailingPersistenceBackend.Files) return await _files!.LoadShips().ConfigureAwait(false);
        await using SailingPersistenceDbContext context = _factory.CreateDbContext();
        return await context.SavedShipStates.AsNoTracking().ToListAsync().ConfigureAwait(false);
    }

    public async Task<SavedShipState?> LoadState(string shipId, string? legacyName = null)
    {
        if (!IsEnabled) return null;
        try
        {
            if (_backend == SailingPersistenceBackend.Files) return await _files!.LoadShip(shipId).ConfigureAwait(false);
            await using SailingPersistenceDbContext context = _factory.CreateDbContext();
            SavedShipState? current = await context.SavedShipStates.AsNoTracking().FirstOrDefaultAsync(s => s.ShipId == shipId).ConfigureAwait(false);
            return current ?? await context.SavedShipStates.AsNoTracking().FirstOrDefaultAsync(s =>
                s.ShipId == null && s.ShipName == (legacyName ?? shipId)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ship restoration failed for {0}; do not overwrite with startup defaults.", shipId);
            throw;
        }
    }

    public Task SaveState(ShipState ship) => TrySaveState(ship);

    /// <summary>
    /// Commits a new ship's default while its initialization gate still excludes
    /// retrieval and world actions. Existing or unreadable records are never seeded.
    /// </summary>
    public async Task<bool> TrySaveInitialState(ShipState ship)
    {
        if (!IsEnabled || !ship.IsInitializing) return false;
        SavedShipState snapshot = ShipSnapshotCodec.Capture(ship, _navigation?.GetRoute(ship));
        try
        {
            if (await LoadState(snapshot.ShipId!, snapshot.ShipName).ConfigureAwait(false) != null) return false;
            return await _saves.Enqueue(snapshot.ShipId!, snapshot, WriteSnapshot).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Initial ship snapshot was not saved for {0}; existing data was left untouched.", snapshot.ShipId);
            return false;
        }
    }

    public Task<bool> TrySaveState(ShipState ship)
    {
        if (!IsEnabled || ship.IsInitializing || ship.IsDocking || ship.IsEconomyBusy) return Task.FromResult(false);
        SavedShipState snapshot = ShipSnapshotCodec.Capture(ship, _navigation?.GetRoute(ship));
        return _saves.Enqueue(ship.ShipId, snapshot, WriteSnapshot);
    }

    private async Task<bool> WriteSnapshot(SavedShipState snapshot)
    {
        try
        {
            if (_backend == SailingPersistenceBackend.Files)
            { await _files!.SaveShip(snapshot).ConfigureAwait(false); return true; }
            await using SailingPersistenceDbContext context = _factory.CreateDbContext();
            SavedShipState? existing = await context.SavedShipStates.FirstOrDefaultAsync(s => s.ShipId == snapshot.ShipId).ConfigureAwait(false);
            existing ??= await context.SavedShipStates.FirstOrDefaultAsync(s => s.ShipId == null && s.ShipName == snapshot.ShipName).ConfigureAwait(false);
            if (existing == null) context.SavedShipStates.Add(snapshot);
            else
            {
                existing.ShipId = snapshot.ShipId; existing.ShipName = snapshot.ShipName;
                existing.AreaResRef = snapshot.AreaResRef; existing.X = snapshot.X; existing.Y = snapshot.Y; existing.Z = snapshot.Z;
                existing.Heading = snapshot.Heading; existing.Hull = snapshot.Hull; existing.Underway = snapshot.Underway;
                existing.WeaponResRef = snapshot.WeaponResRef; existing.SnapshotJson = snapshot.SnapshotJson;
            }
            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) { Log.Error(ex, "Ship snapshot save failed for {0}.", snapshot.ShipId); return false; }
    }

    public Task<bool> FlushAsync(CancellationToken cancellation = default) => _saves.FlushAsync(cancellation);
    public void Dispose()
    {
        try
        {
            if (!FlushAsync().Wait(TimeSpan.FromSeconds(10))) Log.Error("Timed out flushing sailing ship saves during shutdown.");
        }
        catch (Exception ex) { Log.Error(ex, "Sailing ship shutdown flush failed."); }
    }
}
