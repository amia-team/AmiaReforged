using System.Text.Json;
using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;
namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Serial, write-before-publish economic transactions. Never reads native objects on a file/database worker.</summary>
[ServiceBinding(typeof(MaritimeLedgerService))]
public sealed class MaritimeLedgerService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ShipPersistenceOptions _persistence;
    private readonly SailingFileStore _files;
    private readonly SailingPersistenceContextFactory _factory;
    private readonly MaritimeOptions _options;
    private MaritimeEconomyState _state = new();
    private Func<Task> _main = () => Task.CompletedTask;
    public Func<MaritimeEconomyState, Task>? SaveOverride { get; set; } // Native-free fault-injection seam.
    public bool Ready { get; private set; }
    public string? Failure { get; private set; }
    public bool HasRecovery => _state.PendingMoney.Count != 0 || _state.Rentals.Any(r => r.Status == ShipRentalStatus.RecoveryRequired);
    public MaritimeLedgerService(ShipPersistenceOptions persistence, SailingFileStore files,
        SailingPersistenceContextFactory factory, MaritimeOptions options)
    { _persistence = persistence; _files = files; _factory = factory; _options = options; }
    internal void InitializeModule() => _main = async () => { await NwTask.SwitchToMainThread(); };
    public MaritimeEconomyState Snapshot() => MaritimeMarketEngine.Copy(_state);

    public async Task InitializeAsync(ShipRegistryService registry, IslandService? islands = null)
    {
        await _gate.WaitAsync();
        try
        {
            if (Ready) return;
            if (!_persistence.Enabled || _persistence.Backend == SailingPersistenceBackend.Disabled)
                throw new InvalidOperationException("Durable sailing persistence is required for rentals and maritime transactions.");
            MaritimeEconomyState? saved;
            if (_persistence.Backend == SailingPersistenceBackend.Files) saved = await _files.LoadEconomy();
            else
            {
                await using var db = _factory.CreateDbContext();
                var row = await db.MaritimeEconomy.AsNoTracking().SingleOrDefaultAsync(s => s.Id == "maritime-v1");
                saved = row == null ? null : JsonSerializer.Deserialize<MaritimeEconomyState>(row.Payload)
                    ?? throw new InvalidDataException("Empty maritime state.");
            }
            _state = saved ?? new() { Markets = _options.Markets.ToDictionary(m => m.PortId, MaritimeMarketEngine.Copy) };
            MaritimeMarketEngine.Validate(_state);
            if (islands != null && _state.Markets.Values.Any(m => !islands.GetIslands().Any(i => i.Id == m.IslandId)))
                throw new InvalidDataException("Market refers to an island not configured in the sailing system.");
            await _main();
            // Validate the whole restoration before publishing any cargo or access changes.
            foreach (ShipState ship in registry.GetShips())
            {
                var lease = _state.Rentals.LastOrDefault(r => r.ShipId == ship.ShipId && MaritimeMarketEngine.IsOpen(r.Status));
                if (CutterRentalFleet.IsCutter(ship) && lease != null &&
                    (lease.VesselDefinitionId != CutterRentalFleet.DefinitionId ||
                     !string.Equals(lease.DeckAreaResRef, ship.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) ||
                     !string.Equals(lease.CabinAreaResRef ?? "", ship.CabinAreaResRef ?? "", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException("Active cutter rental deck mapping changed; restore its original configuration before starting rentals.");
                if (ship.Rental != null && !_state.Rentals.Any(r => r.RentalId == ship.Rental.RentalId))
                    throw new InvalidDataException("A ship rental snapshot has no agreement in the economy ledger; restore the ledger backup.");
                if (_state.Vessels.TryGetValue(ship.ShipId, out var economic))
                {
                    if (economic.Cargo.Sum(c => (long)c.Quantity) > ship.CargoCapacity ||
                        economic.RentalStorageRevision > ship.RentalStorageRevision && economic.RentalHull > ship.MaximumHull)
                        throw new InvalidDataException("Economic cargo/hull exceeds configured vessel limits.");
                    if (economic.RentalStorageRevision < ship.RentalStorageRevision)
                        throw new InvalidDataException("The economy ledger is older than the ship's rental transition; restore a coherent backup.");
                }
                else if (ship.RentalStorageRevision != 0) throw new InvalidDataException("Missing rental transition ledger.");
                if (saved == null) _state.Vessels[ship.ShipId] = MaritimeMarketEngine.Capture(ship);
            }
            foreach (ShipState ship in registry.GetShips().Where(s => !s.IsInitializing)) Apply(ship, restoreRentalStorage: true);
            if (saved == null) await Save(_state);
            await _main();
            Ready = true;
            if (HasRecovery) Log.Error("Maritime transactions suspended: payment intents={0}, rental recovery obligations={1}. Administrator reconciliation is required.",
                _state.PendingMoney.Count, _state.Rentals.Count(r => r.Status == ShipRentalStatus.RecoveryRequired));
        }
        catch (Exception ex)
        { await _main(); Failure = ex.Message; Ready = false; Log.Error(ex, "Maritime persistence unavailable; previous data was left untouched and economic actions are disabled."); }
        finally { _gate.Release(); }
    }
    private async Task Save(MaritimeEconomyState state)
    {
        MaritimeMarketEngine.Validate(state);
        if (SaveOverride != null) { await SaveOverride(MaritimeMarketEngine.Copy(state)); return; }
        if (_persistence.Backend == SailingPersistenceBackend.Files) { await _files.SaveEconomy(state); return; }
        await using var db = _factory.CreateDbContext();
        var row = await db.MaritimeEconomy.SingleOrDefaultAsync(s => s.Id == "maritime-v1");
        string json = JsonSerializer.Serialize(state);
        if (row == null) db.MaritimeEconomy.Add(new() { Payload = json }); else row.Payload = json;
        await db.SaveChangesAsync();
    }
    public void Apply(ShipState ship, bool restoreRentalStorage = false)
    {
        if (_state.Vessels.TryGetValue(ship.ShipId, out var saved)) MaritimeMarketEngine.Apply(ship, saved, restoreRentalStorage);
        ship.Rental = _state.Rentals.LastOrDefault(r => r.ShipId == ship.ShipId && MaritimeMarketEngine.IsOpen(r.Status));
        // Pool membership is definition-owned. Retired Sea Sprite agreements keep their money obligations,
        // but settlement immediately returns the ordinary ship to communal access without a data rewrite.
        ship.RentalPoolId = CutterRentalFleet.IsCutter(ship) ? "southport" : null;
    }
    /// <param name="plan">Mutates an owned copy and returns the signed native gold delta. All validation belongs here.</param>
    public async Task<MaritimeResult> ExecuteAsync(IReadOnlyList<ShipState> ships,
        Func<MaritimeEconomyState, int> plan, Func<bool> validate, IMaritimeWallet? wallet = null,
        string kind = "trade", string? rentalId = null, bool waitForGate = false)
    {
        // Reject reentrant clicks immediately, rather than queueing a second purchase behind the first.
        if (waitForGate) await _gate.WaitAsync();
        else if (!await _gate.WaitAsync(0)) return MaritimeResult.Fail("Another maritime transaction is in progress. Please wait.");
        MaritimeEconomyState? before = null;
        bool moneyAttempted = false, moneyApplied = false, published = false, durableStarted = false, shipsReserved = false;
        int paymentDelta = 0; string? receipt = null;
        long originalGold = -1;
        try
        {
            await _main();
            if (!Ready || HasRecovery) return MaritimeResult.Fail(Failure ?? "Maritime transactions are unavailable pending administrator recovery.");
            if (!validate() || wallet is { IsValid: false } || ships.Any(s => s.IsEconomyBusy) || ships.Select(s => s.ShipId).Distinct().Count() != ships.Count) return MaritimeResult.Fail("This maritime interaction is no longer valid.");
            foreach (var ship in ships) ship.IsEconomyBusy = true;
            shipsReserved = true;
            before = MaritimeMarketEngine.Copy(_state);
            var proposed = MaritimeMarketEngine.Copy(before);
            foreach (var ship in ships) proposed.Vessels[ship.ShipId] = MaritimeMarketEngine.Capture(ship);
            int delta = plan(proposed);
            if (wallet == null && delta != 0) throw new InvalidOperationException("Native gold requires a character wallet.");
            if (wallet != null)
            {
                originalGold = wallet.Gold;
                if (originalGold + delta is < 0 or > uint.MaxValue) throw new InvalidOperationException("Insufficient gold or character gold limit.");
            }
            proposed.Revision++;
            MaritimeMarketEngine.Validate(proposed);
            if (wallet != null && delta != 0)
            {
                var prepared = MaritimeMarketEngine.Copy(before);
                prepared.PendingMoney.Add(new() { Account = wallet.Account, Character = wallet.Character, Kind = kind,
                    ShipId = ships.FirstOrDefault()?.ShipId ?? "", RentalId = rentalId, GoldDelta = delta, BeforeGold = originalGold, AfterGold = originalGold + delta, At = DateTime.UtcNow });
                durableStarted = true;
                await Save(prepared);
                await _main();
                if (!validate() || !wallet.IsValid || wallet.Gold != originalGold) throw new InvalidOperationException("Character changed during transaction preparation.");
                _state = prepared;
                moneyAttempted = true; paymentDelta = delta; receipt = prepared.PendingMoney.Single().Id;
                wallet.ChangeGold(delta, receipt + ":APPLIED");
                moneyApplied = true;
            }
            durableStarted = true;
            await Save(proposed);
            await _main();
            foreach (var ship in ships)
                if (proposed.Vessels[ship.ShipId].Cargo.Sum(c => (long)c.Quantity) > ship.CargoCapacity)
                    throw new InvalidOperationException("Vessel capacity changed while its transaction was saving.");
            _state = proposed;
            foreach (var ship in ships) Apply(ship);
            published = true;
            return MaritimeResult.Ok($"Transaction completed. Gold change: {(wallet == null ? 0 : delta):+0;-0;0} gp.");
        }
        catch (Exception ex)
        {
            await _main();
            if (durableStarted || ex is not InvalidOperationException) Log.Error(ex, "Maritime transaction failed: kind={0}, rental={1}.", kind, rentalId);
            if (before != null && !published && durableStarted)
            {
                try
                {
                    if (moneyAttempted)
                    {
                        if (wallet == null || !wallet.IsValid) throw new InvalidOperationException("Gold compensation needs the original connected character.");
                        if (!moneyApplied) throw new InvalidOperationException("Native payment result is ambiguous; leave the durable intent for review.");
                        // Reverse ONLY this transaction's amount, preserving unrelated gold changes.
                        wallet.ChangeGold(checked(-paymentDelta), receipt + ":REVERSED");
                    }
                    await Save(before);
                    await _main();
                    _state = before;
                }
                catch (Exception compensation)
                { Ready = false; Failure = "Administrator reconciliation is required after an incomplete payment compensation."; Log.Error(compensation, "Maritime compensation incomplete; prepared intent remains frozen for administrator recovery."); }
            }
            return MaritimeResult.Fail(!durableStarted && ex is InvalidOperationException ? ex.Message :
                "Transaction failed. No automatic retry will charge you again; unresolved payments require administrator review.");
        }
        finally
        {
            await _main();
            if (shipsReserved) foreach (var ship in ships) ship.IsEconomyBusy = false;
            _gate.Release();
        }
    }
}
