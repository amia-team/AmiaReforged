#if SAILING_REGRESSION
using AmiaReforged.Core.Models.Sailing;
using AmiaReforged.Core.Services.Sailing;
using Microsoft.EntityFrameworkCore;

static class DeferredTransferChecks
{
    // Models the engine boundary, not a synchronous Location setter: queueing a
    // jump leaves Area unchanged until a later frame. Production uses these same
    // arrival, transaction and reservation implementations.
    private sealed class Engine(string area)
    {
        public string Area = area;
        public int Jumps;
        public int Frames;
        public int Cancellations;
        private string? _queued;
        private TaskCompletionSource<bool> _frame = new();
        public void Jump(string destination) { Jumps++; _queued = destination; }
        public void Cancel() { Cancellations++; _queued = null; }
        public Task NextFrame() { Frames++; return _frame.Task; }
        public void Advance(bool processJump = true)
        {
            if (processJump && _queued != null) { Area = _queued; _queued = null; }
            TaskCompletionSource<bool> completed = _frame;
            _frame = new();
            completed.SetResult(true);
        }
        public async Task Move(string destination, Action? validate = null, bool rollback = false, int maximumFrames = 300)
        {
            if (rollback) Cancel();
            Jump(destination);
            await ShipTransferTransaction.AwaitArrivalAsync(NextFrame, () => Area == destination,
                validate ?? (() => { }), maximumFrames);
        }
        public void Require(string destination)
        {
            if (Area != destination) throw new InvalidOperationException("Deferred arrival was not confirmed.");
        }
    }

    public static async Task Run(Action<bool, string> check)
    {
        ShipTransferReservations reservations = new();
        using (IDisposable? party = reservations.TryReserve(["captain", "crew"]))
        {
            check(party != null && reservations.Contains("captain") && reservations.Contains("crew"),
                "Entire transfer party is reserved before any native move.");
            check(reservations.TryReserve(["other", "crew"]) == null && !reservations.Contains("other"),
                "A competing party fails atomically without reserving its other player.");
            check(reservations.TryReserve(["captain"]) == null,
                "Duplicate retrieval/boarding clicks cannot initiate a second teleport.");
            party!.Dispose(); party.Dispose();
        }
        check(!reservations.Contains("captain") && reservations.TryReserve(["same", "same"]) == null,
            "Reservation release is idempotent and duplicate participants are rejected.");

        string directory = Path.Combine(Path.GetTempPath(), "amia-deferred-transfer-" + Guid.NewGuid().ToString("N"));
        var options = new ShipPersistenceOptions { Enabled = true, Backend = SailingPersistenceBackend.Files, DataDirectory = directory };
        var factory = SailingPersistenceContextFactory.ForOptions(new DbContextOptionsBuilder<SailingPersistenceDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused;Timeout=1").Options);
        try
        {
            using var persistence = new ShipStatePersistenceService(factory, options: options);
            var storage = new ShipStorageService();
            ShipState sea = new() { ShipId = "Sea Sprite", ShipName = "Sea Sprite", DeckAreaResRef = "sea_sprite_d2",
                AreaResRef = "ocean_01", IsStored = true, StoredPortId = "southport" };
            check(await persistence.TrySaveState(sea), "Stored pre-transfer Sea Sprite is persisted.");
            Engine engine = new("cordor_west");
            string? aboard = null;
            int commits = 0;
            sea.IsDocking = true;
            using (IDisposable? retrieval = reservations.TryReserve(["retriever"]))
            {
                Task<bool> transfer = ShipTransferTransaction.ExecuteAsync([
                    (() => engine.Move(sea.DeckAreaResRef), () => engine.Move("cordor_west", rollback: true),
                        () => engine.Require(sea.DeckAreaResRef))],
                    () => { if (!storage.RetrieveShip(sea, "southport")) throw new Exception("Storage commit rejected.");
                        sea.IsDocking = false; aboard = sea.ShipId; commits++; });
                check(!transfer.IsCompleted && engine.Area == "cordor_west" && engine.Jumps == 1 && commits == 0,
                    "A queued jump is pending, does not block its caller, and is not mistaken for failed arrival.");
                check(sea.IsStored && sea.StoredPortId == "southport" && aboard == null,
                    "Retrieval does not publish storage or membership before engine arrival.");
                check(!await persistence.TrySaveState(sea), "Checkpoints cannot persist a pending port transition.");
                SavedShipState previous = (await persistence.LoadState(sea.ShipId))!;
                var restored = new ShipState { ShipId = sea.ShipId, ShipName = sea.ShipName, DeckAreaResRef = sea.DeckAreaResRef, AreaResRef = "ocean_01" };
                check(ShipSnapshotCodec.TryRestore(restored, previous, out _), "Pre-transfer snapshot remains valid.");
                check(restored.IsStored && restored.StoredPortId == "southport", "An in-flight transfer leaves the durable stored state intact.");
                engine.Advance(processJump: false);
                check(!transfer.IsCompleted && engine.Jumps == 1 && commits == 0,
                    "A slow transition may take multiple engine frames without requeueing its teleport.");
                engine.Advance();
                check(await transfer && engine.Area == "sea_sprite_d2" && aboard == sea.ShipId && commits == 1 && !sea.IsStored,
                    "Deferred retrieval commits membership and storage exactly once after deck arrival.");
                check(await persistence.TrySaveState(sea), "Confirmed retrieval persists its active state.");
                check(ShipSnapshotCodec.TryRestore(restored, (await persistence.LoadState(sea.ShipId))!, out _), "Committed retrieval snapshot remains valid.");
                check(!restored.IsStored && restored.StoredPortId == null,
                    "Restart after deferred retrieval restores the active ship rather than its old stored default.");
            }
            check(!reservations.Contains("retriever"), "Successful transfer releases the player's reservation.");

            Engine failed = new("cordor_west");
            sea.IsStored = true; sea.StoredPortId = "southport";
            int reports = 0;
            Task<bool> timedOut = ShipTransferTransaction.ExecuteAsync([
                (() => failed.Move("sea_sprite_d2", maximumFrames: 2), () => failed.Move("cordor_west", rollback: true),
                    () => failed.Require("sea_sprite_d2"))], () => { sea.IsStored = false; commits++; }, _ => reports++);
            failed.Advance(false); failed.Advance(false);
            check(!timedOut.IsCompleted && failed.Cancellations == 1 && sea.IsStored && commits == 1,
                "Timed-out arrival starts awaited rollback and cancels its unexecuted original jump.");
            failed.Advance();
            check(!await timedOut && failed.Area == "cordor_west" && reports == 1 && sea.StoredPortId == "southport",
                "Failed retrieval preserves the stored state and confirms rollback before returning failure.");
            failed.Advance();
            check(failed.Area == "cordor_west" && failed.Jumps == 2,
                "An old queued deck jump cannot execute after completed rollback.");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        Engine captain = new("deck"), crew = new("deck");
        int partyCommits = 0;
        Task<bool> docking = ShipTransferTransaction.ExecuteAsync([
            (() => captain.Move("cordor_west"), () => captain.Move("deck", rollback: true), () => captain.Require("cordor_west")),
            (() => crew.Move("cordor_west"), () => crew.Move("deck", rollback: true), () => crew.Require("cordor_west"))],
            () => partyCommits++);
        captain.Advance();
        check(!docking.IsCompleted && captain.Area == "cordor_west" && crew.Area == "deck" && partyCommits == 0,
            "A partial crew arrival does not detach any crew or store the ship.");
        crew.Advance();
        check(await docking && partyCommits == 1, "The entire crew must arrive before docking commits once.");

        captain = new("deck"); crew = new("deck");
        List<string> rollbacks = new();
        docking = ShipTransferTransaction.ExecuteAsync([
            (() => captain.Move("port"), async () => { rollbacks.Add("captain"); await captain.Move("deck", rollback: true); }, () => captain.Require("port")),
            (() => crew.Move("port"), async () => { rollbacks.Add("crew"); await crew.Move("deck", rollback: true); }, () => crew.Require("port"))],
            () => partyCommits++);
        captain.Advance();
        captain.Area = "unrelated"; // Early arrival changed while the second player was pending.
        crew.Advance();
        check(!docking.IsCompleted && rollbacks.SequenceEqual(["crew"]),
            "All-party final validation catches an earlier participant leaving and starts reverse rollback.");
        crew.Advance(); captain.Advance();
        check(!await docking && captain.Area == "deck" && crew.Area == "deck" && partyCommits == 1 &&
                rollbacks.SequenceEqual(["crew", "captain"]),
            "A late party validation failure restores every attempted participant without publishing membership.");

        int unattemptedMoves = 0, unattemptedRollbacks = 0;
        bool result = await ShipTransferTransaction.ExecuteAsync([
            (() => Task.FromException(new InvalidOperationException("first move failed")), () => Task.CompletedTask, () => { }),
            (() => { unattemptedMoves++; return Task.CompletedTask; }, () => { unattemptedRollbacks++; return Task.CompletedTask; }, () => { })],
            () => partyCommits++);
        check(!result && unattemptedMoves == 0 && unattemptedRollbacks == 0 && partyCommits == 1,
            "An early failure cannot teleport uninvolved later participants during rollback.");

        Engine changedController = new("source");
        bool controlledCreatureUnchanged = true;
        Task<bool> pinning = ShipTransferTransaction.ExecuteAsync([
            (() => changedController.Move("target", () => { if (!controlledCreatureUnchanged) throw new OperationCanceledException(); }),
                () => changedController.Move("source", rollback: true), () => changedController.Require("target"))],
            () => partyCommits++);
        controlledCreatureUnchanged = false;
        changedController.Advance(); changedController.Advance();
        check(!await pinning && changedController.Area == "source" && partyCommits == 1,
            "A controlled-creature change before confirmation aborts the transaction without assigning the new creature.");

        Engine immediate = new("destination");
        Task arrival = ShipTransferTransaction.AwaitArrivalAsync(immediate.NextFrame, () => immediate.Area == "destination", () => { });
        check(!arrival.IsCompleted, "Even a same-area/immediate native change yields a frame before confirmation.");
        immediate.Advance(); await arrival;
        check(immediate.Frames == 1, "An immediate valid arrival completes after exactly one engine frame.");
    }
}
#endif
