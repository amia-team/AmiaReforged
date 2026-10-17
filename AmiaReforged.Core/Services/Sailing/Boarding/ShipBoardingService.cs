using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipBoardingService))]
public class ShipBoardingService
{
    private readonly ShipRegistryService _registry;
    private readonly PhysicalShipService _physical;
    private readonly ShipAccessService? _access;
    private readonly BoardingRequestBook _book = new();
    private readonly AcceptedBoardingTravelBook _accepted = new();
    private readonly TimeSpan _travelLifetime;
    public event Action<ShipBoardingRequest>? BoardingCompleted;
    public event Action<ShipBoardingRequest>? AcceptedTravelRequested;

    public ShipBoardingService(ShipRegistryService registry, PhysicalShipService physicalShipService,
        ShipEncounterService shipEncounterService, ShipAccessService? access = null, MaritimeOptions? options = null)
    {
        _access = access;
        _registry = registry;
        _physical = physicalShipService;
        _travelLifetime = TimeSpan.FromSeconds(options?.PartyConfirmationSeconds ?? 60);
    }

    public bool TryRequestBoarding(ShipState requestingShip, ShipState? targetShip, string requestingPlayerName,
        out ShipBoardingRequest? request)
    {
        request = null;
        Cleanup();
        if (targetShip == null || string.IsNullOrWhiteSpace(targetShip.HelmsmanPCKey)) return false;
        ShipBoardingRequest candidate = new()
        {
            RequestingShip = requestingShip, TargetShip = targetShip,
            RequestingPlayerName = requestingPlayerName, TargetPlayerName = targetShip.HelmsmanPCKey,
            RequestedAt = DateTime.UtcNow
        };
        if (!IsValid(candidate) || !_book.Add(candidate, DateTime.UtcNow)) return false;
        request = candidate;
        FindPlayer(candidate.TargetPlayerName)?.SendServerMessage(
            $"The {requestingShip.ShipName} requests permission to board. Use ACCEPT or REJECT within 60 seconds.");
        return true;
    }

    public async Task<ShipBoardingRequest?> TryAcceptBoardingAsync(string targetPlayerName)
    {
        Cleanup();
        ShipBoardingRequest? candidate = _book.ForTarget(targetPlayerName);
        if (candidate == null) return null;
        _book.Remove(candidate); // Every attempted resolution frees the request, even a failed transfer.
        if (!IsValid(candidate)) return null;
        NwPlayer? player = FindPlayer(candidate.RequestingPlayerName);
        if (player == null || !_physical.TryGetBoardingLocation(candidate.TargetShip.ShipName, out Location? spawn) || spawn == null) return null;
        NwPlayer? captain = FindPlayer(candidate.TargetPlayerName);
        if (captain == null) return null;
        if (_access != null && !(await _access.AdmitGuestAsync(candidate.TargetShip, captain, player)).Success) return null;
        await NwTask.SwitchToMainThread();
        if (!IsValid(candidate)) return null;
        _accepted.Record(candidate, DateTime.UtcNow + _travelLifetime);
        if (AcceptedTravelRequested != null) { AcceptedTravelRequested(candidate); return candidate; }
        return await TransferAcceptedAsync(candidate, [player]) ? candidate : null;
    }

    public bool IsAcceptedTravelValid(ShipBoardingRequest request) => _accepted.IsValid(request, DateTime.UtcNow) &&
        IsValid(request, requireSourcePresence: false);
    public void CancelAcceptedTravel(ShipBoardingRequest request) => _accepted.Remove(request);

    public async Task<bool> TransferAcceptedAsync(ShipBoardingRequest candidate, IReadOnlyList<NwPlayer> players,
        Func<bool>? validateParty = null, Func<NwPlayer, bool>? validateDeparture = null)
    {
        if (!IsAcceptedTravelValid(candidate) || players.Count == 0 || players[0].PlayerName != candidate.RequestingPlayerName || !IsValid(candidate) ||
            !_physical.TryGetBoardingLocation(candidate.TargetShip.ShipName, out Location? spawn) || spawn == null) return false;
        if (!_accepted.TryStart(candidate, DateTime.UtcNow)) return false;
        try
        {
            if (!await _physical.TryTransferPlayersAsync(players, candidate.RequestingShip.ShipName,
                    candidate.TargetShip.ShipName, spawn, () => IsAcceptedTravelValid(candidate) && validateParty?.Invoke() != false,
                    validateDeparture: validateDeparture)) return false;
            if (BoardingCompleted != null)
                foreach (Action<ShipBoardingRequest> callback in BoardingCompleted.GetInvocationList())
                    try { callback(candidate); }
                    catch (Exception ex) { NLog.LogManager.GetCurrentClassLogger().Error(ex, "Boarding completion subscriber failed."); }
            return true;
        }
        finally { _accepted.Remove(candidate); }
    }

    public bool TryRejectBoarding(string targetPlayerName, out ShipBoardingRequest? request)
    {
        Cleanup();
        request = _book.ForTarget(targetPlayerName);
        if (request == null) return false;
        _book.Remove(request);
        FindPlayer(request.RequestingPlayerName)?.SendServerMessage($"Your request to board {request.TargetShip.ShipName} was rejected.");
        return true;
    }

    private bool IsValid(ShipBoardingRequest request) => IsValid(request, requireSourcePresence: true);

    private bool IsValid(ShipBoardingRequest request, bool requireSourcePresence)
    {
        ShipState source = request.RequestingShip, target = request.TargetShip;
        if (!_registry.IsCurrent(source) || !_registry.IsCurrent(target) ||
            !ShipActionRules.CanInteract(source, target) || source.IsDisabled || target.IsDisabled ||
            source.ShipType != ShipType.Player || target.ShipType != ShipType.Player ||
            target.HelmsmanPCKey != request.TargetPlayerName ||
            ShipActionRules.Distance(source, target) > OceanContactService.BoardRange) return false;
        NwPlayer? requester = FindPlayer(request.RequestingPlayerName), helmsman = FindPlayer(request.TargetPlayerName);
        return requester != null && helmsman != null &&
            _physical.GetShipForPlayer(requester.PlayerName) == source.ShipName &&
            (!requireSourcePresence || _physical.IsPlayerPresent(source.ShipName, requester)) &&
            _physical.IsPlayerPresent(target.ShipName, helmsman);
    }

    public void Cleanup()
    {
        _book.Cleanup(DateTime.UtcNow, IsValid);
        _accepted.Cleanup(DateTime.UtcNow, request => IsValid(request, requireSourcePresence: false));
    }
    public void CancelPlayer(string player) { _book.CancelPlayer(player); _accepted.CancelPlayer(player); }
    public void CancelShip(ShipState ship) { _book.CancelShip(ship); _accepted.CancelShip(ship); }
    public bool HasRequestForPlayer(string player) { Cleanup(); return _book.ForTarget(player) != null; }
    public bool HasRequestFromPlayer(string player) { Cleanup(); return _book.FromPlayer(player); }
    public bool TryGetRequestForPlayer(string player, out ShipBoardingRequest? request)
    { Cleanup(); request = _book.ForTarget(player); return request != null; }
    public IReadOnlyCollection<ShipBoardingRequest> GetActiveRequests() { Cleanup(); return _book.Requests; }
    private static NwPlayer? FindPlayer(string name) => NwModule.Instance.Players.FirstOrDefault(p => p.IsValid && p.PlayerName == name);
}
