using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipCrewService))]
public class ShipCrewService
{
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private readonly PhysicalShipService
        _physicalShipService;

    private readonly Dictionary<
        string,
        Dictionary<string, ShipCrewMember>>
        _crewByShip = new();

    public ShipCrewService(
        PhysicalShipService physicalShipService)
    {
        _physicalShipService =
            physicalShipService;

     //   _physicalShipService.PlayerBoarded +=
      //      HandlePlayerBoarded;
//handle this later
      //  _physicalShipService.PlayerLeft +=
       //     HandlePlayerLeft;

        Log.Info(
            "Ship Crew Service initialized.");
    }

    // -----------------------------------------------------------------
    // Physical Ship Events
    // -----------------------------------------------------------------

    private void HandlePlayerBoarded(
        string shipName,
        NwPlayer player)
    {
        Log.Info(
            $"Ship crew received boarding event: " +
            $"Ship={shipName}, " +
            $"Player={player.PlayerName}");

        AddPassenger(
            shipName,
            player);
    }

    private void HandlePlayerLeft(
        string shipName,
        NwPlayer player)
    {
        Log.Info(
            $"Ship crew received departure event: " +
            $"Ship={shipName}, " +
            $"Player={player.PlayerName}");

        RemoveMember(
            shipName,
            player.PlayerName);
    }

    // -----------------------------------------------------------------
    // Add Passenger
    // -----------------------------------------------------------------

    public bool AddPassenger(
    string shipName,
    NwPlayer player)
{
    if (!_physicalShipService.IsPlayerAboard(
            shipName,
            player.PlayerName))
    {
        Log.Warn(
            $"Cannot add passenger " +
            $"'{player.PlayerName}' to '{shipName}': " +
            "player is not aboard the ship.");

        return false;
    }

    ShipCrewMember? existingMember =
        GetCrewMember(
            shipName,
            player.PlayerName);

    if (existingMember != null)
    {
        if (existingMember.Role ==
            ShipCrewRole.Captain)
        {
            Log.Info(
                $"Player '{player.PlayerName}' " +
                $"is already captain of '{shipName}'. " +
                "Preserving captain role.");

            return true;
        }

        if (existingMember.Role ==
            ShipCrewRole.Crew)
        {
            Log.Info(
                $"Player '{player.PlayerName}' " +
                $"is already crew of '{shipName}'. " +
                "Preserving crew role.");

            return true;
        }
    }

    return SetRole(
        shipName,
        player.PlayerName,
        ShipCrewRole.Passenger);
}
    // -----------------------------------------------------------------
    // Set Crew
    // -----------------------------------------------------------------

    public bool SetCrewMember(
        string shipName,
        NwPlayer player)
    {
        if (!_physicalShipService.IsPlayerAboard(
                shipName,
                player.PlayerName))
        {
            Log.Warn(
                $"Cannot assign crew member " +
                $"'{player.PlayerName}' to '{shipName}': " +
                "player is not aboard the ship.");

            return false;
        }

        return SetRole(
            shipName,
            player.PlayerName,
            ShipCrewRole.Crew);
    }

    // -----------------------------------------------------------------
    // Set Captain
    // -----------------------------------------------------------------

    public bool SetCaptain(
        string shipName,
        NwPlayer player)
    {
        if (!_physicalShipService.IsPlayerAboard(
                shipName,
                player.PlayerName))
        {
            Log.Warn(
                $"Cannot make '{player.PlayerName}' " +
                $"captain of '{shipName}': " +
                "player is not aboard the ship.");

            return false;
        }

        Dictionary<string, ShipCrewMember>
            crew =
                GetOrCreateCrew(
                    shipName);

        foreach (ShipCrewMember member
            in crew.Values)
        {
            if (member.Role ==
                ShipCrewRole.Captain)
            {
                member.Role =
                    ShipCrewRole.Crew;

                Log.Info(
                    $"Previous captain changed to crew: " +
                    $"Ship={shipName}, " +
                    $"Player={member.PlayerName}");
            }
        }

        return SetRole(
            shipName,
            player.PlayerName,
            ShipCrewRole.Captain);
    }

    // -----------------------------------------------------------------
    // Set Role
    // -----------------------------------------------------------------

    private bool SetRole(
        string shipName,
        string playerName,
        ShipCrewRole role)
    {
        Dictionary<string, ShipCrewMember>
            crew =
                GetOrCreateCrew(
                    shipName);

        if (crew.TryGetValue(
                playerName,
                out ShipCrewMember? member))
        {
            member.Role =
                role;
        }
        else
        {
            member =
                new ShipCrewMember
                {
                    PlayerName =
                        playerName,

                    Role =
                        role
                };

            crew[playerName] =
                member;
        }

        Log.Info(
            $"Ship crew role assigned: " +
            $"Ship={shipName}, " +
            $"Player={playerName}, " +
            $"Role={role}");

        return true;
    }

    // -----------------------------------------------------------------
    // Remove Member
    // -----------------------------------------------------------------

    public bool RemoveMember(
        string shipName,
        string playerName)
    {
        if (!_crewByShip.TryGetValue(
                shipName,
                out Dictionary<string, ShipCrewMember>?
                    crew))
        {
            return false;
        }

        bool removed =
            crew.Remove(
                playerName);

        if (removed)
        {
            Log.Info(
                $"Ship crew member removed: " +
                $"Ship={shipName}, " +
                $"Player={playerName}");

            if (crew.Count == 0)
            {
                _crewByShip.Remove(
                    shipName);
            }
        }

        return removed;
    }

    // -----------------------------------------------------------------
    // Lookup
    // -----------------------------------------------------------------

    public ShipCrewMember? GetCrewMember(
        string shipName,
        string playerName)
    {
        if (!_crewByShip.TryGetValue(
                shipName,
                out Dictionary<string, ShipCrewMember>?
                    crew))
        {
            return null;
        }

        crew.TryGetValue(
            playerName,
            out ShipCrewMember? member);

        return member;
    }

    public ShipCrewRole? GetRole(
        string shipName,
        string playerName)
    {
        return GetCrewMember(
                   shipName,
                   playerName)
               ?.Role;
    }

    public IReadOnlyCollection<ShipCrewMember>
        GetCrew(
            string shipName)
    {
        if (!_crewByShip.TryGetValue(
                shipName,
                out Dictionary<string, ShipCrewMember>?
                    crew))
        {
            return Array.Empty<ShipCrewMember>();
        }

        return crew.Values;
    }

    public ShipCrewMember? GetCaptain(
        string shipName)
    {
        if (!_crewByShip.TryGetValue(
                shipName,
                out Dictionary<string, ShipCrewMember>?
                    crew))
        {
            return null;
        }

        return crew.Values.FirstOrDefault(
            member =>
                member.Role ==
                ShipCrewRole.Captain);
    }

    // -----------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------

    private Dictionary<string, ShipCrewMember>
        GetOrCreateCrew(
            string shipName)
    {
        if (!_crewByShip.TryGetValue(
                shipName,
                out Dictionary<string, ShipCrewMember>?
                    crew))
        {
            crew =
                new Dictionary<string, ShipCrewMember>(
                    StringComparer.Ordinal);

            _crewByShip[
                shipName] =
                crew;
        }

        return crew;
    }
}