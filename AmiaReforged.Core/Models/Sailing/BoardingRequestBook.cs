namespace AmiaReforged.Core.Models.Sailing;

/// <summary>Bounded consent requests; expiry does not depend on a window remaining open.</summary>
public sealed class BoardingRequestBook
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);
    private readonly List<ShipBoardingRequest> _requests = new();
    public IReadOnlyCollection<ShipBoardingRequest> Requests => _requests.ToArray();

    public void Cleanup(DateTime now, Func<ShipBoardingRequest, bool>? stillValid = null) =>
        _requests.RemoveAll(r => now - r.RequestedAt >= Lifetime || r.RequestedAt > now ||
            (stillValid != null && !stillValid(r)));

    public bool Add(ShipBoardingRequest request, DateTime now)
    {
        Cleanup(now);
        if (_requests.Any(r => ReferenceEquals(r.TargetShip, request.TargetShip) ||
            r.RequestingPlayerName == request.RequestingPlayerName)) return false;
        request.RequestedAt = now;
        _requests.Add(request);
        return true;
    }

    public ShipBoardingRequest? ForTarget(string player) => _requests.FirstOrDefault(r => r.TargetPlayerName == player);
    public bool FromPlayer(string player) => _requests.Any(r => r.RequestingPlayerName == player);
    public void Remove(ShipBoardingRequest request) => _requests.Remove(request);
    public void CancelPlayer(string player) => _requests.RemoveAll(r => r.RequestingPlayerName == player || r.TargetPlayerName == player);
    public void CancelShip(ShipState ship) => _requests.RemoveAll(r => ReferenceEquals(r.RequestingShip, ship) || ReferenceEquals(r.TargetShip, ship));
}
