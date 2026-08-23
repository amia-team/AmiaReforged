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
        foreach (ShipState ship in ships)
        {
            if (ReferenceEquals(ship, viewer))
            {
                continue;
            }

            if (ship.AreaResRef != viewer.AreaResRef)
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