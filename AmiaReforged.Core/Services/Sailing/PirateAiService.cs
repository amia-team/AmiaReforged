using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(PirateAiService))]
public sealed class PirateAiService
{
    private const float DetectionRange = 80.0f;

    private readonly ShipNavigationService _shipNavigationService;

    public PirateAiService(
        ShipNavigationService shipNavigationService)
    {
        _shipNavigationService =
            shipNavigationService;
    }

    public void UpdatePirates(
        IReadOnlyCollection<ShipState> ships)
    {
        foreach (ShipState pirate
        in ships.Where(s => s.ShipType == ShipType.Pirate))
        {
            ShipState? target =
                FindNearestPlayerShip(
                    pirate,
                    ships);

            if (target == null)
            {
                continue;
            }

            _shipNavigationService.SetDestination(
                pirate,
                target.AreaResRef,
                target.X,
                target.Y,
                target.Z);
        }
    }

    private static ShipState? FindNearestPlayerShip(
        ShipState pirate,
        IReadOnlyCollection<ShipState> ships)
    {
        ShipState? nearest = null;
        float bestDistance = DetectionRange;

        foreach (ShipState ship in ships)
        {
            if (ReferenceEquals(ship, pirate))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(ship.HelmsmanPCKey))
            {
                continue;
            }

            if (!string.Equals(
                    ship.AreaResRef,
                    pirate.AreaResRef,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            float dx = ship.X - pirate.X;
            float dy = ship.Y - pirate.Y;

            float distance =
                MathF.Sqrt(dx * dx + dy * dy);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = ship;
            }
        }

        return nearest;
    }
}