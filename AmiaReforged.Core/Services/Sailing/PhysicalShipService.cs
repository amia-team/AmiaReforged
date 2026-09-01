using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(PhysicalShipService))]
public class PhysicalShipService
{
    private const float DeckSpawnX = 43.0f;

    private const float DeckSpawnY = 43.0f;

    private const float DeckSpawnZ = 0.0f;

    private const float DeckSpawnRotation = 0.0f;

    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private readonly Dictionary<string, PhysicalShip>
        _physicalShips = new();

    // -----------------------------------------------------------------
    // Aboard Player Tracking
    // -----------------------------------------------------------------

    private readonly Dictionary<string, HashSet<string>>
        _aboardPlayers = new();

    // -----------------------------------------------------------------
    // Aboard Player Events
    // -----------------------------------------------------------------

    public event Action<string, NwPlayer>? PlayerBoarded;

    public event Action<string, NwPlayer>? PlayerLeft;

public PhysicalShipService()
{
    Log.Info("Physical Ship Service initialized.");
}

    // -----------------------------------------------------------------
    // Registration
    // -----------------------------------------------------------------

public void RegisterPhysicalShip(ShipDefinition definition)
{
    PhysicalShip ship = new()
    {
        ShipName = definition.ShipName,
        PlaceableTag = definition.PlaceableTag,
        ExitPlaceableTag = definition.ExitTag,
        DeckAreaResRef = definition.DeckAreaResRef,
        CabinAreaResRef = definition.CabinAreaResRef
    };

    _physicalShips[ship.ShipName] = ship;

    Log.Info(
        $"Registered physical ship: " +
        $"Ship={ship.ShipName}, " +
        $"Tag={ship.PlaceableTag}, " +
        $"Deck={ship.DeckAreaResRef}, " +
        $"Cabin={ship.CabinAreaResRef}");
}
    // -----------------------------------------------------------------
    // Physical Ship Interaction Registration
    // -----------------------------------------------------------------

    public void RegisterPhysicalShipInteractions()
    {
        foreach (PhysicalShip physicalShip
            in _physicalShips.Values)
        {
            List<NwPlaceable> shipPlaceables =
                FindShipPlaceables(
                    physicalShip.ShipName);

            foreach (NwPlaceable placeable
                in shipPlaceables)
            {
                placeable.OnLeftClick +=
                    HandlePhysicalShipClick;

                Log.Info(
                    $"Physical ship interaction registered: " +
                    $"Ship={physicalShip.ShipName}, " +
                    $"Tag={placeable.Tag}, " +
                    $"ResRef={placeable.ResRef}");
            }

            List<NwPlaceable> exitPlaceables =
                NwObject.FindObjectsWithTag<NwPlaceable>(
                    physicalShip.ExitPlaceableTag)
                .ToList();

            foreach (NwPlaceable exitPlaceable
                in exitPlaceables)
            {
                exitPlaceable.OnLeftClick +=
                    HandleShipExitClick;

                Log.Info(
                    $"Ship exit registered: " +
                    $"Ship={physicalShip.ShipName}, " +
                    $"Tag={exitPlaceable.Tag}, " +
                    $"ResRef={exitPlaceable.ResRef}");
            }
        }
    }

    // -----------------------------------------------------------------
    // Lookup
    // -----------------------------------------------------------------

    public PhysicalShip? GetPhysicalShip(
        string shipName)
    {
        if (_physicalShips.TryGetValue(
                shipName,
                out PhysicalShip? physicalShip))
        {
            return physicalShip;
        }

        Log.Warn(
            $"Physical ship '{shipName}' " +
            "is not registered.");

        return null;
    }

    public IReadOnlyCollection<PhysicalShip>
        GetPhysicalShips()
    {
        return _physicalShips.Values;
    }

    public bool HasPhysicalShip(
        string shipName)
    {
        return _physicalShips.ContainsKey(
            shipName);
    }

    // -----------------------------------------------------------------
    // Physical Ship Lookup
    // -----------------------------------------------------------------

    public List<NwPlaceable> FindShipPlaceables(
        string shipName)
    {
        PhysicalShip? physicalShip =
            GetPhysicalShip(shipName);

        if (physicalShip == null)
        {
            return new List<NwPlaceable>();
        }

        List<NwObject> objects =
            NwObject.FindObjectsWithTag(
                physicalShip.PlaceableTag)
            .ToList();

        List<NwPlaceable> placeables =
            objects
                .OfType<NwPlaceable>()
                .ToList();

        Log.Info(
            $"Physical ship lookup: " +
            $"Ship={shipName}, " +
            $"Tag={physicalShip.PlaceableTag}, " +
            $"Found={placeables.Count}");

        foreach (NwPlaceable placeable
            in placeables)
        {
            Log.Info(
                $"Physical ship found: " +
                $"Ship={shipName}, " +
                $"Tag={placeable.Tag}, " +
                $"ResRef={placeable.ResRef}");
        }

        return placeables;
    }

    public NwPlaceable? FindShipPlaceable(
        string shipName)
    {
        List<NwPlaceable> placeables =
            FindShipPlaceables(
                shipName);

        if (placeables.Count == 0)
        {
            Log.Warn(
                $"Physical ship not found: " +
                $"Ship={shipName}");

            return null;
        }

        return placeables[0];
    }

    // -----------------------------------------------------------------
    // Aboard Player Tracking
    // -----------------------------------------------------------------

    public bool AddPlayerAboard(
        string shipName,
        NwPlayer player)
    {
        if (!HasPhysicalShip(shipName))
        {
            Log.Warn(
                $"Cannot add player '{player.PlayerName}' " +
                $"aboard '{shipName}': " +
                "physical ship does not exist.");

            return false;
        }

        string? existingShip =
            GetShipForPlayer(
                player.PlayerName);

        if (existingShip != null)
        {
            if (string.Equals(
                    existingShip,
                    shipName,
                    StringComparison.Ordinal))
            {
                Log.Info(
                    $"Player {player.PlayerName} " +
                    $"is already aboard {shipName}.");

                return false;
            }

            Log.Warn(
                $"Player {player.PlayerName} " +
                $"is already aboard '{existingShip}' " +
                $"and cannot be added to '{shipName}'.");

            return false;
        }

        if (!_aboardPlayers.TryGetValue(
                shipName,
                out HashSet<string>? players))
        {
            players =
                new HashSet<string>(
                    StringComparer.Ordinal);

            _aboardPlayers[
                shipName] =
                players;
        }

        players.Add(
            player.PlayerName);

        Log.Info(
            $"Player {player.PlayerName} " +
            $"is now aboard {shipName}. " +
            $"Aboard={players.Count}");

        // Notify interested services that the player
        // has successfully boarded the ship.
        PlayerBoarded?.Invoke(
            shipName,
            player);

        return true;
    }

    public bool RemovePlayerAboard(
        string shipName,
        NwPlayer player)
    {
        if (!_aboardPlayers.TryGetValue(
                shipName,
                out HashSet<string>? players))
        {
            Log.Info(
                $"Player {player.PlayerName} " +
                $"was not being tracked aboard " +
                $"{shipName}.");

            return false;
        }

        bool removed =
            players.Remove(
                player.PlayerName);

        if (removed)
        {
            Log.Info(
                $"Player {player.PlayerName} " +
                $"left {shipName}. " +
                $"Aboard={players.Count}");

            // Notify interested services that the player
            // has successfully left the ship.
            PlayerLeft?.Invoke(
                shipName,
                player);

            if (players.Count == 0)
            {
                _aboardPlayers.Remove(
                    shipName);
            }
        }

        return removed;
    }

    public bool IsPlayerAboard(
        string shipName,
        string playerName)
    {
        return _aboardPlayers.TryGetValue(
                   shipName,
                   out HashSet<string>? players) &&
               players.Contains(
                   playerName);
    }

    public IReadOnlyCollection<string>
        GetPlayersAboard(
            string shipName)
    {
        if (!_aboardPlayers.TryGetValue(
                shipName,
                out HashSet<string>? players))
        {
            return Array.Empty<string>();
        }

        return players;
    }

    public string? GetShipForPlayer(
        string playerName)
    {
        foreach (
            KeyValuePair<string, HashSet<string>>
                entry in _aboardPlayers)
        {
            if (entry.Value.Contains(
                    playerName))
            {
                return entry.Key;
            }
        }

        return null;
    }

    // -----------------------------------------------------------------
    // Board Ship
    // -----------------------------------------------------------------

    public bool BoardShip(
        string shipName,
        NwPlayer player)
    {
        PhysicalShip? physicalShip =
            GetPhysicalShip(shipName);

        if (physicalShip == null)
        {
            player.SendServerMessage(
                "That ship could not be found.");

            return false;
        }

        // -------------------------------------------------------------
        // Prevent duplicate boarding
        // -------------------------------------------------------------

        if (IsPlayerAboard(
                shipName,
                player.PlayerName))
        {
            player.SendServerMessage(
                $"You are already aboard the {shipName}.");

            Log.Info(
                $"Player {player.PlayerName} attempted " +
                $"to board '{shipName}', but is already aboard.");

            return false;
        }

        // -------------------------------------------------------------
        // Prevent being aboard multiple ships
        // -------------------------------------------------------------

        string? existingShip =
            GetShipForPlayer(
                player.PlayerName);

        if (existingShip != null)
        {
            player.SendServerMessage(
                $"You are already aboard the {existingShip}.");

            Log.Info(
                $"Player {player.PlayerName} attempted " +
                $"to board '{shipName}', but is already " +
                $"aboard '{existingShip}'.");

            return false;
        }

        NwCreature? creature =
            player.ControlledCreature;

        if (creature == null ||
            !creature.IsValid)
        {
            Log.Warn(
                $"Cannot board '{shipName}': " +
                $"player '{player.PlayerName}' " +
                "has no valid controlled creature.");

            return false;
        }

        NwArea? deckArea =
            NwModule.Instance.Areas.FirstOrDefault(
                area =>
                    string.Equals(
                        area.ResRef,
                        physicalShip.DeckAreaResRef,
                        StringComparison.OrdinalIgnoreCase));

        if (deckArea == null)
        {
            Log.Warn(
                $"Cannot board '{shipName}': " +
                $"deck area '{physicalShip.DeckAreaResRef}' " +
                "was not found.");

            player.SendServerMessage(
                "The ship's deck is currently unavailable.");

            return false;
        }

        System.Numerics.Vector3 position =
            new(
                DeckSpawnX,
                DeckSpawnY,
                DeckSpawnZ);

        Location boardingLocation =
            Location.Create(
                deckArea,
                position,
                DeckSpawnRotation);

        creature.Location =
            boardingLocation;

        // -------------------------------------------------------------
        // Track player aboard ship
        // -------------------------------------------------------------

        if (!AddPlayerAboard(
                shipName,
                player))
        {
            Log.Warn(
                $"Player {player.PlayerName} was moved to " +
                $"the deck of '{shipName}', but could not be " +
                "added to aboard tracking.");

            return false;
        }

        player.SendServerMessage(
            $"You board the {shipName}.");

        Log.Info(
            $"Player {player.PlayerName} boarded " +
            $"the {shipName}. " +
            $"Deck={physicalShip.DeckAreaResRef}, " +
            $"Position=(" +
            $"{DeckSpawnX:0.00}, " +
            $"{DeckSpawnY:0.00}, " +
            $"{DeckSpawnZ:0.00})");

        return true;
    }

    // -----------------------------------------------------------------
    // Leave Ship
    // -----------------------------------------------------------------

    public bool LeaveShip(
        string shipName,
        NwPlayer player)
    {
        PhysicalShip? physicalShip =
            GetPhysicalShip(shipName);

        if (physicalShip == null)
        {
            player.SendServerMessage(
                "That ship could not be found.");

            return false;
        }

        NwCreature? creature =
            player.ControlledCreature;

        if (creature == null ||
            !creature.IsValid)
        {
            Log.Warn(
                $"Cannot leave '{shipName}': " +
                $"player '{player.PlayerName}' " +
                "has no valid controlled creature.");

            return false;
        }

        NwPlaceable? shipPlaceable =
            FindShipPlaceable(
                shipName);

        if (shipPlaceable == null)
        {
            player.SendServerMessage(
                "The physical ship could not be found.");

            return false;
        }

        Location shipLocation =
            shipPlaceable.Location;

        NwArea? area =
            shipLocation.Area;

        if (area == null)
        {
            Log.Warn(
                $"Cannot leave '{shipName}': " +
                "physical ship has no valid area.");

            return false;
        }

        System.Numerics.Vector3 position =
            shipLocation.Position;

        position.X += 3.0f;

        Location exitLocation =
            Location.Create(
                area,
                position,
                shipLocation.Rotation);

        creature.Location =
            exitLocation;

        // -------------------------------------------------------------
        // Remove player from aboard tracking
        // -------------------------------------------------------------

        RemovePlayerAboard(
            shipName,
            player);

        player.SendServerMessage(
            $"You leave the {shipName}.");

        Log.Info(
            $"Player {player.PlayerName} left " +
            $"the {shipName}. " +
            $"Area={area.ResRef}, " +
            $"Position=(" +
            $"{position.X:0.00}, " +
            $"{position.Y:0.00}, " +
            $"{position.Z:0.00})");

        return true;
    }

    // -----------------------------------------------------------------
    // Click Handlers
    // -----------------------------------------------------------------

    private void HandlePhysicalShipClick(
        PlaceableEvents.OnLeftClick obj)
    {
        NwPlayer player =
            obj.ClickedBy;

        NwPlaceable placeable =
            obj.Placeable;

        Log.Info(
            $"Physical ship clicked: " +
            $"Player={player.PlayerName}, " +
            $"Tag={placeable.Tag}, " +
            $"ResRef={placeable.ResRef}");

        PhysicalShip? physicalShip =
            _physicalShips.Values.FirstOrDefault(
                x =>
                    string.Equals(
                        x.PlaceableTag,
                        placeable.Tag,
                        StringComparison.Ordinal));

        if (physicalShip == null)
        {
            Log.Warn(
                $"No physical ship registered for " +
                $"placeable tag '{placeable.Tag}'.");

            return;
        }

        BoardShip(
            physicalShip.ShipName,
            player);
    }

    private void HandleShipExitClick(
        PlaceableEvents.OnLeftClick obj)
    {
        NwPlayer player =
            obj.ClickedBy;

        NwPlaceable placeable =
            obj.Placeable;

        Log.Info(
            $"Ship exit clicked: " +
            $"Player={player.PlayerName}, " +
            $"Tag={placeable.Tag}, " +
            $"ResRef={placeable.ResRef}");

        PhysicalShip? physicalShip =
            _physicalShips.Values.FirstOrDefault(
                x =>
                    string.Equals(
                        x.ExitPlaceableTag,
                        placeable.Tag,
                        StringComparison.Ordinal));

        if (physicalShip == null)
        {
            Log.Warn(
                $"No physical ship registered for " +
                $"exit tag '{placeable.Tag}'.");

            return;
        }

        LeaveShip(
            physicalShip.ShipName,
            player);
    }
public void SpawnPhysicalShip(string shipName)
{
    TestPhysicalShip(shipName);
}
    // -----------------------------------------------------------------
    // Test
    // -----------------------------------------------------------------

    public bool TestPhysicalShip(
        string shipName)
    {
        NwPlaceable? placeable =
            FindShipPlaceable(
                shipName);

        if (placeable == null)
        {
            return false;
        }

        Log.Info(
            $"Physical ship test successful: " +
            $"Ship={shipName}, " +
            $"Tag={placeable.Tag}, " +
            $"ResRef={placeable.ResRef}, " +
            $"Position=(" +
            $"{placeable.Position.X:0.00}, " +
            $"{placeable.Position.Y:0.00}, " +
            $"{placeable.Position.Z:0.00})");

        return true;
    }
    public bool MovePhysicalShip(
    string shipName,
    Location location)
{
    NwPlaceable? placeable =
        FindShipPlaceable(shipName);

    if (placeable == null)
    {
        Log.Warn(
            $"Cannot move physical ship '{shipName}': " +
            "physical placeable was not found.");

        return false;
    }

    placeable.Location =
        location;

    Log.Info(
        $"Physical ship '{shipName}' moved to " +
        $"Area={location.Area?.ResRef}, " +
        $"Position=(" +
        $"{location.Position.X:0.00}, " +
        $"{location.Position.Y:0.00}, " +
        $"{location.Position.Z:0.00})");

    return true;
}
}