using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(MerchantPortService))]
public sealed class MerchantPortService
{
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private const int DefaultPortStaySeconds = 30;

    private readonly MerchantTradeService
        _merchantTradeService;

    public MerchantPortService(
        MerchantTradeService merchantTradeService)
    {
        _merchantTradeService =
            merchantTradeService;
    }

    public bool BeginPortStay(
        ShipState ship,
        ShipNavigationWaypoint waypoint)
    {
        if (ship.ShipType != ShipType.Merchant)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                waypoint.PortId))
        {
            return false;
        }

        if (ship.IsInPort)
        {
            return true;
        }

        ship.IsInPort = true;
ship.CurrentTradePortId =
    waypoint.PortId;
        ship.PortDepartureTime =
            DateTime.UtcNow.AddSeconds(
                DefaultPortStaySeconds);

        // ---------------------------------------------------------
        // Execute the merchant's trade once when entering port.
        // ---------------------------------------------------------

        _merchantTradeService.ExecuteTrade(
            ship,
            waypoint.PortId);

        Log.Info(
            $"Merchant '{ship.ShipName}' docked at " +
            $"{waypoint.PortId}. " +
            $"Departure={ship.PortDepartureTime:O}");

        return true;
    }

    public bool UpdatePortStay(
        ShipState ship)
    {
        if (!ship.IsInPort)
        {
            return false;
        }

        if (DateTime.UtcNow <
            ship.PortDepartureTime)
        {
            return true;
        }

        ship.IsInPort = false;

ship.CurrentTradePortId = null;

        Log.Info(
            $"Merchant '{ship.ShipName}' departing port.");

        return false;
    }
}