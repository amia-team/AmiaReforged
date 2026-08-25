using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(MerchantTradeRouteService))]
public sealed class MerchantTradeRouteService
{
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private readonly ShipNavigationService
        _shipNavigationService;

    public MerchantTradeRouteService(
        ShipNavigationService shipNavigationService)
    {
        _shipNavigationService =
            shipNavigationService;
    }

  public void AssignDriftwoodSouthportRoute(
    ShipState ship)
{
    ShipNavigationRoute route =
        new()
        {
            ShipName = ship.ShipName,
            Loop = true
        };

    route.Waypoints.Add(
        new ShipNavigationWaypoint
        {
            AreaResRef = "ocean_01",
            X = 120f,
            Y = 90f,
            Z = 0f,
            Description = "Driftwood Isle"
        });

    route.Waypoints.Add(
        new ShipNavigationWaypoint
        {
            AreaResRef = "ocean_01",
            X = 90f,
            Y = 100f,
            Z = 0f,
            Description = "Southport"
        });

    // -------------------------------------------------------------
    // Activate navigation using the first waypoint.
    // -------------------------------------------------------------

    ShipNavigationWaypoint firstWaypoint =
        route.Waypoints[0];

    _shipNavigationService.SetDestination(
        ship,
        firstWaypoint.AreaResRef,
        firstWaypoint.X,
        firstWaypoint.Y,
        firstWaypoint.Z);

    // -------------------------------------------------------------
    // Install the route.
    // -------------------------------------------------------------

    _shipNavigationService.SetRoute(
        ship,
        route);

    Log.Info(
        $"Merchant trade route assigned: " +
        $"Ship={ship.ShipName}, " +
        $"Route=Driftwood Isle <-> Southport");
}
}