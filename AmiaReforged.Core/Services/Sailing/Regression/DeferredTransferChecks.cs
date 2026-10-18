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
        public float Distance;
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
            bool sameAreaMove = Area == destination;
            if (rollback) Cancel();
            Jump(destination);
            await ShipTransferTransaction.AwaitArrivalAsync(NextFrame,
                () => ShipTransferTransaction.ArrivalConfirmed(Area == destination, sameAreaMove, Distance),
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
            check(reports == 1, "The triggering failure is logged before awaiting native rollback, preserving return-to-deck diagnostics.");
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
        await MultiplayerChecks(check);
    }

    private static async Task MultiplayerChecks(Action<bool, string> check)
    {
        var now = DateTime.UtcNow;
        var islands = new IslandService(); var areas = new SailingAreaService();
        var ship = new ShipState { ShipId = "party-ship", ShipName = "party-ship", DeckAreaResRef = "deck",
            AreaResRef = "ocean_01", X = 120, Y = 90 };
        var landfall = ShipDisembarkRules.Resolve(ship, islands.GetIslands(), areas.ContainsArea);
        check(landfall is { PortId: "driftwood_isle", AreaResRef: "sea_islet1", X: 24, Y: 207, Z: 0 },
            "Actual Driftwood configuration resolves its landfall independently of ship deck and boarding requests.");
        check(!ShipTransferTransaction.ArrivalConfirmed(false, false, 0) &&
            !ShipTransferTransaction.ArrivalConfirmed(true, true, 4) &&
            !ShipTransferTransaction.ArrivalConfirmed(true, false, float.NaN) &&
            ShipTransferTransaction.ArrivalConfirmed(true, false, 5),
            "Cross-area arrival permits safe-jump displacement; wrong areas, invalid positions and unexecuted same-area moves remain rejected.");

        for (int size = 1; size <= 4; size++)
        for (int order = 0; order < size; order++)
        {
            string[] names = Enumerable.Range(0, size).Select(i => $"player-{i}").ToArray();
            string[] followers = names.Skip(1).OrderBy(n => (Array.IndexOf(names, n) + order) % size).ToArray();
            var consent = new PartyTravelConsent(names[0], followers.Select(n => new PartyTravelMember(n, n)).ToArray(), now.AddMinutes(1));
            foreach (string name in followers)
            {
                check(consent.Answer(name, true, now) && !consent.Answer(name, true, now), "Duplicate acceptance cannot add a participant twice.");
            }
            check(consent.TryStart(true, now, _ => true, out var selected) && selected.Length == size &&
                !consent.TryStart(true, now, _ => true, out _), $"{size}-player confirmation starts once, ordering={order}.");
            check(!consent.CloseConfirmation() && consent.IsOpen(now), "Native/reentrant window close after confirmation cannot cancel travel.");
            var engines = selected.ToDictionary(n => n, _ => new Engine("shore"));
            // Simulated safe placement outside the old three-metre threshold.
            if (size >= 3) engines[selected[2]].Distance = 5;
            Dictionary<string, HashSet<string>> membership = new();
            var reservations = new ShipTransferReservations(); int commits = 0;
            using (var held = reservations.TryReserve(selected))
            {
                check(held != null, "Every selected participant is reserved before the first jump.");
                var moves = selected.Select(name => (
                    (Func<Task>)(() => engines[name].Move("deck", () => { if (!consent.IsOpen(now)) throw new OperationCanceledException(); })),
                    (Func<Task>)(() => engines[name].Move("shore", rollback: true)),
                    (Action)(() => engines[name].Require("deck")))).ToArray();
                Task<bool> boarding = ShipTransferTransaction.ExecuteAsync(moves, () =>
                {
                    foreach (string name in selected) ShipCrewLifecycle.TransferMembership(membership, null, ship.ShipName, name);
                    commits++;
                });
                foreach (string name in selected)
                {
                    check(reservations.TryReserve([name]) == null, "Repeated activation/reentrant transfer cannot reserve an in-flight player.");
                    check(commits == 0 && membership.Count == 0, "Individual arrivals do not publish partial group membership.");
                    engines[name].Advance(false);
                    check(!boarding.IsCompleted && engines[name].Jumps == 1, "Delayed frames never requeue a forward transition.");
                    check(!consent.CloseConfirmation(), "Window close between individual arrivals is harmless.");
                    engines[name].Advance();
                }
                check(await boarding && commits == 1 && membership[ship.ShipName].SetEquals(selected),
                    $"{size}-player boarding commits correct membership exactly once after all arrivals, ordering={order}.");
            }
            foreach (string name in selected)
            {
                using (var exitReservation = reservations.TryReserve([name]))
                {
                    check(exitReservation != null, "Completed boarding releases reservations for immediate disembark.");
                    Task<bool> exit = ShipTransferTransaction.ExecuteAsync([
                        (() => engines[name].Move(landfall!.AreaResRef), () => engines[name].Move("deck", rollback: true),
                            () => engines[name].Require(landfall!.AreaResRef))],
                        () => ShipCrewLifecycle.TransferMembership(membership, ship.ShipName, null, name));
                    check(reservations.TryReserve([name]) == null, "Repeated exit activation cannot overlap disembark.");
                    engines[name].Advance();
                    check(await exit && engines[name].Area == "sea_islet1" && engines[name].Jumps == 2 && engines[name].Cancellations == 0,
                        "Driftwood arrival immediately after party boarding has no rollback or return-to-deck jump.");
                }
            }
            check(membership.Count == 0, "Successful disembarks remove only arrived players and leave no stale ship membership.");
        }

        var partial = new PartyTravelConsent("leader", [new("yes", "yes"), new("no", "no")], now.AddMinutes(1));
        partial.Answer("yes", true, now);
        check(!partial.TryStart(true, now, _ => true, out _), "Partial consent waits for pending participant answers.");
        partial.Answer("no", false, now);
        check(partial.TryStart(true, now, _ => true, out var subset) && subset.SequenceEqual(["leader", "yes"]),
            "Rejected participants stay behind without invalidating accepted travel.");
        var closed = new PartyTravelConsent("leader", [], now.AddMinutes(1));
        check(closed.CloseConfirmation() && !closed.TryStart(false, now, _ => true, out _), "Closing an unconfirmed window cancels without movement.");
        VerifyNativeAdapter(check);
        var offlineBefore = new PartyTravelConsent("leader", [new("offline", "offline")], now.AddMinutes(1));
        offlineBefore.Answer("offline", true, now);
        check(!offlineBefore.TryStart(true, now, n => n != "offline", out _), "A disconnected accepted participant cannot start boarding.");

        for (int disconnected = 0; disconnected < 4; disconnected++)
        {
            Engine[] engines = Enumerable.Range(0, 4).Select(_ => new Engine("shore")).ToArray();
            bool[] online = [true, true, true, true]; int commits = 0, errors = 0;
            var moves = Enumerable.Range(0, 4).Select(index => (
                (Func<Task>)(() => engines[index].Move("deck", () => { if (online.Any(value => !value)) throw new OperationCanceledException(); })),
                (Func<Task>)(() => online[index] ? engines[index].Move("shore", rollback: true) : Task.FromException(new OperationCanceledException())),
                (Action)(() => engines[index].Require("deck")))).ToArray();
            Task<bool> transfer = ShipTransferTransaction.ExecuteAsync(moves, () => commits++, _ => errors++);
            for (int index = 0; index < disconnected; index++) engines[index].Advance();
            online[disconnected] = false;
            for (int frame = 0; frame < 16 && !transfer.IsCompleted; frame++) foreach (var engine in engines) engine.Advance();
            check(transfer.IsCompleted && !await transfer && commits == 0 && errors == 2,
                $"Disconnect in participant position {disconnected} aborts commit and reports failed native rollback independently.");
            check(engines.Take(disconnected).All(e => e.Area == "shore" && e.Cancellations == 1) &&
                engines.Skip(disconnected + 1).All(e => e.Jumps == 0),
                "Disconnect restores earlier attempted players and never moves unattempted participants.");
        }
        Console.WriteLine("PASS: multiplayer boarding/disembark transfer simulations (1-4 players, ordering, consent, disconnect, reentry, Driftwood).");
    }

    private static void VerifyNativeAdapter(Action<bool, string> check,
        [System.Runtime.CompilerServices.CallerFilePath] string file = "")
    {
        string root = Path.GetDirectoryName(Path.GetDirectoryName(file))!;
        string party = File.ReadAllText(Path.Combine(root, "Boarding", "ShipPartyTransitionService.cs"));
        string start = party[party.IndexOf("s.Selected = selected;", StringComparison.Ordinal)..
            party.IndexOf("bool success = await ExecuteAsync(s);", StringComparison.Ordinal)];
        check(start.Contains("CloseView(pair.Key)") && start.Contains(".ToArray()"),
            "Native adapter retires a snapshot of confirmation windows before initiating any individual move.");
        check(party.Contains("if (!s.Consent.CloseConfirmation()) return;") && party.Contains("if (s.Finished) return;"),
            "Native close dispatch uses the tested consent rule, and duplicate completion cannot repeat lifecycle notifications.");
        string physical = File.ReadAllText(Path.Combine(root, "Fleet", "PhysicalShipService.cs"));
        check(physical.Contains("ShipTransferTransaction.ArrivalConfirmed(creature.Area == destination.Area,") &&
            physical.Contains("entry.Previous.Area == destination.Area") && physical.Contains("HasArrived(creature, destination, sameAreaMove)"),
            "Native polling and final participant validation use the tested cross-area/same-area arrival policy.");
        check(physical.Contains("if (IsTransferPending(name)) continue;") &&
            physical.Contains("if (!player.IsValid || IsTransferPending(player.PlayerName) ||"),
            "Native cleanup and reconnect membership recovery continue to skip reserved participants.");
    }
}
#endif
