using System.Numerics;
using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipBoardingService))]
public class ShipBoardingService
{
    private readonly PhysicalShipService
    _physicalShipService;
    private const float BoardingOffset = 2.0f;

    private readonly Dictionary<string, ShipBoardingRequest>
        _requestsByTargetShip = new();

    private readonly Dictionary<string, ShipBoardingRequest>
        _requestsByRequestingPlayer = new();

    private readonly ShipEncounterService
        _shipEncounterService;

    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    public event Action<ShipBoardingRequest>?
        BoardingCompleted;

    public ShipBoardingService(
    ShipEncounterService shipEncounterService,
    PhysicalShipService physicalShipService)
{
    _shipEncounterService =
        shipEncounterService;

    _physicalShipService =
        physicalShipService;

    Log.Info(
        "Ship Boarding Service initialized.");
}

    public bool TryRequestBoarding(
        ShipState requestingShip,
        string requestingPlayerName,
        out ShipBoardingRequest? request)
    {
        request = null;

        if (!_shipEncounterService.TryGetTarget(
                requestingShip,
                out ShipState? targetShip,
                out ShipEncounter? encounter) ||
            targetShip == null ||
            encounter == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                targetShip.HelmsmanPCKey))
        {
            return false;
        }

        if (_requestsByRequestingPlayer.ContainsKey(
                requestingPlayerName))
        {
            return false;
        }

        if (_requestsByTargetShip.ContainsKey(
                targetShip.ShipName))
        {
            return false;
        }

        request = new ShipBoardingRequest
        {
            RequestingShip =
                requestingShip,

            TargetShip =
                targetShip,

            RequestingPlayerName =
                requestingPlayerName,

            TargetPlayerName =
                targetShip.HelmsmanPCKey,

            RequestedAt =
                DateTime.UtcNow
        };

        _requestsByTargetShip[
            targetShip.ShipName] =
            request;

        _requestsByRequestingPlayer[
            requestingPlayerName] =
            request;

        Log.Info(
            $"Boarding request created: " +
            $"{requestingShip.ShipName} -> " +
            $"{targetShip.ShipName}, " +
            $"Requester={requestingPlayerName}, " +
            $"Target={request.TargetPlayerName}, " +
            $"Distance={encounter.Distance:0.00}");

        NwPlayer? targetPlayer =
            FindPlayer(
                request.TargetPlayerName);

        if (targetPlayer != null)
        {
            targetPlayer.SendServerMessage(
                $"The {requestingShip.ShipName} " +
                $"is requesting permission to board.");

            targetPlayer.SendServerMessage(
                "Use ACCEPT or REJECT in the sailing window.");
        }

        return true;
    }

    public bool TryAcceptBoarding(
        string targetPlayerName,
        out ShipBoardingRequest? request)
    {
        request = null;

        ShipBoardingRequest? foundRequest =
            FindRequestForTargetPlayer(
                targetPlayerName);

        if (foundRequest == null)
        {
            return false;
        }

        if (!_shipEncounterService.TryGetEncounter(
                foundRequest.RequestingShip,
                foundRequest.TargetShip,
                out ShipEncounter? encounter) ||
            encounter == null)
        {
            RemoveRequest(
                foundRequest);

            return false;
        }

        NwPlayer? requestingPlayer =
            FindPlayer(
                foundRequest.RequestingPlayerName);

        if (requestingPlayer == null)
        {
            RemoveRequest(
                foundRequest);

            return false;
        }

        NwCreature? creature =
            requestingPlayer.ControlledCreature;

        if (creature == null ||
            !creature.IsValid)
        {
            RemoveRequest(
                foundRequest);

            return false;
        }

        NwWaypoint? spawnWaypoint =
    NwObject.FindObjectsWithTag<NwWaypoint>(
        "SAILING_DECK_SPAWN")
    .FirstOrDefault(
        waypoint =>
            waypoint.Area != null &&
            string.Equals(
                waypoint.Area.ResRef,
                foundRequest.TargetShip.DeckAreaResRef,
                StringComparison.OrdinalIgnoreCase));

if (spawnWaypoint == null)
{
    Log.Warn(
        $"Cannot board '{foundRequest.TargetShip.ShipName}': " +
        $"no deck spawn found for area " +
        $"'{foundRequest.TargetShip.DeckAreaResRef}'.");

    RemoveRequest(
        foundRequest);

    return false;
}

Location boardingLocation =
    spawnWaypoint.Location;
    
    creature.Location =
    boardingLocation;
    _physicalShipService.RemovePlayerAboard(
    foundRequest.RequestingShip.ShipName,
    requestingPlayer);
        if (!_physicalShipService.AddPlayerAboard(
        foundRequest.TargetShip.ShipName,
        requestingPlayer))
{
    Log.Warn(
        $"Player '{requestingPlayer.PlayerName}' was moved " +
        $"onto '{foundRequest.TargetShip.ShipName}' but " +
        "could not be added to aboard tracking.");
}
        foundRequest.RequestingShip.HelmsmanPCKey =
            null;

        RemoveRequest(
            foundRequest);

        requestingPlayer.SendServerMessage(
            $"You board the {foundRequest.TargetShip.ShipName}.");

        Log.Info(
            $"Boarding accepted: " +
            $"{foundRequest.RequestingPlayerName} boarded " +
            $"{foundRequest.TargetShip.ShipName}.");

        BoardingCompleted?.Invoke(
            foundRequest);

        request =
            foundRequest;

        return true;
    }

    public bool TryRejectBoarding(
        string targetPlayerName,
        out ShipBoardingRequest? request)
    {
        request = null;

        ShipBoardingRequest? foundRequest =
            FindRequestForTargetPlayer(
                targetPlayerName);

        if (foundRequest == null)
        {
            return false;
        }

        NwPlayer? requestingPlayer =
            FindPlayer(
                foundRequest.RequestingPlayerName);

        requestingPlayer?.SendServerMessage(
            $"Your request to board the " +
            $"{foundRequest.TargetShip.ShipName} was rejected.");

        NwPlayer? targetPlayer =
            FindPlayer(
                targetPlayerName);

        targetPlayer?.SendServerMessage(
            $"You reject the boarding request from the " +
            $"{foundRequest.RequestingShip.ShipName}.");

        Log.Info(
            $"Boarding rejected: " +
            $"{foundRequest.RequestingShip.ShipName} -> " +
            $"{foundRequest.TargetShip.ShipName}");

        RemoveRequest(
            foundRequest);

        request =
            foundRequest;

        return true;
    }

    public bool HasRequestForPlayer(
        string playerName)
    {
        return FindRequestForTargetPlayer(
            playerName) != null;
    }

    public bool HasRequestFromPlayer(
        string playerName)
    {
        return _requestsByRequestingPlayer.ContainsKey(
            playerName);
    }

    public bool TryGetRequestForPlayer(
        string playerName,
        out ShipBoardingRequest? request)
    {
        request =
            FindRequestForTargetPlayer(
                playerName);

        return request != null;
    }

    public IReadOnlyCollection<ShipBoardingRequest>
        GetActiveRequests()
    {
        return _requestsByTargetShip.Values;
    }

    private ShipBoardingRequest? FindRequestForTargetPlayer(
        string playerName)
    {
        foreach (ShipBoardingRequest request
            in _requestsByTargetShip.Values)
        {
            if (string.Equals(
                    request.TargetPlayerName,
                    playerName,
                    StringComparison.Ordinal))
            {
                return request;
            }
        }

        return null;
    }

    private NwPlayer? FindPlayer(
        string playerName)
    {
        return NwModule.Instance.Players.FirstOrDefault(
            player =>
                string.Equals(
                    player.PlayerName,
                    playerName,
                    StringComparison.Ordinal));
    }

    private void RemoveRequest(
        ShipBoardingRequest request)
    {
        _requestsByTargetShip.Remove(
            request.TargetShip.ShipName);

        _requestsByRequestingPlayer.Remove(
            request.RequestingPlayerName);
    }

    private Vector3 GetBoardingPosition(
        ShipState ship)
    {
        float x =
            ship.X;

        float y =
            ship.Y;

        switch (ship.Heading)
        {
            case Heading.North:
                y -= BoardingOffset;
                break;

            case Heading.NorthEast:
                x -= BoardingOffset;
                y -= BoardingOffset;
                break;

            case Heading.East:
                x -= BoardingOffset;
                break;

            case Heading.SouthEast:
                x -= BoardingOffset;
                y += BoardingOffset;
                break;

            case Heading.South:
                y += BoardingOffset;
                break;

            case Heading.SouthWest:
                x += BoardingOffset;
                y += BoardingOffset;
                break;

            case Heading.West:
                x += BoardingOffset;
                break;

            case Heading.NorthWest:
                x += BoardingOffset;
                y -= BoardingOffset;
                break;
        }

        return new Vector3(
            x,
            y,
            ship.Z);
    }

    private float GetHeadingRotation(
        Heading heading)
    {
        return heading switch
        {
            Heading.East => 0.0f,
            Heading.NorthEast => 0.785398f,
            Heading.North => 1.570796f,
            Heading.NorthWest => 2.356194f,
            Heading.West => 3.141593f,
            Heading.SouthWest => 3.926991f,
            Heading.South => 4.712389f,
            Heading.SouthEast => 5.497787f,
            _ => 0.0f
        };
    }
}