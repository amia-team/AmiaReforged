using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Native operations stay behind this adapter; production and regression checks share the entire workflow.</summary>
public interface IHostileBoardingWorld
{
    Task MainThread();
    HostileBoardingFacts Inspect(string actor, ShipState source, ShipState? target, bool transferring = false);
    BoardingPoint? Deck(ShipState ship);
    BoardingPoint? Emergency();
    bool PrepareDefenders(string actor, ShipState target, out string encounter);
    BoardingDefenderState Defenders(string encounter);
    void CleanupDefenders(string encounter);
    Task<bool> Transfer(string actor, string? fromShip, string? toShip, BoardingPoint point, Func<bool> validate, bool recovery = false);
    bool OnShip(string actor, ShipState ship);
    bool CanRecover(string actor, HostileBoardingVisit visit);
    bool SafelyOutsideVisit(string actor, HostileBoardingVisit visit);
    bool AtPoint(string actor, BoardingPoint point);
    bool Checkpoint(string actor);
    BoardingPoint? ReturnDestination(string actor, HostileBoardingVisit visit, out string? ship);
}

public sealed class HostileBoardingEngine(ShipRegistryService registry, HostileBoardingOptions options,
    IHostileBoardingWorld world, IHostileBoardingStore store)
{
    private sealed class Session(ShipState target, string defenders)
    {
        public ShipState Target { get; } = target;
        public string Defenders { get; } = defenders;
        public HashSet<string> Boarders { get; } = new(StringComparer.Ordinal);
        public bool Resolved { get; set; }
        public bool Closed { get; set; }
    }
    private readonly Dictionary<string, Session> _sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HostileBoardingVisit> _visits = new(StringComparer.Ordinal);
    private readonly HashSet<string> _operations = new(StringComparer.Ordinal);
    public bool HasVisit(string actor) => _visits.ContainsKey(actor);
    public bool IsResolved(string actor) => _visits.TryGetValue(actor, out var visit) &&
        _sessions.TryGetValue(visit.TargetId, out var session) && session.Resolved;
    public bool IsShipEngaged(ShipState ship) => _sessions.TryGetValue(ship.ShipId, out var s) &&
        ReferenceEquals(s.Target, ship) && (s.Boarders.Count > 0 || s.Resolved);

    public HostileBoardingEligibility Check(string actor, ShipState source, ShipState? target)
    {
        if (_operations.Contains(actor) || _visits.ContainsKey(actor)) return HostileBoardingEligibility.Denied("Finish your existing boarding visit or transfer first");
        if (target != null && _sessions.TryGetValue(target.ShipId, out var session))
        {
            if (!ReferenceEquals(session.Target, target) || session.Closed || session.Resolved || world.Defenders(session.Defenders) != BoardingDefenderState.Fighting)
                return HostileBoardingEligibility.Denied("This deck encounter has ended");
            if (session.Boarders.Count >= options.MaximumBoarders) return HostileBoardingEligibility.Denied("The boarding party is full");
        }
        return HostileBoardingRules.Check(source, target, world.Inspect(actor, source, target), options);
    }

    public async Task<HostileBoardingEligibility> BoardAsync(string actor, ShipState source, ShipState? target)
    {
        var eligibility = Check(actor, source, target);
        if (!eligibility.Allowed || target == null) return eligibility;
        if (!_operations.Add(actor)) return HostileBoardingEligibility.Denied("Boarding already pending");
        Session? session = null;
        bool arrived = false, journaled = false;
        try
        {
            HostileBoardingVisit? previousVisit = await store.Load(actor);
            await world.MainThread();
            if (previousVisit != null)
            {
                _visits[actor] = previousVisit;
                return HostileBoardingEligibility.Denied("Recover your previous boarding visit first");
            }
            eligibility = HostileBoardingRules.Check(source, target, world.Inspect(actor, source, target), options);
            if (!eligibility.Allowed) return eligibility;
            BoardingPoint? origin = world.Deck(source), destination = world.Deck(target), emergency = world.Emergency();
            if (origin == null || destination == null || emergency == null) return HostileBoardingEligibility.Denied("Boarding return plan is unavailable");
            if (!_sessions.TryGetValue(target.ShipId, out session))
            {
                if (!world.PrepareDefenders(actor, target, out string defenders)) return HostileBoardingEligibility.Denied("Hostile defenders could not be prepared; boarding is gated");
                _sessions[target.ShipId] = session = new(target, defenders);
            }
            if (!ReferenceEquals(session.Target, target) || session.Closed || session.Resolved || session.Boarders.Count >= options.MaximumBoarders ||
                world.Defenders(session.Defenders) != BoardingDefenderState.Fighting)
                return HostileBoardingEligibility.Denied("Deck encounter is unavailable or full");
            session.Boarders.Add(actor);
            HostileBoardingVisit visit = new(actor, source.ShipId, source.Rental?.RentalId, target.ShipId, origin, destination, emergency,
                source.CabinAreaResRef, target.CabinAreaResRef);
            await store.Save(visit); // Durability precedes the first queued native jump.
            journaled = true;
            await world.MainThread();
            _visits[actor] = visit;
            bool Validate() => registry.IsCurrent(source) && registry.IsCurrent(target) &&
                HostileBoardingRules.Check(source, target, world.Inspect(actor, source, target, true), options).Allowed &&
                world.Defenders(session.Defenders) == BoardingDefenderState.Fighting && world.Deck(target) == destination;
            arrived = await world.Transfer(actor, source.ShipName, target.ShipName, destination, Validate);
            await world.MainThread();
            if (!arrived) return HostileBoardingEligibility.Denied("Boarding transfer failed; original membership was not committed");
            world.Checkpoint(actor); // A failed export leaves the durable recovery journal in place.
            return new(true, "Hostile deck reached. Defeat the defenders, or use RETREAT at any time. Cargo looting is unavailable.");
        }
        finally
        {
            try
            {
                await world.MainThread();
                if (!arrived && session != null)
                {
                    if (!journaled || world.OnShip(actor, source))
                    {
                        if (journaled) { await store.Remove(actor); await world.MainThread(); }
                        _visits.Remove(actor);
                        session.Boarders.Remove(actor);
                    }
                    if (session.Boarders.Count == 0 && !session.Resolved)
                    { world.CleanupDefenders(session.Defenders); _sessions.Remove(session.Target.ShipId); }
                }
            }
            finally { _operations.Remove(actor); }
        }
    }

    // Retreat is independent of encounter range and origin movement. Unusable origins go to saved landfall.
    public async Task<bool> ReturnAsync(string actor)
    {
        if (!_operations.Add(actor)) return false;
        try
        {
            HostileBoardingVisit? visit = _visits.GetValueOrDefault(actor) ?? await store.Load(actor);
            await world.MainThread();
            if (visit == null) return false;
            _visits[actor] = visit;
            if (!world.CanRecover(actor, visit))
            {
                // A respawn/DM rescue already moved the character away from the visit decks.
                if (!world.SafelyOutsideVisit(actor, visit) || !world.Checkpoint(actor)) return false;
            }
            else
            {
                BoardingPoint? destination = world.ReturnDestination(actor, visit, out string? ship);
                if (destination == null) return false; // Keep the journal and retry when resources return.
                ShipState? target = registry.GetShip(visit.TargetId);
                string? from = target != null && world.OnShip(actor, target) ? target.ShipName : null;
                if (!await world.Transfer(actor, from, ship, destination, () => (world.CanRecover(actor, visit) || world.AtPoint(actor, destination)) &&
                    world.ReturnDestination(actor, visit, out var currentShip) == destination && currentShip == ship, true)) return false;
                await world.MainThread();
                if (!world.Checkpoint(actor)) return false;
            }
            await store.Remove(actor);
            await world.MainThread();
            _visits.Remove(actor);
            if (_sessions.TryGetValue(visit.TargetId, out var session))
            {
                session.Boarders.Remove(actor);
                if (session.Boarders.Count == 0)
                {
                    session.Resolved |= world.Defenders(session.Defenders) == BoardingDefenderState.Defeated;
                    session.Closed = true;
                    world.CleanupDefenders(session.Defenders);
                }
            }
            return true;
        }
        finally { await world.MainThread(); _operations.Remove(actor); }
    }

    public bool RequiresRecovery(string actor)
    {
        if (!_visits.TryGetValue(actor, out var visit)) return false;
        return registry.GetShip(visit.OriginId) is not { ShipType: ShipType.Player, IsActiveWorld: true, IsDisabled: false } ||
            !registry.IsCurrent(registry.GetShip(visit.TargetId)) || registry.GetShip(visit.TargetId) is not { IsActiveWorld: true } ||
            !world.OnShip(actor, registry.GetShip(visit.TargetId)!) ||
            !_sessions.TryGetValue(visit.TargetId, out var s) || !registry.IsCurrent(s.Target) ||
            world.Defenders(s.Defenders) == BoardingDefenderState.Unavailable;
    }

    public void Reconcile()
    {
        foreach (Session session in _sessions.Values.ToArray())
        {
            if (world.Defenders(session.Defenders) == BoardingDefenderState.Defeated) session.Resolved = true;
            if (!registry.IsCurrent(session.Target) && session.Boarders.Count == 0)
            { world.CleanupDefenders(session.Defenders); _sessions.Remove(session.Target.ShipId); }
        }
    }

    public void DisposeDefenders()
    {
        foreach (Session session in _sessions.Values) world.CleanupDefenders(session.Defenders);
        _sessions.Clear(); // Journals are intentionally retained for restart recovery.
    }
}
