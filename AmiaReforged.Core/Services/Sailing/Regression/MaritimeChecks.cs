#if SAILING_REGRESSION
using System.Text.Json;
using AmiaReforged.Core.Models.Sailing;
using AmiaReforged.Core.Services.Sailing;
using Microsoft.EntityFrameworkCore;

static class MaritimeChecks
{
    sealed class Wallet : IMaritimeWallet
    {
        public string Account { get; init; } = "account-A";
        public string Character { get; init; } = "character-A";
        public long Gold { get; set; } = 5000;
        public bool IsValid { get; set; } = true;
        public int Calls;
        public bool ThrowAfterPayment;
        public void ChangeGold(int delta, string? receipt = null)
        {
            if (!IsValid || Gold + delta < 0 || Gold + delta > uint.MaxValue) throw new InvalidOperationException("Invalid wallet.");
            Calls++; Gold += delta;
            if (ThrowAfterPayment) throw new InvalidOperationException("Ambiguous native result.");
        }
    }
    sealed class Fixture : IDisposable
    {
        public readonly string Directory;
        public readonly MaritimeOptions Options = new();
        public readonly ShipRegistryService Registry = new();
        public readonly SailingFileStore Files;
        public readonly MaritimeLedgerService Ledger;
        public readonly ShipState Ship;
        public readonly SailingPersistenceContextFactory Factory;
        public Fixture(string? directory = null, SailingPersistenceBackend backend = SailingPersistenceBackend.Files)
        {
            Directory = directory ?? Path.Combine(Path.GetTempPath(), "amia-maritime-" + Guid.NewGuid().ToString("N"));
            var persistence = new ShipPersistenceOptions { Backend = backend, DataDirectory = Directory };
            Files = new(persistence);
            Factory = SailingPersistenceContextFactory.ForOptions(new DbContextOptionsBuilder<SailingPersistenceDbContext>()
                .UseInMemoryDatabase("maritime-" + Guid.NewGuid().ToString("N")).Options);
            Ledger = new(persistence, Files, Factory, Options);
            Ship = new() { ShipId = "test-cutter", ShipName = "Test Cutter", DefinitionId = CutterRentalFleet.DefinitionId, AllowsWeapons = false,
                WeaponResRef = "", DeckAreaResRef = "test_cut_deck", AreaResRef = "ocean_01",
                IsStored = true, StoredPortId = "southport", RentalPoolId = "southport", CargoCapacity = 50 };
            Registry.Register(Ship);
        }
        public Task Initialize() => Ledger.InitializeAsync(Registry, new IslandService());
        public void Dispose() { if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, true); }
    }
    static ShipRentalRecord Agreement(Fixture f, Wallet wallet) => new() { ShipId = f.Ship.ShipId,
        RenterAccount = wallet.Account, PayerCharacter = wallet.Character, PortId = "southport", Fee = 100, Deposit = 200,
        DamageRate = 2, CheckoutHull = f.Ship.Hull, Status = ShipRentalStatus.CheckoutPending };
    static async Task<MaritimeResult> Reserve(Fixture f, ShipRentalRecord record) => await f.Ledger.ExecuteAsync([f.Ship],
        state => ShipRentalRules.Reserve(state, f.Ship, record), () => true, kind: "rental-reserve");
    static async Task<MaritimeResult> Activate(Fixture f, ShipRentalRecord record, Wallet wallet) => await f.Ledger.ExecuteAsync([f.Ship],
        state => ShipRentalRules.Activate(state, record.RentalId, wallet.Account, DateTime.UtcNow), () => wallet.IsValid, wallet, "rental-checkout", record.RentalId);
    static bool Throws(Action action) { try { action(); return false; } catch (InvalidOperationException) { return true; } }
    public static async Task Run(Action<bool, string> check)
    {
        using var f = new Fixture(); await f.Initialize();
        check(f.Ledger.Ready && !f.Ledger.HasRecovery, "Fresh maritime store initializes validated markets and vessel economics.");
        check(!f.Options.RentalsEnabled, "Paid rentals are opt-in until live transfer/resource validation; communal fleet is preserved by default.");
        var state = f.Ledger.Snapshot();
        check(state.Markets.Count == 2 && state.Markets["driftwood"].IslandId == "driftwood_isle", "Only verified configured markets are seeded, with explicit storage-island mapping.");
        check(MaritimeMarketEngine.Prices(state.Markets["southport"].Goods["grain"]).Buy == 8 &&
            MaritimeMarketEngine.Prices(state.Markets["southport"].Goods["timber"]).Sell == 18 &&
            MaritimeMarketEngine.Prices(state.Markets["driftwood"].Goods["timber"]).Buy == 10 &&
            MaritimeMarketEngine.Prices(state.Markets["driftwood"].Goods["grain"]).Sell == 15, "Initial supplier and destination prices preserve existing route trade prices.");
        foreach (var g in state.Markets.Values.SelectMany(m => m.Goods.Values))
            foreach (int testedStock in new[] { 0, g.TargetStock, g.MaximumStock })
            {
                var p = MaritimeMarketEngine.Prices(g, testedStock);
                check(p.Sell >= g.Floor && p.Buy <= g.Ceiling && p.Buy - p.Sell == g.Spread, "All configured price extremes retain bounded, positive spreads.");
            }
        DateTime now = DateTime.UtcNow;
        var adjusted = MaritimeMarketEngine.Copy(state.Markets["southport"]);
        adjusted.AdjustedAt = now.AddDays(-10); MaritimeMarketEngine.Adjust(adjusted, now, 300);
        check(adjusted.Goods["grain"].Stock == 400 && adjusted.Goods["timber"].Stock == 0, "Periodic stock adjustment bounds long offline intervals and prevents negative stock.");
        var same = MaritimeMarketEngine.Copy(state); var vessel = same.Vessels[f.Ship.ShipId];
        long gold = 10000;
        int outflow = MaritimeMarketEngine.PortTrade(same, vessel, 50, "southport", "grain", 50, true, gold, now); gold += outflow;
        int inflow = MaritimeMarketEngine.PortTrade(same, vessel, 50, "southport", "grain", 50, false, gold, now); gold += inflow;
        check(gold < 10000 && vessel.Cargo.Count == 0 && same.Markets["southport"].Goods["grain"].Stock == 200,
            "Same-port buy/sell roundtrip loses the spread, preserves stock and cannot create infinite profit.");
        check(Throws(() => MaritimeMarketEngine.PortTrade(same, vessel, 50, "missing", "grain", 1, true, gold, now)) &&
            Throws(() => MaritimeMarketEngine.PortTrade(same, vessel, 50, "southport", "grain", -1, true, gold, now)), "Unknown ports and negative quantities are rejected.");
        check(Throws(() => MaritimeMarketEngine.PortTrade(same, vessel, 50, "southport", "grain", 51, true, gold, now)) &&
            Throws(() => MaritimeMarketEngine.PortTrade(same, vessel, 50, "southport", "grain", 1, true, 0, now)), "Capacity and insufficient gold reject purchases without publishing cargo.");
        long treasury = same.Markets["southport"].Treasury;
        check(treasury + gold == 60000, "Port treasury and player gold are conserved across both transaction directions.");
        var travel = MaritimeMarketEngine.Copy(state); var carrier = travel.Vessels[f.Ship.ShipId];
        int buy = MaritimeMarketEngine.PortTrade(travel, carrier, 50, "southport", "grain", 50, true, 5000, now);
        int sell = MaritimeMarketEngine.PortTrade(travel, carrier, 50, "driftwood", "grain", 50, false, 5000 + buy, now);
        check(buy + sell > 100, "A configured inter-port voyage can earn profit after the default fixed rental fee.");

        Wallet wallet = new(); var record = Agreement(f, wallet);
        check((await Reserve(f, record)).Success && f.Ship.Rental?.Status == ShipRentalStatus.CheckoutPending,
            "Rental reservation durably claims exactly one existing vessel without charging gold.");
        check(!(await Reserve(f, Agreement(f, new Wallet { Account = "account-B" }))).Success,
            "Concurrent/sequential competing renter cannot reserve an already claimed vessel.");
        Wallet poor = new() { Gold = 10 }; long original = wallet.Gold;
        check(!(await Activate(f, record, poor)).Success && poor.Gold == 10 && poor.Calls == 0,
            "Insufficient funds cannot activate or charge an agreement.");
        bool engineArrived = false, aboard = false; var frame = new TaskCompletionSource();
        Task<bool> checkout = ShipTransferTransaction.ExecuteAsync([
            (async () => { await frame.Task; engineArrived = true; }, () => { engineArrived = false; return Task.CompletedTask; },
                () => { if (!engineArrived) throw new InvalidOperationException("Not arrived."); })],
            () => { new ShipStorageService().RetrieveShip(f.Ship, "southport"); aboard = true; },
            prepareCommit: async () => { if (!(await Activate(f, record, wallet)).Success) throw new InvalidOperationException("Payment failed."); });
        check(!checkout.IsCompleted && !aboard && wallet.Gold == original && f.Ship.IsStored,
            "Deferred engine transfer leaves rental payment, storage and membership uncommitted.");
        frame.SetResult(); check(await checkout && aboard && !f.Ship.IsStored && wallet.Gold == original - 300 && wallet.Calls == 1,
            "Confirmed arrival activates the agreement and charges fee/deposit exactly once before transfer commit.");
        check(!(await Activate(f, record, wallet)).Success && wallet.Gold == original - 300 && wallet.Calls == 1,
            "Duplicate activation/click cannot charge an active rental again.");
        check(ShipAccessService.MayBoard(f.Ship, wallet.Account) && !ShipAccessService.MayBoard(f.Ship, "intruder") &&
            !ShipAccessService.MayCommand(f.Ship, "intruder"), "Account authorization protects rental boarding and command outside NUI handlers.");
        await f.Ledger.ExecuteAsync([f.Ship], st => { st.Rentals.Single(r => r.RentalId == record.RentalId).Guests.Add("guest"); return 0; }, () => true, kind: "boarding-consent");
        check(ShipAccessService.MayBoard(f.Ship, "guest") && !ShipAccessService.MayCommand(f.Ship, "guest"), "Explicitly invited passengers can board but cannot command or return a rental.");
        check(ShipAccessService.MayRecoverMembership(f.Ship, wallet.Account, true) &&
            ShipAccessService.MayRecoverMembership(f.Ship, "guest", true) && !ShipAccessService.MayRecoverMembership(f.Ship, "intruder", true) &&
            !ShipAccessService.MayRecoverMembership(f.Ship, wallet.Account, false),
            "Authorized existing rental occupants can recover deck membership for safe exits even when new economic actions are suspended.");
        check(!ShipRentalService.CanReturn(f.Ship, "guest", "southport", false) &&
            !ShipRentalService.CanReturn(f.Ship, wallet.Account, "southport", false), "Passenger and underway rental returns are rejected independently of disembarking.");
        using var persistence = new ShipStatePersistenceService(f.Factory, options: new ShipPersistenceOptions { DataDirectory = f.Directory });
        check(await persistence.TrySaveState(f.Ship), "Active retrieved rental checkpoints through the existing ship persistence infrastructure.");
        var restored = new ShipState { ShipId = f.Ship.ShipId, ShipName = "renamed", DefinitionId = CutterRentalFleet.DefinitionId, AllowsWeapons = false,
            DeckAreaResRef = "test_cut_deck", AreaResRef = "ocean_01" };
        check(ShipSnapshotCodec.TryRestore(restored, (await persistence.LoadState(f.Ship.ShipId))!, out _) && !restored.IsStored &&
            restored.Rental?.RenterAccount == wallet.Account && restored.Rental.RentalId == record.RentalId,
            "Ship restart restoration keeps active storage state, stable rental identity and account even with changed display name.");
        var rebootRegistry = new ShipRegistryService(); rebootRegistry.Register(restored);
        var reboot = new MaritimeLedgerService(new ShipPersistenceOptions { DataDirectory = f.Directory }, f.Files, f.Factory, f.Options);
        await reboot.InitializeAsync(rebootRegistry);
        check(reboot.Ready && restored.Rental?.Status == ShipRentalStatus.Active && restored.Rental.Guests.Contains("guest") && wallet.Calls == 1,
            "Restart restores rental economics without recreating ships or replaying payment.");
        f.Ship.IsStored = true; f.Ship.StoredPortId = "southport"; f.Ship.Hull = 85;
        f.Ship.Cargo.Add(new() { ItemId = "grain", Quantity = 1 });
        check(!ShipRentalService.CanReturn(f.Ship, wallet.Account, "southport", false), "Cargo must be settled explicitly; rental return never destroys cargo.");
        f.Ship.Cargo.Clear();
        var settlement = ShipRentalService.Settlement(f.Ship.Rental!, f.Ship.Hull);
        check(settlement == (30, 170) && ShipRentalService.Settlement(f.Ship.Rental!, 0) == (200, 0), "Damage settlement is conservative, agreement-priced and capped by the deposit.");
        check(!ShipRentalService.CanReturn(f.Ship, wallet.Account, "southport", true), "Rental return waits for all aboard memberships to leave through docking.");
        var returned = await f.Ledger.ExecuteAsync([f.Ship], s => ShipRentalRules.Return(s, f.Ship, wallet.Account, "southport", false, now),
            () => true, wallet, "rental-return", record.RentalId);
        check(returned.Success && wallet.Gold == original - 130 && f.Ship.Rental == null && f.Ship.Hull == 85 && f.Ship.IsStored,
            "Successful return refunds only the remaining deposit, preserves hull, and ends only this rental.");
        check(ShipRentalService.Available(f.Ship, "southport") && !ShipAccessService.MayBoard(f.Ship, wallet.Account),
            "Returned vessel becomes rentable again and is no longer freely retrievable by its previous renter.");
        check(!f.Ledger.Snapshot().Rentals.Any(r => MaritimeMarketEngine.IsOpen(r.Status)), "Returned agreement remains in history without an outstanding obligation.");

        using var rollback = new Fixture(); await rollback.Initialize(); Wallet payer = new(); var reserved = Agreement(rollback, payer);
        await Reserve(rollback, reserved);
        int writes = 0; rollback.Ledger.SaveOverride = async s => { if (++writes == 2) throw new IOException("Injected final write failure."); await rollback.Files.SaveEconomy(s); };
        var failed = await Activate(rollback, reserved, payer);
        check(!failed.Success && payer.Gold == 5000 && payer.Calls == 2 && rollback.Ship.Rental?.Status == ShipRentalStatus.CheckoutPending,
            "Failed checkout persistence reverses its exact debit and preserves the original rental/ship state.");
        check(!rollback.Ledger.HasRecovery && rollback.Ledger.Ready, "Successful compensation clears the durable prepared-payment journal.");
        rollback.Ledger.SaveOverride = null;
        bool moved = false, committed = false;
        var failedArrival = await ShipTransferTransaction.ExecuteAsync([
            (() => Task.FromException(new TimeoutException("Missing deck/deferred arrival")), () => { moved = false; return Task.CompletedTask; }, () => { })],
            () => committed = true, prepareCommit: async () => { moved = true; await Activate(rollback, reserved, payer); },
            abortCommit: async () => { await rollback.Ledger.ExecuteAsync([rollback.Ship], s => ShipRentalRules.Cancel(s, reserved.RentalId, false, now), () => true); });
        check(!failedArrival && !moved && !committed && payer.Gold == 5000 && rollback.Ship.IsStored,
            "Missing/deferred destination never reaches payment preparation and cancels the uncharged reservation safely.");
        using var concurrent = new Fixture(); await concurrent.Initialize(); var started = new TaskCompletionSource(); var unblock = new TaskCompletionSource();
        concurrent.Ledger.SaveOverride = async s => { started.TrySetResult(); await unblock.Task; await concurrent.Files.SaveEconomy(s); };
        Task<MaritimeResult> first = Reserve(concurrent, Agreement(concurrent, new Wallet())); await started.Task;
        var second = await Reserve(concurrent, Agreement(concurrent, new Wallet { Account = "second" }));
        check(!second.Success && concurrent.Ship.IsEconomyBusy, "Concurrent economic attempts fail immediately while the first durable reservation is pending.");
        unblock.SetResult(); check((await first).Success && !concurrent.Ship.IsEconomyBusy, "Successful reservation releases the economic gate once.");
        // Economic rollback cleanup waits asynchronously for a different vessel's writer.
        var cancellationStarted = new TaskCompletionSource(); var cancellationUnblock = new TaskCompletionSource();
        concurrent.Ledger.SaveOverride = async st => { cancellationStarted.TrySetResult(); await cancellationUnblock.Task; await concurrent.Files.SaveEconomy(st); };
        Task<MaritimeResult> unrelated = concurrent.Ledger.ExecuteAsync([concurrent.Ship], _ => 0, () => true);
        await cancellationStarted.Task;
        string pendingId = concurrent.Ship.Rental!.RentalId;
        Task<MaritimeResult> cleanup = concurrent.Ledger.ExecuteAsync([concurrent.Ship], st => ShipRentalRules.Cancel(st, pendingId, false, now), () => true,
            kind: "rental-cancel", waitForGate: true);
        check(!cleanup.IsCompleted, "Rollback cleanup waits asynchronously for the economic writer instead of stranding an uncharged reservation.");
        cancellationUnblock.SetResult(); await unrelated;
        check((await cleanup).Success && concurrent.Ship.Rental == null, "Queued cleanup cancels only its own uncharged agreement after the writer releases.");
        using var ambiguous = new Fixture(); await ambiguous.Initialize(); Wallet uncertain = new() { ThrowAfterPayment = true }; var agreement = Agreement(ambiguous, uncertain);
        await Reserve(ambiguous, agreement); var unknown = await Activate(ambiguous, agreement, uncertain);
        check(!unknown.Success && !ambiguous.Ledger.Ready && ambiguous.Ledger.HasRecovery, "Ambiguous native payment freezes a durable intent rather than guessing/refunding unrelated money.");
        var afterCrash = new MaritimeLedgerService(new ShipPersistenceOptions { DataDirectory = ambiguous.Directory }, ambiguous.Files, ambiguous.Factory, ambiguous.Options);
        await afterCrash.InitializeAsync(ambiguous.Registry);
        check(afterCrash.HasRecovery && !(await afterCrash.ExecuteAsync([ambiguous.Ship], _ => -300, () => true, uncertain)).Success && uncertain.Calls == 1,
            "Restart with an unresolved payment never recharges, refunds or duplicates an active checkout automatically.");

        using var marketFixture = new Fixture(); await marketFixture.Initialize(); Wallet trader = new(); marketFixture.Ship.RentalPoolId = null;
        int stock = marketFixture.Ledger.Snapshot().Markets["southport"].Goods["grain"].Stock;
        var purchased = await marketFixture.Ledger.ExecuteAsync([marketFixture.Ship], s => MaritimeMarketEngine.PortTrade(s, s.Vessels[marketFixture.Ship.ShipId], 50,
            "southport", "grain", 20, true, trader.Gold, now), () => true, trader);
        check(purchased.Success && marketFixture.Ship.Cargo.Single().Quantity == 20 && trader.Gold == 4840 &&
            marketFixture.Ledger.Snapshot().Markets["southport"].Goods["grain"].Stock == stock - 20,
            "Actual ledger purchase commits quantity, native-wallet payment, vessel cargo and port stock together.");
        var economicReload = new MaritimeLedgerService(new ShipPersistenceOptions { DataDirectory = marketFixture.Directory }, marketFixture.Files, marketFixture.Factory, marketFixture.Options);
        await economicReload.InitializeAsync(marketFixture.Registry);
        check(economicReload.Snapshot().Markets["southport"].Goods["grain"].Stock == stock - 20 && marketFixture.Ship.Cargo.Single().Quantity == 20,
            "Market inventories and ship cargo survive an economic restart without default resets.");
        var merchantState = marketFixture.Ledger.Snapshot();
        var merchant = new ShipState { ShipId = "Golden Gull", ShipName = "Golden Gull", ShipType = ShipType.Merchant, DeckAreaResRef = "golden_gull_d2",
            AreaResRef = "ocean_01", CargoCapacity = 120, MerchantGold = 1000, IsInPort = true, CurrentTradePortId = "southport", PortDepartureTime = now.AddSeconds(30) };
        merchant.Cargo.Add(new() { ItemId = "timber", Quantity = 50 }); merchantState.Vessels[merchant.ShipId] = MaritimeMarketEngine.Capture(merchant);
        int beforeTimber = merchantState.Markets["southport"].Goods["timber"].Stock;
        check(MerchantTradeService.NpcArrival(merchantState, merchant, "southport", "driftwood", 1, now, f.Options), "NPC merchant processes a new, explicit route arrival.");
        var merchantEconomics = merchantState.Vessels[merchant.ShipId]; int tradeCount = merchantState.RecentTrades.Count;
        check(merchantEconomics.MerchantGold == 1500 && merchantEconomics.Cargo.Single().ItemId == "grain" && merchantEconomics.Cargo.Single().Quantity == 50 &&
            merchantState.Markets["southport"].Goods["timber"].Stock == beforeTimber + 50, "Golden Gull sells actual timber, buys grain for its next destination and changes the port market.");
        check(!MerchantTradeService.NpcArrival(merchantState, merchant, "southport", "driftwood", 1, now, f.Options) && merchantState.RecentTrades.Count == tradeCount,
            "Repeated/reloaded arrival identifier cannot duplicate merchant cargo, gold or transactions.");
        var merchantReload = MaritimeMarketEngine.Copy(merchantState); MaritimeMarketEngine.Apply(merchant, merchantReload.Vessels[merchant.ShipId]);
        check(merchant.IsInPort && merchant.MerchantArrivalSequence == 1 && merchant.MerchantGold == 1500 && merchant.PortDepartureTime == now.AddSeconds(30),
            "Merchant ledger restoration preserves completed arrival, port stay, cargo and gold.");
        check(Throws(() => MerchantTradeService.NpcArrival(merchantReload, merchant, "missing", "driftwood", 2, now, f.Options)), "Missing NPC port market is explicit failure, not a fabricated trade.");
        var emptyMerchant = new VesselEconomics { ShipId = "seller", MerchantGold = 1000, Cargo = [new() { ItemId = "grain", Quantity = 10 }] };
        var playerShip = new VesselEconomics { ShipId = "buyer", MerchantGold = 0 };
        int delta = MerchantTradeService.MerchantExchange(merchantReload, playerShip, emptyMerchant, 50, 120, 100000, "southport", "grain", 4, true, 1000, now);
        check(delta == -32 && playerShip.Cargo.Single().Quantity == 4 && emptyMerchant.Cargo.Single().Quantity == 6 && emptyMerchant.MerchantGold == 1032,
            "Explicit merchant exchange moves real cargo and gold between the captured ships.");
        check(Throws(() => MerchantTradeService.MerchantExchange(merchantReload, playerShip, emptyMerchant, 50, 120, 100000, "southport", "grain", 7, true, 1000, now)),
            "Merchant quantity cannot exceed actual cargo stock.");
        int reverse = MerchantTradeService.MerchantExchange(merchantReload, playerShip, emptyMerchant, 50, 120, 100000, "southport", "grain", 4, false, 968, now);
        check(delta + reverse < 0 && playerShip.Cargo.Count == 0, "Merchant resale also retains the spread and cannot manufacture profit.");

        check(ShipRentalRules.ResourcesValid(true, true, true, true, true, true), "Existing complete rental mappings pass the shared resource gate.");
        for (int missing = 0; missing < 6; missing++)
        {
            bool[] resources = [true, true, true, true, true, true]; resources[missing] = false;
            check(!ShipRentalRules.ResourcesValid(resources[0], resources[1], resources[2], resources[3], resources[4], resources[5]),
                "Missing physical/deck/cabin/helm/exit or ambiguous deck rejects paid checkout before any money operation.");
        }
        using var durable = new Fixture(); await durable.Initialize(); Wallet durablePayer = new(); var durableAgreement = Agreement(durable, durablePayer);
        var oldStoredSnapshot = ShipSnapshotCodec.Capture(durable.Ship);
        await Reserve(durable, durableAgreement); await Activate(durable, durableAgreement, durablePayer);
        var staleShip = new ShipState { ShipId = durable.Ship.ShipId, ShipName = durable.Ship.ShipName, DeckAreaResRef = durable.Ship.DeckAreaResRef, AreaResRef = "ocean_01" };
        ShipSnapshotCodec.TryRestore(staleShip, oldStoredSnapshot, out _);
        var staleRegistry = new ShipRegistryService(); staleRegistry.Register(staleShip);
        var recovered = new MaritimeLedgerService(new ShipPersistenceOptions { DataDirectory = durable.Directory }, durable.Files, durable.Factory, durable.Options);
        await recovered.InitializeAsync(staleRegistry);
        check(recovered.Ready && !staleShip.IsStored && staleShip.Rental?.Status == ShipRentalStatus.Active,
            "Committed checkout journal restores active vessel storage even when the independent ship checkpoint was not saved.");
        var activeSnapshot = ShipSnapshotCodec.Capture(staleShip);
        durable.Ship.IsStored = true; durable.Ship.StoredPortId = "southport"; durable.Ship.Hull = 90;
        await durable.Ledger.ExecuteAsync([durable.Ship], st => ShipRentalRules.Return(st, durable.Ship, durablePayer.Account, "southport", false, now),
            () => true, durablePayer, "rental-return", durableAgreement.RentalId);
        ShipSnapshotCodec.TryRestore(staleShip, activeSnapshot, out _);
        var restoredReturn = new MaritimeLedgerService(new ShipPersistenceOptions { DataDirectory = durable.Directory }, durable.Files, durable.Factory, durable.Options);
        await restoredReturn.InitializeAsync(staleRegistry);
        check(restoredReturn.Ready && staleShip.IsStored && staleShip.StoredPortId == "southport" && staleShip.Hull == 90 && staleShip.Rental == null,
            "Committed return journal restores storage and settled hull over an older active checkpoint without resurrecting the rental.");
        staleShip.RentalStorageRevision += 100;
        var outdated = new MaritimeLedgerService(new ShipPersistenceOptions { DataDirectory = durable.Directory }, durable.Files, durable.Factory, durable.Options);
        await outdated.InitializeAsync(staleRegistry);
        check(!outdated.Ready && staleShip.RentalStorageRevision > restoredReturn.Snapshot().Revision,
            "An older economy backup is rejected instead of downgrading a newer ship rental transition.");

        using var lateFailure = new Fixture(); await lateFailure.Initialize(); Wallet lateWallet = new(); var lateRecord = Agreement(lateFailure, lateWallet);
        await Reserve(lateFailure, lateRecord); int validation = 0, teleports = 0, reversals = 0; bool lateMembership = false;
        bool late = await ShipTransferTransaction.ExecuteAsync([
            (() => { teleports++; return Task.CompletedTask; }, () => { reversals++; return Task.CompletedTask; },
                () => { if (++validation == 2) throw new InvalidOperationException("Actor changed after economic prepare."); })],
            () => lateMembership = true,
            prepareCommit: async () => { if (!(await Activate(lateFailure, lateRecord, lateWallet)).Success) throw new Exception("Payment."); },
            abortCommit: async () => { await lateFailure.Ledger.ExecuteAsync([lateFailure.Ship], st => ShipRentalRules.Cancel(st, lateRecord.RentalId, true, now),
                () => true, lateWallet, "rental-cancel", lateRecord.RentalId); });
        check(!late && !lateMembership && lateWallet.Gold == 5000 && lateWallet.Calls == 2 && teleports == 1 && reversals == 1 && lateFailure.Ship.Rental == null,
            "Late transfer validation failure refunds this exact checkout, cancels its agreement and rolls back the single attempted movement.");
        check(lateFailure.Ship.IsStored && lateFailure.Ledger.Snapshot().Rentals.Single().Status == ShipRentalStatus.Cancelled,
            "Late checkout rollback preserves the stored vessel and an auditable cancelled agreement.");
        using var externalGold = new Fixture(); await externalGold.Initialize(); Wallet externalWallet = new(); var externalRecord = Agreement(externalGold, externalWallet);
        await Reserve(externalGold, externalRecord); int externalWrites = 0;
        externalGold.Ledger.SaveOverride = async st =>
        {
            if (++externalWrites == 2) { externalWallet.Gold += 7; throw new IOException("Final save failed while unrelated reward arrived."); }
            await externalGold.Files.SaveEconomy(st);
        };
        await Activate(externalGold, externalRecord, externalWallet);
        check(externalWallet.Gold == 5007, "Payment compensation reverses only its own debit, preserving unrelated gold awarded during the asynchronous write.");
        using var merchantFixture = new Fixture(); await merchantFixture.Initialize();
        merchantFixture.Registry.Register(merchant);
        var actualNpc = await merchantFixture.Ledger.ExecuteAsync([merchant], st =>
        { MerchantTradeService.NpcArrival(st, merchant, "driftwood", "southport", 2, now, merchantFixture.Options); return 0; }, () => true, kind: "npc-arrival");
        check(actualNpc.Success && merchant.MerchantArrivalSequence == 2 && merchant.Cargo.Any(c => c.ItemId == "timber") && merchant.MerchantGold >= 0,
            "Second route arrival executes through the real durable ledger and buys reciprocal cargo.");
        var npcReload = new MaritimeLedgerService(new ShipPersistenceOptions { DataDirectory = merchantFixture.Directory }, merchantFixture.Files, merchantFixture.Factory, merchantFixture.Options);
        await npcReload.InitializeAsync(merchantFixture.Registry);
        check(npcReload.Ready && merchant.MerchantArrivalSequence == 2 && merchant.Cargo.Any(c => c.ItemId == "timber"),
            "NPC merchant cargo, gold and completed arrival survive the actual file-store restart.");

        using var owed = new Fixture(); await owed.Initialize(); Wallet owedPayer = new(); var owedAgreement = Agreement(owed, owedPayer);
        await Reserve(owed, owedAgreement); await Activate(owed, owedAgreement, owedPayer);
        var recovery = await owed.Ledger.ExecuteAsync([owed.Ship], st => ShipRentalRules.RequireRecovery(st, owedAgreement.RentalId, 300, "Payer disconnected before rollback refund."), () => true);
        check(recovery.Success && owed.Ledger.HasRecovery && owed.Ship.Rental?.GoldOwed == 300 && !ShipAccessService.MayCommand(owed.Ship, owedPayer.Account),
            "Known failed checkout refund records a durable recovery obligation and suspends command/new transactions.");
        check(ShipAccessService.MayRecoverMembership(owed.Ship, owedPayer.Account, true), "A recovery-frozen renter already on the deck can still recover membership to leave safely.");
        var owedReboot = new MaritimeLedgerService(new ShipPersistenceOptions { DataDirectory = owed.Directory }, owed.Files, owed.Factory, owed.Options);
        await owedReboot.InitializeAsync(owed.Registry);
        check(owedReboot.Ready && owedReboot.HasRecovery && owed.Ship.IsStored && owed.Ship.Rental?.GoldOwed == 300,
            "Restart preserves the refund obligation and stored recovery state without replaying a charge/refund.");
        var wealthy = new ShipState { ShipId = "wealthy", ShipName = "wealthy", ShipType = ShipType.Merchant,
            DeckAreaResRef = "deck", AreaResRef = "ocean_01", CargoCapacity = 120, MerchantGold = int.MaxValue - 9 };
        wealthy.Cargo.Add(new() { ItemId = "timber", Quantity = 50 });
        var wealthState = f.Ledger.Snapshot(); wealthState.Vessels[wealthy.ShipId] = MaritimeMarketEngine.Capture(wealthy);
        MerchantTradeService.NpcArrival(wealthState, wealthy, "southport", "driftwood", 1, now, f.Options);
        check(wealthState.Vessels[wealthy.ShipId].MerchantGold == int.MaxValue - 409 &&
            MaritimeMarketEngine.Cargo(wealthState.Vessels[wealthy.ShipId], "timber") == 50,
            "Legacy large merchant balances neither overflow nor lose unsold cargo while respecting the configured credit limit.");

        await CrewChecks(check);
        using var dbFixture = new Fixture(backend: SailingPersistenceBackend.PostgreSql); await dbFixture.Initialize();
        check(dbFixture.Ledger.Ready, "Maritime database integration roundtrips through the isolated sailing-only EF model.");
        using (var db = dbFixture.Factory.CreateDbContext()) check(await db.MaritimeEconomy.CountAsync() == 1, "A single durable aggregate row is written; no unrelated Amia table is used.");
        using (var pg = new SailingPersistenceDbContext(new DbContextOptionsBuilder<SailingPersistenceDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused").Options))
        {
            string generated = pg.Database.GenerateCreateScript();
            check(generated.Contains("CREATE TABLE saved_maritime_economy (") && generated.Contains("\"PK_saved_maritime_economy\" PRIMARY KEY (id)"),
                "Actual Npgsql provider confirms the additive economy table and primary key offline, without connecting or executing SQL.");
        }
        using var corrupt = new Fixture(); System.IO.Directory.CreateDirectory(Path.Combine(corrupt.Directory, "economy"));
        string path = Path.Combine(corrupt.Directory, "economy", "maritime-v1.json"); await File.WriteAllTextAsync(path, "{broken"); await corrupt.Initialize();
        check(!corrupt.Ledger.Ready && await File.ReadAllTextAsync(path) == "{broken", "Invalid economic persistence disables transactions and preserves every original byte.");
        using var absent = new Fixture(); await absent.Initialize();
        check(!ShipRentalService.Available(absent.Ship, "missing") && new IslandService().GetIslands().Single(i => i.Id == "driftwood_isle").ShipyardTag == null,
            "Invalid rental ports fail eligibility; Driftwood shipyard resources are explicitly absent from current configuration.");
    }
    static async Task CrewChecks(Action<bool, string> check)
    {
        var ship = new ShipState { ShipId = "A", ShipName = "A", DeckAreaResRef = "deck", AreaResRef = "ocean_01",
            HelmsmanPCKey = "captain", Underway = true, IsNavigating = true };
        Dictionary<string, HashSet<string>> aboard = new() { ["A"] = ["captain", "passenger", "crew", "other-passenger"] };
        check(!ShipCrewLifecycle.ReleaseOwnHelm(ship, "passenger") && ship.HelmsmanPCKey == "captain" && ship.Underway,
            "Passenger/crew departure cannot release another player's helm or stop ship navigation.");
        bool exit = await ShipTransferTransaction.ExecuteAsync([(Task.CompletedTask.AsFunc(), Task.CompletedTask.AsFunc(), () => { })],
            () => ShipCrewLifecycle.TransferMembership(aboard, "A", null, "passenger"));
        check(exit && aboard["A"].SetEquals(["captain", "crew", "other-passenger"]) && ship.HelmsmanPCKey == "captain" && ship.Underway && ship.IsNavigating,
            "Only the departing passenger's membership changes after successful transfer; captain, other crew and route remain intact.");
        bool failed = await ShipTransferTransaction.ExecuteAsync([( () => Task.FromException(new TimeoutException()), Task.CompletedTask.AsFunc(), () => { })],
            () => { ShipCrewLifecycle.TransferMembership(aboard, "A", null, "crew"); ShipCrewLifecycle.ReleaseOwnHelm(ship, "crew"); });
        check(!failed && aboard["A"].Contains("crew") && ship.HelmsmanPCKey == "captain", "Failed exit preserves membership and cannot clear another player's helm.");
        check(ShipCrewLifecycle.ReleaseOwnHelm(ship, "captain") && aboard["A"].Contains("captain"), "LEAVE HELM releases only ownership and keeps the captain physically aboard.");
        ship.HelmsmanPCKey = "captain";
        await ShipTransferTransaction.ExecuteAsync([(Task.CompletedTask.AsFunc(), Task.CompletedTask.AsFunc(), () => { })],
            () => { ShipCrewLifecycle.TransferMembership(aboard, "A", null, "captain"); ShipCrewLifecycle.ReleaseOwnHelm(ship, "captain"); });
        check(ship.HelmsmanPCKey == null && !aboard["A"].Contains("captain") && aboard["A"].Contains("crew"), "Captain disembark releases their own helm, preserving remaining crew.");
        ShipCrewLifecycle.TransferMembership(aboard, "A", "B", "crew"); ShipCrewLifecycle.TransferMembership(aboard, "B", null, "crew");
        check(!aboard["A"].Contains("crew") && !aboard.ContainsKey("B"), "Boarding transfer followed by disembarking leaves no stale membership on either ship.");
        check(ShipCrewLifecycle.MatchesExit("A", "A", "deck", "deck", "deck", "cabin") &&
            !ShipCrewLifecycle.MatchesExit("B", "A", "deck", "deck", "deck", "cabin") &&
            !ShipCrewLifecycle.MatchesExit("A", "A", "deck", "unrelated", "deck", "cabin"), "Exit context checks membership, current deck/cabin and clicked object's area instead of selecting the first duplicated tag.");
        var combat = new ShipTargetSelection(); var sailing = new ShipTargetSelection();
        var target = new ShipState { ShipId = "B", ShipName = "B", DeckAreaResRef = "other", AreaResRef = "ocean_01" };
        combat.Refresh("captain", ship, [target]); combat.Select("captain", ship, 0, [target]);
        combat.Refresh("passenger", ship, [target]); combat.Select("passenger", ship, 0, [target]);
        sailing.Refresh("captain", ship, [target]); sailing.Select("captain", ship, 0, [target]);
        check(combat.ClearForSource("passenger", "A") && combat.GetSelected("captain", ship, [target]) == target && sailing.GetSelected("captain", ship, [target]) == target,
            "Passenger window/selection cleanup preserves other players and independently owned Sailing/Combat selections.");
        check(!combat.ClearForSource("captain", "stale-ship") && combat.GetSelected("captain", ship, [target]) == target,
            "A stale departure callback cannot clear the captain's selection for a different ship.");
        var reservations = new ShipTransferReservations(); using var first = reservations.TryReserve(["passenger"]);
        using var second = reservations.TryReserve(["other-passenger"]);
        check(first != null && second != null && reservations.TryReserve(["passenger"]) == null,
            "Independent passenger transfers can proceed while duplicate exit clicks cannot queue another jump.");
    }
    static Func<Task> AsFunc(this Task task) => () => task;
}
#endif
