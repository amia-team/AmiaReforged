using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Canonical runtime instances, keyed independently of mutable display names.</summary>
[ServiceBinding(typeof(ShipRegistryService))]
public sealed class ShipRegistryService
{
    private readonly Dictionary<string, ShipState> _ships = new(StringComparer.Ordinal);

    public ShipState? GetShip(string identity) => _ships.GetValueOrDefault(identity) ??
        _ships.Values.FirstOrDefault(s => string.Equals(s.ShipName, identity, StringComparison.Ordinal));

    public IReadOnlyCollection<ShipState> GetShips() => _ships.Values.ToArray();

    public bool IsCurrent(ShipState? ship) => ship != null &&
        _ships.TryGetValue(ship.ShipId, out ShipState? current) && ReferenceEquals(current, ship);

    public void Register(ShipState ship)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ship.ShipId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ship.ShipName);
        ShipState? existing = GetShip(ship.ShipId) ?? GetShip(ship.ShipName);
        if (existing != null && !ReferenceEquals(existing, ship))
            throw new InvalidOperationException($"Ship identity already registered: {ship.ShipId}");
        if (_ships.Values.Any(s => !ReferenceEquals(s, ship) && (CutterRentalFleet.IsCutter(ship) || CutterRentalFleet.IsCutter(s)) &&
            new[] { s.DeckAreaResRef, s.CabinAreaResRef }.Where(a => !string.IsNullOrWhiteSpace(a)).Intersect(
                new[] { ship.DeckAreaResRef, ship.CabinAreaResRef }.Where(a => !string.IsNullOrWhiteSpace(a)), StringComparer.OrdinalIgnoreCase).Any()))
            throw new InvalidOperationException("A rental cutter cannot share an interior with another ship.");
        _ships[ship.ShipId] = ship;
    }

    public bool Remove(ShipState ship) => IsCurrent(ship) && _ships.Remove(ship.ShipId);
}
