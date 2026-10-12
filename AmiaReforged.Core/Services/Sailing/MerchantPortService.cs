using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(MerchantPortService))]
public sealed class MerchantPortService(MerchantTradeService trade, ShipNavigationService navigation, ShipStatePersistenceService persistence) : IDisposable
{
    private bool _disposed;
    private readonly Dictionary<string, string> _lastFailure = new();
    public void Dispose() => _disposed = true;
    private readonly HashSet<string> _pending = new();
    private readonly Dictionary<string, DateTime> _retry = new();
    public bool BeginPortStay(ShipState ship, ShipNavigationWaypoint waypoint)
    {
        if (ship.ShipType != ShipType.Merchant || string.IsNullOrWhiteSpace(waypoint.PortId)) return false;
        if (ship.IsInPort) return true;
        ship.IsInPort = true; ship.Underway = false; ship.CurrentTradePortId = waypoint.PortId;
        ship.PortDepartureTime = DateTime.UtcNow.AddSeconds(30);
        ship.PendingMerchantArrivalSequence = checked(ship.MerchantArrivalSequence + 1);
        StartTrade(ship);
        return true;
    }
    private void StartTrade(ShipState ship)
    {
        if (_disposed || !_pending.Add(ship.ShipId)) return;
        _ = CompleteTrade(ship);
    }
    private async Task CompleteTrade(ShipState ship)
    {
        try
        {
            var route = navigation.GetRoute(ship);
            string? next = route == null ? null : route.Waypoints.Skip(route.CurrentWaypointIndex + 1)
                .Concat(route.Loop ? route.Waypoints.Take(route.CurrentWaypointIndex + 1) : [])
                .FirstOrDefault(w => w.PortId != null)?.PortId;
            var result = await trade.ProcessArrivalAsync(ship, ship.CurrentTradePortId!, next, ship.PendingMerchantArrivalSequence);
            await NwTask.SwitchToMainThread();
            if (_disposed) return;
            if (result.Success) { _lastFailure.Remove(ship.ShipId); await persistence.TrySaveState(ship); }
            else if (!_lastFailure.TryGetValue(ship.ShipId, out var previous) || previous != result.Message)
            { _lastFailure[ship.ShipId] = result.Message; NLog.LogManager.GetCurrentClassLogger().Warn("Merchant arrival suspended: ship={0}, reason={1}.", ship.ShipId, result.Message); }
        }
        catch (Exception ex) { NLog.LogManager.GetCurrentClassLogger().Error(ex, "Merchant arrival failed; port stay retained."); }
        finally { await NwTask.SwitchToMainThread(); _pending.Remove(ship.ShipId); _retry[ship.ShipId] = DateTime.UtcNow.AddSeconds(5); }
    }
    public bool UpdatePortStay(ShipState ship)
    {
        if (_disposed || !ship.IsInPort) return false;
        if (ship.PendingMerchantArrivalSequence > ship.MerchantArrivalSequence)
        {
            if (!_pending.Contains(ship.ShipId) && (!_retry.TryGetValue(ship.ShipId, out var due) || due <= DateTime.UtcNow)) StartTrade(ship);
            return true;
        }
        if (_pending.Contains(ship.ShipId) || ship.IsEconomyBusy || DateTime.UtcNow < ship.PortDepartureTime) return true;
        ship.IsInPort = false; ship.CurrentTradePortId = null; return false;
    }
}
