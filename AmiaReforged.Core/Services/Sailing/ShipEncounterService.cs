using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipEncounterService))]
public class ShipEncounterService
{
    public const float EncounterDistance = 35f;
    private readonly Dictionary<(string, string), ShipEncounter> _activeEncounters = new();
    private readonly Dictionary<ShipState, HashSet<ShipState>> _neighbors = new();
    private readonly HashSet<ShipState> _current = new();
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public event Action<ShipEncounter>? EncounterStarted;
    public event Action<ShipEncounter>? EncounterEnded;

    public void CheckEncounters(IReadOnlyCollection<ShipState> ships)
    {
        ShipState[] active = ships.Where(ShipActionRules.IsActive).ToArray();
        _current.Clear();
        _current.UnionWith(active);
        foreach (ShipEncounter encounter in _activeEncounters.Values.ToArray())
            if (!_current.Contains(encounter.ShipA) || !_current.Contains(encounter.ShipB) ||
                !IsEligiblePair(encounter.ShipA, encounter.ShipB)) Remove(encounter);

        for (int i = 0; i < active.Length; i++)
        for (int j = i + 1; j < active.Length; j++)
        {
            ShipState a = active[i], b = active[j];
            if (!IsEligiblePair(a, b)) continue;
            var key = Key(a, b);
            if (_activeEncounters.TryGetValue(key, out ShipEncounter? existing))
            {
                if (!ReferenceEquals(existing.ShipA, a) && !ReferenceEquals(existing.ShipB, a))
                {
                    Remove(existing);
                }
                else
                {
                    existing.Distance = ShipActionRules.Distance(a, b);
                    existing.AreaResRef = a.AreaResRef;
                    continue;
                }
            }
            ShipEncounter encounter = new() { ShipA = a, ShipB = b, AreaResRef = a.AreaResRef,
                Distance = ShipActionRules.Distance(a, b) };
            _activeEncounters[key] = encounter;
            AddNeighbor(a, b);
            AddNeighbor(b, a);
            Notify(EncounterStarted, encounter);
        }
    }

    private static bool IsEligiblePair(ShipState a, ShipState b) =>
        ShipActionRules.CanInteract(a, b) && ShipActionRules.Distance(a, b) <= EncounterDistance;

    public bool IsInEncounter(ShipState a, ShipState b) => TryGetEncounter(a, b, out _);
    public IReadOnlyCollection<ShipEncounter> GetActiveEncounters() => _activeEncounters.Values
        .Where(e => _current.Contains(e.ShipA) && _current.Contains(e.ShipB) && IsEligiblePair(e.ShipA, e.ShipB)).ToArray();

    public IEnumerable<ShipState> GetNearbyShips(ShipState source) =>
        _neighbors.TryGetValue(source, out HashSet<ShipState>? neighbors) ?
        neighbors.Where(target => _current.Contains(source) && _current.Contains(target) &&
            IsEligiblePair(source, target)).OrderBy(s => s.ShipId, StringComparer.Ordinal).ToArray() : [];

    public bool TryGetEncounter(ShipState a, ShipState b, out ShipEncounter? encounter)
    {
        if (_current.Contains(a) && _current.Contains(b) && IsEligiblePair(a, b) &&
            _activeEncounters.TryGetValue(Key(a, b), out encounter) &&
            ((ReferenceEquals(encounter.ShipA, a) && ReferenceEquals(encounter.ShipB, b)) ||
             (ReferenceEquals(encounter.ShipA, b) && ReferenceEquals(encounter.ShipB, a)))) return true;
        encounter = null;
        return false;
    }

    // Automatic discovery/proximity queries only. Player actions supply their own target.
    public bool TryGetEncounter(ShipState ship, out ShipEncounter? encounter)
    {
        ShipState? nearest = GetNearbyShips(ship).OrderBy(s => ShipActionRules.Distance(ship, s)).FirstOrDefault();
        encounter = null;
        return nearest != null && TryGetEncounter(ship, nearest, out encounter);
    }

    public bool TryGetTarget(ShipState ship, out ShipState? target, out ShipEncounter? encounter)
    {
        if (TryGetEncounter(ship, out encounter) && encounter != null)
        {
            target = ReferenceEquals(encounter.ShipA, ship) ? encounter.ShipB : encounter.ShipA;
            return true;
        }
        target = null;
        return false;
    }

    private void AddNeighbor(ShipState a, ShipState b)
    {
        if (!_neighbors.TryGetValue(a, out HashSet<ShipState>? set)) _neighbors[a] = set = new();
        set.Add(b);
    }

    private void Remove(ShipEncounter encounter)
    {
        _activeEncounters.Remove(Key(encounter.ShipA, encounter.ShipB));
        foreach (var pair in new[] { (encounter.ShipA, encounter.ShipB), (encounter.ShipB, encounter.ShipA) })
            if (_neighbors.TryGetValue(pair.Item1, out var set))
            {
                set.Remove(pair.Item2);
                if (set.Count == 0) _neighbors.Remove(pair.Item1);
            }
        Notify(EncounterEnded, encounter);
    }

    private static (string, string) Key(ShipState a, ShipState b) =>
        string.CompareOrdinal(a.ShipId, b.ShipId) < 0 ? (a.ShipId, b.ShipId) : (b.ShipId, a.ShipId);

    private static void Notify(Action<ShipEncounter>? callbacks, ShipEncounter encounter)
    {
        if (callbacks == null) return;
        foreach (Action<ShipEncounter> callback in callbacks.GetInvocationList())
            try { callback(encounter); } catch (Exception ex) { Log.Error(ex, "Encounter subscriber failed."); }
    }
}
