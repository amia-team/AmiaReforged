using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipStorageService))]
public sealed class ShipStorageService
{
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    public bool StoreShip(
        ShipState ship,
        string portId)
    {
        if (ship == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(portId))
        {
            return false;
        }

        if (ship.IsStored)
        {
            Log.Info(
                $"Ship '{ship.ShipName}' is already stored " +
                $"at '{ship.StoredPortId}'.");

            return false;
        }

        ship.IsStored = true;
        ship.StoredPortId = portId;

        ship.Underway = false;
        ship.IsNavigating = false;
        ship.DestinationAreaResRef = null;

        Log.Info(
            $"Ship '{ship.ShipName}' stored at port " +
            $"'{portId}'.");

        return true;
    }

    public bool RetrieveShip(
        ShipState ship,
        string portId)
    {
        if (ship == null)
        {
            return false;
        }

        if (!ship.IsStored)
        {
            Log.Info(
                $"Ship '{ship.ShipName}' is not currently stored.");

            return false;
        }

        if (!string.Equals(
                ship.StoredPortId,
                portId,
                StringComparison.OrdinalIgnoreCase))
        {
            Log.Info(
                $"Ship '{ship.ShipName}' is stored at " +
                $"'{ship.StoredPortId}', not '{portId}'.");

            return false;
        }

        ship.IsStored = false;
        ship.StoredPortId = null;

        Log.Info(
            $"Ship '{ship.ShipName}' retrieved from port " +
            $"'{portId}'.");

        return true;
    }

public IEnumerable<ShipState> GetStoredShips(
    IEnumerable<ShipState> ships,
    string portId)
{
    if (string.IsNullOrWhiteSpace(portId))
    {
        return Enumerable.Empty<ShipState>();
    }

    return ships.Where(
        ship =>
            ship.IsStored &&
            string.Equals(
                ship.StoredPortId,
                portId,
                StringComparison.OrdinalIgnoreCase));
}

public bool IsStored(
    ShipState ship)
{
    return ship.IsStored;
}

    public bool IsStoredAtPort(
        ShipState ship,
        string portId)
    {
        return ship.IsStored &&
               string.Equals(
                   ship.StoredPortId,
                   portId,
                   StringComparison.OrdinalIgnoreCase);
    }
}