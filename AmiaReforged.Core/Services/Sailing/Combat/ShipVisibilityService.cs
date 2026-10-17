using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipVisibilityService))]
public sealed class ShipVisibilityService
{
    private const float NearbyRange = 40f;
    private const float HorizonRange = 70f;

    public IEnumerable<VisibleShipContact> GetVisibleShips(
        ShipState viewer,
        IReadOnlyCollection<ShipState> ships)
    {
        if (!ShipActionRules.IsActive(viewer)) yield break;
        foreach (ShipState ship in ships)
        {
            if (!ShipActionRules.IsActive(ship) || ReferenceEquals(ship, viewer))
            {
                continue;
            }

            if (!string.Equals(ship.AreaResRef, viewer.AreaResRef, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            float dx = ship.X - viewer.X;
            float dy = ship.Y - viewer.Y;
            float distance = MathF.Sqrt(dx * dx + dy * dy);

            if (distance > HorizonRange)
            {
                continue;
            }

            yield return new VisibleShipContact
            {
                Ship = ship,
                Distance = distance,
                IsHorizonContact = distance > NearbyRange
            };
        }
    }
}