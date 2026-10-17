namespace AmiaReforged.Core.Models.Sailing;

/// <summary>One-use captain acceptance, bound to the exact request and ships.</summary>
public sealed class AcceptedBoardingTravelBook
{
    private sealed class Acceptance(ShipBoardingRequest request, DateTime expires)
    {
        public ShipState Source { get; } = request.RequestingShip;
        public ShipState Target { get; } = request.TargetShip;
        public string Requester { get; } = request.RequestingPlayerName;
        public string Captain { get; } = request.TargetPlayerName;
        public DateTime Expires { get; } = expires;
        public bool Started { get; set; }
        public bool Matches(ShipBoardingRequest request) => ReferenceEquals(Source, request.RequestingShip) &&
            ReferenceEquals(Target, request.TargetShip) && Requester == request.RequestingPlayerName && Captain == request.TargetPlayerName;
    }
    private readonly Dictionary<ShipBoardingRequest, Acceptance> _accepted = new(ReferenceEqualityComparer.Instance);

    public void Record(ShipBoardingRequest request, DateTime expires) => _accepted.TryAdd(request, new(request, expires));
    public bool IsValid(ShipBoardingRequest request, DateTime now) => _accepted.TryGetValue(request, out var acceptance) &&
        acceptance.Matches(request) && (acceptance.Started || now < acceptance.Expires);
    public bool TryStart(ShipBoardingRequest request, DateTime now)
    {
        if (!IsValid(request, now) || _accepted[request].Started) return false;
        _accepted[request].Started = true;
        return true;
    }
    public void Remove(ShipBoardingRequest request) => _accepted.Remove(request);
    public void Cleanup(DateTime now, Func<ShipBoardingRequest, bool> stillValid)
    {
        foreach (var request in _accepted.Keys.ToArray())
            if (!IsValid(request, now) || !stillValid(request)) Remove(request);
    }
    public void CancelPlayer(string player)
    {
        foreach (var pair in _accepted.Where(p => p.Value.Requester == player || p.Value.Captain == player).ToArray()) Remove(pair.Key);
    }
    public void CancelShip(ShipState ship)
    {
        foreach (var pair in _accepted.Where(p => ReferenceEquals(p.Value.Source, ship) || ReferenceEquals(p.Value.Target, ship)).ToArray()) Remove(pair.Key);
    }
}
