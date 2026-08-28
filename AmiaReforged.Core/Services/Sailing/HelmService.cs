using AmiaReforged.Core.Models.Sailing; using Anvil.API; using Anvil.API.Events; using Anvil.Services; using NLog;
namespace AmiaReforged.Core.Services.Sailing;
[ServiceBinding(typeof(HelmService))] public class HelmService { private const string StartingAreaResRef = "ocean_01";
private const float SailingStep = 1.0f;
private const float AreaMaxX = 640.0f;
private const float AreaMaxY = 640.0f;
//private const bool PersistShips = false;

private const float BoundaryEntryOffset = 5.0f;
private const int RepairAmount = 25;
private const int MaxHull = 100;
private readonly Dictionary<string, string> _helmShips = new()
{
    ["sailing_helm"] = "Sea Sprite",
    ["sailing_helm_black_pearl"] = "Black Pearl"
};

private readonly Dictionary<string, string> _shipPlaceableTags = new()
{
    ["Sea Sprite"] = "sea_sprite",
    ["Black Pearl"] = "black_pearl",
 
};

private readonly Dictionary<string, ShipState> _ships = new();

private readonly Dictionary<string, string> _playerShips = new();

private readonly SailingAreaService _sailingAreaService;
private readonly MerchantTradeService _merchantTradeService;
private readonly MerchantPortService _merchantPortService;
private readonly Dictionary<string, NuiWindowToken>
    _merchantTradeTokens =
        new(StringComparer.Ordinal);
    private readonly SailingNuiService _sailingNuiService;
private const string MerchantTradeWindowId =
    "merchant_trade";

private readonly NuiBind<string>
    _tradePlayerGoldBind =
        new("trade_player_gold");

private readonly NuiBind<string>
    _tradePlayerCargoBind =
        new("trade_player_cargo");

private readonly NuiBind<string>
    _tradeMerchantGoldBind =
        new("trade_merchant_gold");

private readonly NuiBind<string>
    _tradeMerchantCargoBind =
        new("trade_merchant_cargo");
    private readonly IslandService
    _islandService;
private readonly ShipStatePersistenceService
_shipStatePersistenceService;

private readonly ShipEncounterService
    _shipEncounterService;

private readonly ShipBoardingService
    _shipBoardingService;

private readonly ShipCombatService
    _shipCombatService;

private readonly HorizonContactService
    _horizonContactService;

    private readonly ShipNavigationService
    _shipNavigationService;

private readonly ShipObstacleService
    _shipObstacleService;

private readonly OceanContactService
    _oceanContactService;

private readonly PhysicalShipService
_physicalShipService;

private readonly ChartDiscoveryService _chartDiscoveryService;

    private readonly PirateAiService _pirateAiService;

    private readonly ShipCrewService
    _shipCrewService;   
private static readonly Logger Log =
    LogManager.GetCurrentClassLogger();

public HelmService(
    SailingNuiService sailingNuiService,
    ShipStatePersistenceService shipStatePersistenceService,
    ShipEncounterService shipEncounterService,
    ShipBoardingService shipBoardingService,
    ShipCombatService shipCombatService,
    ShipNavigationService shipNavigationService,
    ShipObstacleService shipObstacleService,
    ShipCrewService shipCrewService,
    ShipRoutePlannerService shipRoutePlannerService,
    HorizonContactService horizonContactService,
    OceanContactService oceanContactService,
    PirateAiService pirateAiService,
    IslandService islandService,
    PhysicalShipService physicalShipService,
    ChartDiscoveryService chartDiscoveryService,
    MerchantPortService merchantPortService,
    MerchantTradeService merchantTradeService,
    SailingAreaService sailingAreaService)
    {
        _shipRoutePlannerService =
        shipRoutePlannerService;

        _sailingAreaService =
    sailingAreaService;

        _sailingNuiService =
            sailingNuiService;

    _shipStatePersistenceService =
        shipStatePersistenceService;

    _shipEncounterService =
        shipEncounterService;

    _shipBoardingService =
        shipBoardingService;

    _shipCombatService =
        shipCombatService;

    _shipNavigationService =
    shipNavigationService;

    _shipObstacleService =
    shipObstacleService;

    _shipCrewService =
    shipCrewService;

    _horizonContactService =
    horizonContactService;

    _oceanContactService =
    oceanContactService;

    _islandService =
    islandService;

    _physicalShipService =
    physicalShipService;

    _chartDiscoveryService = chartDiscoveryService;

    _merchantPortService = merchantPortService;

        _pirateAiService =
    pirateAiService;

    _merchantTradeService =
    merchantTradeService;

        _shipEncounterService.EncounterStarted +=
    HandleShipEncounterStarted;

_shipEncounterService.EncounterEnded +=
    HandleShipEncounterEnded;
    
        _shipBoardingService.BoardingCompleted -=
    HandleBoardingCompleted;

_shipBoardingService.BoardingCompleted +=
    HandleBoardingCompleted;
foreach (string helmTag in _helmShips.Keys)
{
    foreach (NwPlaceable helm in NwObject.FindObjectsWithTag<NwPlaceable>(helmTag))
    {
   helm.OnLeftClick -= HandleHelmClick;
   helm.OnLeftClick += HandleHelmClick;
            }
}
foreach (NwPlaceable boardingPoint in
    NwObject.FindObjectsWithTag<NwPlaceable>(
        "ship_boarding_point"))
{
boardingPoint.OnLeftClick -=
    HandleBoardingPointClick;

boardingPoint.OnLeftClick +=
    HandleBoardingPointClick;
        }
      foreach (NwPlaceable boardTest in
    NwObject.FindObjectsWithTag<NwPlaceable>("board_test"))
{
    boardTest.OnLeftClick -= HandleBoardTestClick;
    boardTest.OnLeftClick += HandleBoardTestClick;
}  
    }    


public void RegisterShipDefinition(ShipDefinition definition)
{
    _helmShips[definition.HelmTag] =
        definition.ShipName;

    _shipPlaceableTags[definition.ShipName] =
        definition.PlaceableTag;

    Log.Info(
        $"Registered helm '{definition.HelmTag}' for {definition.ShipName}.");
}

private readonly ShipRoutePlannerService
    _shipRoutePlannerService;


    private void HandleHelmClick(
    PlaceableEvents.OnLeftClick obj)
{
    Log.Info(
        $"Sailing helm clicked: " +
        $"Tag={obj.Placeable.Tag}, " +
        $"ResRef={obj.Placeable.ResRef}");

    if (!_helmShips.TryGetValue(
            obj.Placeable.Tag,
            out string? shipName))
    {
        Log.Warn(
            $"No ship configured for sailing helm " +
            $"tag '{obj.Placeable.Tag}'.");

        return;
    }

    NwPlayer player = obj.ClickedBy;

    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        player.SendServerMessage(
            $"The {shipName} is not currently available.");

        Log.Warn(
            $"Player {player.PlayerName} attempted to take " +
            $"the helm of '{shipName}', but the ship does not exist.");

        return;
    }

    bool tookHelm = TakeHelm(
        shipName,
        player.PlayerName);

if (tookHelm)
{
    _playerShips[player.PlayerName] =
        shipName;


            if (!_shipCrewService.SetCaptain(
            shipName,
            player))
    {
        Log.Warn(
            $"Player {player.PlayerName} took the helm of " +
            $"'{shipName}', but could not be assigned " +
            $"the Captain role because they are not aboard.");

        LeaveHelm(
            shipName);

        player.SendServerMessage(
            "You must be aboard the ship to take the helm.");

        return;
    }

    player.SendServerMessage(
        $"You take the helm of the {shipName}.");

    Log.Info(
        $"Player {player.PlayerName} " +
        $"took the helm of the {shipName}.");

        
    player.OnNuiEvent -=
        HandleSailingNuiEvent;

     player.OnNuiEvent +=
        HandleSailingNuiEvent;

    _sailingNuiService.Open(
        player,
        ship);
}
    else
    {
        player.SendServerMessage(
            "Someone is already at the helm.");

        Log.Info(
            $"Player {player.PlayerName} attempted to take " +
            $"the helm of the {shipName}, " +
            $"but it was occupied.");
    }
}
private async void HandleBoardTestClick(
    PlaceableEvents.OnLeftClick obj)
{
    NwArea? deck =
        NwModule.Instance.Areas.FirstOrDefault(
            a => a.ResRef == "sea_sprite_d2");

    if (deck == null)
    {
        Log.Error("Board test failed: sea_sprite_d2 not found.");
        return;
    }

    await NwTask.NextFrame();

    if (obj.ClickedBy.ControlledCreature == null ||
        !obj.ClickedBy.ControlledCreature.IsValid)
    {
        return;
    }

    obj.ClickedBy.ControlledCreature.Location =
        Location.Create(
            deck,
            new System.Numerics.Vector3(42f, 42f, 0f),
            0f);
}
    private async void HandleBoardingPointClick(
    PlaceableEvents.OnLeftClick obj)
{
    NwPlayer player = obj.ClickedBy;

    string shipName =
        player.LoginCreature
            .GetObjectVariable<LocalVariableString>(
                "SAILING_DOCKED_SHIP")
            .Value;

    if (string.IsNullOrWhiteSpace(shipName))
    {
        player.SendServerMessage(
            "You do not have a ship anchored offshore.");

        return;
    }

    NwArea? deck =
        NwModule.Instance.Areas.FirstOrDefault(
            x => x.ResRef == "sea_sprite_d2");

    if (deck == null)
    {
        Log.Error("Boarding failed: sea_sprite_d not found.");
        return;
    }

    // Let the click event finish completely.
    await NwTask.NextFrame();

    if (player.ControlledCreature == null ||
        !player.ControlledCreature.IsValid)
    {
        return;
    }

    player.ControlledCreature.Location =
        Location.Create(
            deck,
            new System.Numerics.Vector3(43f, 43f, 0f),
            0f);

    _physicalShipService.AddPlayerAboard(
        shipName,
        player);

    Log.Info(
        $"Boarded {shipName}: Player={player.PlayerName}");
}
    private static void NotifyHelmsman(
    ShipState ship,
    string message)
{
    if (string.IsNullOrEmpty(
            ship.HelmsmanPCKey))
    {
        return;
    }

    NwPlayer? player =
        NwModule.Instance.Players.FirstOrDefault(
            p => p.PlayerName ==
                 ship.HelmsmanPCKey);

    player?.SendServerMessage(message);
}

private bool UpdateDockingState(
    ShipState ship)
{
    bool previousCanDock =
        ship.CanDock;

    string? previousIsland =
        ship.NearbyIslandId;

    IslandLocation? island =
        _islandService.GetNearestIsland(ship);

    ship.CanDock = false;
    ship.NearbyIslandId = null;

    if (island == null)
    {
        return previousCanDock != ship.CanDock ||
               previousIsland != ship.NearbyIslandId;
    }

    float dx = island.OceanX - ship.X;
    float dy = island.OceanY - ship.Y;

    float distance =
        MathF.Sqrt(dx * dx + dy * dy);

    if (distance <= island.DockRadius)
    {
        ship.CanDock = true;
        ship.NearbyIslandId = island.Id;

        Log.Info(
            $"Ship '{ship.ShipName}' entered docking range of {island.Name}.");
    }

    return previousCanDock != ship.CanDock ||
           previousIsland != ship.NearbyIslandId;
}
private static string GetRelativeBearing(
        ShipState ship,
    OceanContact contact)
{
    float deltaX =
        contact.X - ship.X;

    float deltaY =
        contact.Y - ship.Y;

    float worldAngle =
        MathF.Atan2(
            deltaY,
            deltaX);

    float shipAngle =
        ship.Heading switch
        {
            Heading.East => 0f,
            Heading.NorthEast => MathF.PI / 4f,
            Heading.North => MathF.PI / 2f,
            Heading.NorthWest => 3f * MathF.PI / 4f,
            Heading.West => MathF.PI,
            Heading.SouthWest => 5f * MathF.PI / 4f,
            Heading.South => 3f * MathF.PI / 2f,
            Heading.SouthEast => 7f * MathF.PI / 4f,
            _ => 0f,
        };

    float relative =
        worldAngle - shipAngle;

    while (relative > MathF.PI)
    {
        relative -=
            2f * MathF.PI;
    }

    while (relative < -MathF.PI)
    {
        relative +=
            2f * MathF.PI;
    }

    float degrees =
        relative * 180f / MathF.PI;

    if (MathF.Abs(degrees) <= 15f)
    {
        return "bow";
    }

    return degrees > 0f
        ? "port bow"
        : "starboard bow";
}
    private void HandleSailingNuiEvent(
    ModuleEvents.OnNuiEvent obj)
{
    Log.Info(
        $"Sailing NUI event: " +
        $"Player={obj.Player.PlayerName}, " +
        $"Event={obj.EventType}, " +
        $"Element={obj.ElementId}");

    if (!_playerShips.TryGetValue(
            obj.Player.PlayerName,
            out string? shipName))
    {
        Log.Warn(
            $"Received sailing NUI event from player " +
            $"'{obj.Player.PlayerName}', but they are not " +
            $"assigned to a ship.");

        return;
        }
        ShipState? ship =
    GetShip(shipName);

if (ship == null)
{
    Log.Warn(
        $"Received sailing NUI event for ship '{shipName}', " +
        "but the ship no longer exists.");

    return;
}

        if (obj.EventType == NuiEventType.MouseUp)
{
    switch (obj.ElementId)
    {
        case "ahead_button":
            ship.Underway = true;

            Log.Info(
                $"Ship '{shipName}' underway.");

            UpdateSailingNui(ship);
            break;

        case "stop_button":
            ship.Underway = false;

            Log.Info(
                $"Ship '{shipName}' stopped.");

            UpdateSailingNui(ship);
            break;

        case "astern_button":
            MoveAstern(
                shipName,
                obj.Player);
            break;

            case "left_button":
                TurnLeft(
                    shipName);
                break;

            case "right_button":
                TurnRight(
                    shipName);
                    break;

                 case "test_navigation_button":
                 TestNavigation(
                 shipName);
                break;

                case "hail_button":
                HailTarget(
                    shipName,
                    obj.Player);
                break;
case "trade_button":
    TradeWithTarget(
        shipName,
        obj.Player);
                    break;
    case "merchant_trade_buy_1":
    BuyFromMerchant(
        obj.Player,
        1);
       break;
case "merchant_trade_sell_1":
    SellToMerchant(
        obj.Player,
        1);
    break;
                    
                case "dock_button":
            DockShip(
                shipName,
                obj.Player);
            return;

                case "board_button":
                RequestBoarding(
                    shipName,
                    obj.Player);
                break;

            case "accept_board_button":
                AcceptBoarding(
                    shipName,
                    obj.Player);
                break;

            case "reject_board_button":
                RejectBoarding(
                    shipName,
                    obj.Player);
                break;

          case "attack_button":
             _ = AttackTarget(
             shipName,
            obj.Player);
             break;

                case "repair_button":
                _ = RepairShip(
                  shipName,
               obj.Player);
                break;



                case "weapon_cannon_button":
                    _ = EquipWeapon(
                         shipName,
                         obj.Player,
                        "ship_cannon");
                    break;

                case "weapon_ballista_button":
                    _ = EquipWeapon(
                        shipName,
                        obj.Player,
                        "ship_ballista");
                    break;

                case "weapon_catapult_button":
                    _ = EquipWeapon(
                     shipName,
                      obj.Player,
                     "ship_catapult");
                break;

            case "weapon_heavy_cannon_button":
            _ = EquipWeapon(
                shipName,
                    obj.Player,
                "ship_heavy_cannon");
                 break;
            }

_chartDiscoveryService.RevealAroundShip(
    obj.Player,
    ship);

            _sailingNuiService.Update(
    obj.Player,
    ship,
    _ships.Values);
    }

    if (obj.EventType == NuiEventType.MouseUp &&
        obj.ElementId == "leave_button")
    {
        LeaveHelm(shipName);
        ship.Underway = false;
        _playerShips.Remove(
            obj.Player.PlayerName);

        obj.Player.SendServerMessage(
            $"You leave the helm of the {shipName}.");

        _sailingNuiService.Close(
            obj.Player);

        obj.Player.OnNuiEvent -=
            HandleSailingNuiEvent;

        Log.Info(
            $"Player {obj.Player.PlayerName} " +
            $"left the helm of the {shipName}.");

        return;
    }

    if (obj.EventType == NuiEventType.Close &&
    obj.ElementId == "_window_")
{
    if (ship.IsDocking)
    {
        Log.Info(
            $"Ignoring NUI Close event for '{shipName}' because the ship is docking.");

        return;
    }
        ship =
        GetShip(shipName);

        if (ship != null &&
            ship.HelmsmanPCKey ==
            obj.Player.PlayerName)
        {
            LeaveHelm(shipName);

            obj.Player.SendServerMessage(
                $"You leave the helm of the {shipName}.");

            Log.Info(
                $"Player {obj.Player.PlayerName} " +
                $"closed the sailing window " +
                $"and left the helm of the {shipName}.");
        }

        _playerShips.Remove(
            obj.Player.PlayerName);

        _sailingNuiService.Close(
            obj.Player);

        obj.Player.OnNuiEvent -=
            HandleSailingNuiEvent;
    }
}

private async Task AttackTarget(
    string shipName,
    NwPlayer player)
{
    string playerName =
        player.PlayerName;

    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        Log.Warn(
            $"Cannot attack from ship '{shipName}': " +
            "ship does not exist.");

        player.SendServerMessage(
            "Your ship could not be found.");

        return;
    }

    try
    {
ShipCombatService.ShipAttackResult result =
    await _shipCombatService.TryAttack(
        ship,
        player);

        // ShipCombatService performs asynchronous database work.
        // Return to the NWN main thread before touching
        // NwPlayer, NwModule, or other native NWN API.

        await NwTask.SwitchToMainThread();

        // -----------------------------------------------------------------
        // Cooldown
        // -----------------------------------------------------------------

        if (result.IsCooldown)
        {
            string message =
                $"⚓ WEAPON RELOADING\n" +
                $"{result.Weapon.DisplayName}\n" +
                $"Ready in " +
                $"{result.CooldownRemaining.TotalSeconds:0.0}s";

            player.SendServerMessage(
                $"Weapons reloading. " +
                $"{result.CooldownRemaining.TotalSeconds:0.0} " +
                "seconds remaining.");

            _sailingNuiService.ShowCombatMessage(
                player,
                message);

            Log.Info(
                $"Player {playerName} attempted to attack " +
                $"from '{ship.ShipName}', but weapons are " +
                $"reloading. " +
                $"Remaining={result.CooldownRemaining.TotalSeconds:0.00}s.");

            return;
        }

        // -----------------------------------------------------------------
        // No target
        // -----------------------------------------------------------------

        if (result.NoTargetFound)
        {
            const string message =
                "⚔ NO TARGET\n" +
                "There is no valid ship target.";

            player.SendServerMessage(
                "There is no valid ship target to attack.");

            _sailingNuiService.ShowCombatMessage(
                player,
                message);

            Log.Info(
                $"Player {playerName} attempted to attack " +
                $"from '{ship.ShipName}', but no valid target exists.");

            return;
        }

        // -----------------------------------------------------------------
        // Out of range
        // -----------------------------------------------------------------

        if (result.IsOutOfRange)
        {
            string message =
                $"⚔ OUT OF RANGE\n" +
                $"Target distance: {result.Distance:0.0}\n" +
                $"Weapon range: {result.Weapon.MaxRange:0.0}";

            player.SendServerMessage(
                $"The target is out of range. " +
                $"Distance: {result.Distance:0.0} " +
                $" / Range: {result.Weapon.MaxRange:0.0}.");

            _sailingNuiService.ShowCombatMessage(
                player,
                message);

            Log.Info(
                $"Player {playerName} attempted to attack " +
                $"from '{ship.ShipName}', but the target was " +
                $"out of weapon range. " +
                $"Distance={result.Distance:0.00}, " +
                $"Range={result.Weapon.MaxRange:0.00}.");

            return;
        }

        // -----------------------------------------------------------------
        // Outside firing arc
        // -----------------------------------------------------------------

        if (result.IsOutOfArc)
        {
            string message =
                $"⚔ OUT OF ARC\n" +
                $"{result.Weapon.DisplayName}\n" +
                $"Arc: {result.Weapon.Arc}\n" +
                $"Change heading to bring the target into the " +
                $"firing arc.";

            player.SendServerMessage(
                $"The {result.Weapon.DisplayName} " +
                "cannot fire at that target from your current heading.");

            _sailingNuiService.ShowCombatMessage(
                player,
                message);

            Log.Info(
                $"Player {playerName} attempted to attack " +
                $"from '{ship.ShipName}', but the target was " +
                $"outside the weapon firing arc. " +
                $"Weapon={result.Weapon.DisplayName}, " +
                $"Arc={result.Weapon.Arc}.");

            return;
        }

        // -----------------------------------------------------------------
        // Disabled target
        // -----------------------------------------------------------------

        if (result.TargetDisabled ||
            result.TargetShip == null)
        {
            if (result.TargetShip != null)
            {
                string message =
                    $"⚠ TARGET DISABLED\n" +
                    $"{result.TargetShip.ShipName}\n" +
                    "This ship can no longer be attacked.";

                player.SendServerMessage(
                    $"The {result.TargetShip.ShipName} " +
                    "is already disabled.");

                _sailingNuiService.ShowCombatMessage(
                    player,
                    message);
            }
            else
            {
                const string message =
                    "⚠ NO VALID TARGET\n" +
                    "There is no valid ship target.";

                player.SendServerMessage(
                    "There is no valid ship target to attack.");

                _sailingNuiService.ShowCombatMessage(
                    player,
                    message);
            }

            return;
        }

        // -----------------------------------------------------------------
        // Successful attack
        // -----------------------------------------------------------------

        ShipState targetShip =
            result.TargetShip;

        string combatMessage =
            $"⚔ HIT — {targetShip.ShipName}\n" +
            $"{result.Weapon.DisplayName}\n" +
            $"Damage: {result.Damage}\n" +
            $"Hull: {result.PreviousHull}% → " +
            $"{targetShip.Hull}%";

        player.SendServerMessage(
            $"You fire upon the {targetShip.ShipName}.");

        player.SendServerMessage(
            $"{targetShip.ShipName} hull: " +
            $"{targetShip.Hull}%.");

        _sailingNuiService.ShowCombatMessage(
            player,
            combatMessage);

        Log.Info(
            $"Player {playerName} attacked " +
            $"'{targetShip.ShipName}' from " +
            $"'{ship.ShipName}'. " +
            $"Weapon={result.Weapon.DisplayName}, " +
            $"Damage={result.Damage}, " +
            $"Hull={result.PreviousHull}->{targetShip.Hull}.");

        // -----------------------------------------------------------------
        // Target disabled
        // -----------------------------------------------------------------

        if (targetShip.Hull <= 0)
        {
            string disabledMessage =
                $"☠ SHIP DISABLED\n" +
                $"{targetShip.ShipName}\n" +
                "Hull integrity has reached 0%.";

            player.SendServerMessage(
                $"The {targetShip.ShipName} has been disabled!");

            _sailingNuiService.ShowCombatMessage(
                player,
                disabledMessage);

            string? targetPlayerKey =
                targetShip.HelmsmanPCKey;

            if (!string.IsNullOrWhiteSpace(
                    targetPlayerKey))
            {
                NwPlayer? targetPlayer =
                    NwModule.Instance.Players.FirstOrDefault(
                        p =>
                            string.Equals(
                                p.PlayerName,
                                targetPlayerKey,
                                StringComparison.Ordinal));

                if (targetPlayer != null)
                {
                    targetPlayer.SendServerMessage(
                        $"Your ship, the {targetShip.ShipName}, " +
                        "has been disabled!");

                    _sailingNuiService.ShowCombatMessage(
                        targetPlayer,
                        $"☠ YOUR SHIP IS DISABLED\n" +
                        $"{targetShip.ShipName}\n" +
                        "Hull integrity: 0%");
                }
            }

            Log.Info(
                $"Ship '{targetShip.ShipName}' has been disabled " +
                $"by '{ship.ShipName}'.");
        }

        // -----------------------------------------------------------------
        // Refresh attacker's NUI
        // -----------------------------------------------------------------

_sailingNuiService.Update(
    player,
    ship,
    _ships.Values);
    }
    catch (Exception ex)
    {
        Log.Error(
            ex,
            $"Error while attacking from ship '{shipName}' " +
            $"for player '{playerName}'.");

        _sailingNuiService.ShowCombatMessage(
            player,
            "⚠ COMBAT ERROR\n" +
            "The attack could not be completed.");
    }
}
private async Task EquipWeapon(
    string shipName,
    NwPlayer player,
    string weaponResRef)
{
    string playerName =
        player.PlayerName;

    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        Log.Warn(
            $"Cannot equip weapon on ship '{shipName}': " +
            "ship does not exist.");

        player.SendServerMessage(
            "Your ship could not be found.");

        return;
    }

    ShipWeapon? weapon =
        _shipCombatService
            .GetAvailableWeapons()
            .FirstOrDefault(
                x =>
                    string.Equals(
                        x.ResRef,
                        weaponResRef,
                        StringComparison.OrdinalIgnoreCase));

    if (weapon == null)
    {
        Log.Warn(
            $"Player {playerName} attempted to equip " +
            $"unknown weapon '{weaponResRef}' " +
            $"on ship '{shipName}'.");

        await NwTask.SwitchToMainThread();

        player.SendServerMessage(
            "That weapon is not available.");

        return;
    }

    if (!_shipCombatService.TryEquipWeapon(
            ship,
            weapon.ResRef))
    {
        await NwTask.SwitchToMainThread();

        player.SendServerMessage(
            "That weapon could not be equipped.");

        return;
    }

    await _shipStatePersistenceService.SaveState(
        ship);

    await NwTask.SwitchToMainThread();

    player.SendServerMessage(
        $"You equip the {weapon.DisplayName}.");

    Log.Info(
        $"Player {playerName} equipped " +
        $"'{weapon.DisplayName}' on " +
        $"ship '{ship.ShipName}'.");

_sailingNuiService.Update(
    player,
    ship,
    _ships.Values);
}

private ShipState SpawnContactAsShip(
    OceanContact contact)
{
 ShipState ship = new()
{
    ShipName = contact.Name,

    DeckAreaResRef = contact.Type switch
    {
        EncounterType.Pirate => "pirate_brig_d",
        EncounterType.Merchant => "merchant_ship_d",
        _ => "sea_sprite_d2"
    },

    ShipType = ShipType.Player, // keep them all as Player

    AreaResRef = contact.AreaResRef,
    X = contact.X,
    Y = contact.Y,
    Z = 0.0f,
    Heading = Heading.West,
    Hull = 100,
    Underway = true,
    HelmsmanPCKey = null,

    SpritePrefix = contact.ShipResRef switch
    {
        "stormrunner" => "brig",
        "black_pearl" => "galleon",
        "golden_gull" => "cog",
        "sea_sprite" => "sloop",
        _ => "sloop"
    }
};
    _ships[ship.ShipName] = ship;

// Register this ship's physical placeable tag.
_shipPlaceableTags[ship.ShipName] = contact.ShipTag;

// Spawn or update its physical representation.
SpawnOrUpdatePhysicalShip(
    ship,
    contact.ShipResRef,
    contact.ShipTag);

    Log.Info(
        $"Spawned NPC ship '{ship.ShipName}' at " +
        $"{ship.AreaResRef} ({ship.X:0.0}, {ship.Y:0.0}).");

    return ship;
}
    //boarding check

    //docking
    private async void DockShip(
    string shipName,
    NwPlayer player)
{
    ShipState? ship =
    GetShip(shipName);

if (ship == null)
{
    return;
}

if (ship.IsDocking)
{
    Log.Info(
        $"Dock request ignored for '{shipName}' because it is already docking.");

    return;
}

ship.IsDocking = true;

    if (!ship.CanDock ||
        string.IsNullOrWhiteSpace(ship.NearbyIslandId))
    {
        player.SendServerMessage(
            "You are not close enough to dock.");

        return;
    }

    IslandLocation? island =
        _islandService.GetNearestIsland(ship);

    if (island == null ||
        !string.Equals(
            island.Id,
            ship.NearbyIslandId,
            StringComparison.Ordinal))
    {
        player.SendServerMessage(
            "Unable to locate a suitable landing.");

        return;
    }

    NwArea? landingArea =
        NwModule.Instance.Areas.FirstOrDefault(
            area =>
                area.ResRef ==
                island.LandingArea);

    if (landingArea == null)
    {
        player.SendServerMessage(
            "The landing area could not be found.");

        Log.Error(
            $"Landing area '{island.LandingArea}' was not found.");

        return;
    }

    //ship.Underway = false;
    //ship.HelmsmanPCKey = null;
    Location landingLocation =
        Location.Create(
            landingArea,
            new System.Numerics.Vector3(
                island.LandingX,
                island.LandingY,
                island.LandingZ),
            0.0f);
        player.LoginCreature.GetObjectVariable<LocalVariableString>(
        "SAILING_DOCKED_SHIP").Value = ship.ShipName;

ship.Underway = false;
ship.HelmsmanPCKey = null;

    _sailingNuiService.Close(player);

// belt-and-suspenders: remove any cached token
   // _tokens.Remove(player.PlayerName);

    _physicalShipService.RemovePlayerAboard(
    ship.ShipName,
    player);
// Let the NUI fully close before changing areas.
    try
{
    await NwTask.NextFrame();

    player.ControlledCreature.Location =
        landingLocation;

    player.SendServerMessage(
        $"You make landfall at {island.Name}.");

    Log.Info(
        $"Ship '{ship.ShipName}' docked at {island.Name}.");
}
finally
{
    ship.IsDocking = false;
}
}
//boarding
    private void BoardShip(
    string shipName,
    NwPlayer? player)
{
    if (player == null)
    {
        return;
    }

    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        player.SendServerMessage(
            "Your ship could not be found.");

        return;
    }

    NwArea? shipArea =
        NwModule.Instance.Areas.FirstOrDefault(
            area => area.ResRef == ship.DeckAreaResRef);

    if (shipArea == null)
    {
        player.SendServerMessage(
            "Your ship's deck could not be found.");

        Log.Error(
            $"Ship deck area '{ship.ShipName}' was not found.");

        return;
    }

    Location boardingLocation =
        Location.Create(
            shipArea,
            new System.Numerics.Vector3(
                12f,
                8f,
                0f),
            0.0f);

    player.ControlledCreature.Location =
    boardingLocation;

player.SendServerMessage(
    $"You board the {ship.ShipName}.");

Log.Info(
    $"Player '{player.PlayerName}' boarded '{ship.ShipName}'.");

player.LoginCreature
    .GetObjectVariable<LocalVariableString>(
        "SAILING_DOCKED_SHIP")
    .Delete();
}
    //hail
    private void HailTarget(
    string shipName,
    NwPlayer player)
{
    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        Log.Warn(
            $"Cannot hail from ship '{shipName}': " +
            $"ship does not exist.");

        return;
    }

    // -------------------------------------------------------------
    // Ocean contact hailing
    // -------------------------------------------------------------

    OceanContact? contact =
        _oceanContactService.GetClosestContact(ship);

    if (contact != null)
    {
        float distance =
            MathF.Sqrt(
                MathF.Pow(contact.X - ship.X, 2) +
                MathF.Pow(contact.Y - ship.Y, 2));

        if (distance <= OceanContactService.AttackRange)
        {
            switch (contact.Type)
            {
                case EncounterType.Pirate:

                    player.SendServerMessage(
                        "The pirate raises black colors and ignores your hail.");

                    return;

                case EncounterType.Merchant:

                    player.SendServerMessage(
                        "The merchant acknowledges your signal.");

                    return;

                case EncounterType.Wreck:

                    player.SendServerMessage(
                        "The wreck offers no reply.");

                    return;
            }
        }
    }

    // -------------------------------------------------------------
    // Existing ship-to-ship hailing
    // -------------------------------------------------------------

    if (!_shipEncounterService.TryGetEncounter(
            ship,
            out ShipEncounter? encounter) ||
        encounter == null)
    {
        player.SendServerMessage(
            "There is no ship close enough to hail.");

        Log.Info(
            $"Player {player.PlayerName} attempted to hail " +
            $"from '{shipName}', but there is no active encounter.");

        return;
    }

    ShipState targetShip;

if (ReferenceEquals(
        encounter.ShipA,
        ship))
{
    targetShip =
        encounter.ShipB;
}
else
{
    targetShip =
        encounter.ShipA;
}

// -------------------------------------------------------------
// Merchant ship
// -------------------------------------------------------------

if (targetShip.ShipType == ShipType.Merchant)
{
    player.SendServerMessage(
        $"You hail the {targetShip.ShipName}.");

    player.SendServerMessage(
        $"The {targetShip.ShipName} acknowledges your signal.");

    Log.Info(
        $"Merchant hail: " +
        $"Player={player.PlayerName}, " +
        $"Ship={ship.ShipName}, " +
        $"Merchant={targetShip.ShipName}, " +
        $"Distance={encounter.Distance:0.00}");

    _sailingNuiService.Update(
        player,
        ship,
        _ships.Values);

    return;
}

// -------------------------------------------------------------
// Existing player-to-player hailing
// -------------------------------------------------------------

// Message to the player who clicked HAIL.
player.SendServerMessage(
    $"You hail the {targetShip.ShipName}.");

// The target ship's current helmsman.
string? targetPlayerKey =
    targetShip.HelmsmanPCKey;

if (!string.IsNullOrWhiteSpace(
        targetPlayerKey))
{
    NwPlayer? targetPlayer =
        NwModule.Instance.Players.FirstOrDefault(
            p => string.Equals(
                p.PlayerName,
                targetPlayerKey,
                StringComparison.Ordinal));

    if (targetPlayer != null)
    {
        targetPlayer.SendServerMessage(
            $"The {ship.ShipName} is hailing you.");

       Log.Info(
    $"Ship hail delivered: " +
    $"Player={targetPlayer.PlayerName}, " +
    $"Ship={targetShip.ShipName}");
    }
    else
    {
        Log.Warn(
            $"Could not find online player " +
            $"'{targetPlayerKey}' at the helm of " +
            $"'{targetShip.ShipName}'.");
    }
}
else
{
    Log.Info(
        $"Ship '{targetShip.ShipName}' has no helmsman. " +
        $"Hail from '{ship.ShipName}' was not delivered.");
}

Log.Info(
    $"Ship hail: " +
    $"{ship.ShipName} -> " +
    $"{targetShip.ShipName}, " +
    $"Area={encounter.AreaResRef}, " +
    $"Distance={encounter.Distance:0.00}");
}



    private void RequestBoarding(
    string shipName,
    NwPlayer player)
{
    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        return;
    }

    if (_shipBoardingService.TryRequestBoarding(
            ship,
            player.PlayerName,
            out ShipBoardingRequest? request) &&
        request != null)
    {
        player.SendServerMessage(
            $"You request permission to board the " +
            $"{request.TargetShip.ShipName}.");

        Log.Info(
            $"Player {player.PlayerName} requested boarding: " +
            $"{request.RequestingShip.ShipName} -> " +
            $"{request.TargetShip.ShipName}.");

   _sailingNuiService.Update(
    player,
    ship,
    _ships.Values);

        NwPlayer? targetPlayer =
            NwModule.Instance.Players.FirstOrDefault(
                p =>
                    string.Equals(
                        p.PlayerName,
                        request.TargetPlayerName,
                        StringComparison.Ordinal));

        if (targetPlayer != null)
        {
            ShipState? targetShip =
                GetShip(
                    request.TargetShip.ShipName);

            if (targetShip != null)
            {
              _sailingNuiService.Update(
    targetPlayer,
    targetShip,
    _ships.Values);
            }
        }

        return;
    }

    player.SendServerMessage(
        "You cannot request boarding right now.");

    Log.Info(
        $"Player {player.PlayerName} could not request " +
        $"boarding from '{shipName}'.");
}

private void AcceptBoarding(
    string shipName,
    NwPlayer player)
{
    if (_shipBoardingService.TryAcceptBoarding(
            player.PlayerName,
            out ShipBoardingRequest? request) &&
        request != null)
    {
        player.SendServerMessage(
            $"You allow the {request.RequestingShip.ShipName} " +
            $"to board the {request.TargetShip.ShipName}.");

        ShipState? ship =
            GetShip(shipName);

        if (ship != null)
        {
        _sailingNuiService.Update(
    player,
    ship,
    _ships.Values);
        }

        Log.Info(
            $"Player {player.PlayerName} accepted boarding " +
            $"from '{request.RequestingShip.ShipName}'.");
    }
    else
    {
        player.SendServerMessage(
            "There is no valid boarding request to accept.");
    }
}

private void RejectBoarding(
    string shipName,
    NwPlayer player)
{
    if (_shipBoardingService.TryRejectBoarding(
            player.PlayerName,
            out ShipBoardingRequest? request) &&
        request != null)
    {
        ShipState? ship =
            GetShip(shipName);

        if (ship != null)
        {
       _sailingNuiService.Update(
    player,
    ship,
    _ships.Values);
        }

        NwPlayer? requestingPlayer =
            NwModule.Instance.Players.FirstOrDefault(
                p =>
                    string.Equals(
                        p.PlayerName,
                        request.RequestingPlayerName,
                        StringComparison.Ordinal));

        if (requestingPlayer != null)
        {
            ShipState? requestingShip =
                GetShip(
                    request.RequestingShip.ShipName);

            if (requestingShip != null)
            {
                _sailingNuiService.Update(
                    requestingPlayer,
                    requestingShip);
            }
        }

        Log.Info(
            $"Player {player.PlayerName} rejected boarding " +
            $"from '{request.RequestingShip.ShipName}'.");
    }
    else
    {
        player.SendServerMessage(
            "There is no boarding request to reject.");
    }
}

private void HandleBoardingCompleted(
    ShipBoardingRequest request)
{
    NwPlayer? requestingPlayer =
        NwModule.Instance.Players.FirstOrDefault(
            p =>
                string.Equals(
                    p.PlayerName,
                    request.RequestingPlayerName,
                    StringComparison.Ordinal));

    if (requestingPlayer == null)
    {
        return;
    }

    _playerShips.Remove(
        requestingPlayer.PlayerName);

    _sailingNuiService.Close(
        requestingPlayer);

    requestingPlayer.OnNuiEvent -=
        HandleSailingNuiEvent;

    requestingPlayer.SendServerMessage(
        $"You are now aboard the " +
        $"{request.TargetShip.ShipName}.");

    ShipState? targetShip =
        GetShip(
            request.TargetShip.ShipName);

    NwPlayer? targetPlayer =
        NwModule.Instance.Players.FirstOrDefault(
            p =>
                string.Equals(
                    p.PlayerName,
                    request.TargetPlayerName,
                    StringComparison.Ordinal));

    if (targetPlayer != null &&
        targetShip != null)
    {
   _sailingNuiService.Update(
    targetPlayer,
    targetShip,
    _ships.Values);
    }

    Log.Info(
        $"Boarding completed: " +
        $"{request.RequestingPlayerName} is now aboard " +
        $"{request.TargetShip.ShipName}.");
}

public ShipState CreateShip(ShipDefinition definition)
{
    ShipState ship = new()
    {
        ShipName = definition.ShipName,
        SpritePrefix = definition.SpritePrefix,
        DeckAreaResRef = definition.DeckAreaResRef,
        AreaResRef = definition.OceanAreaResRef,
        ShipType = definition.ShipType,
        X = definition.X,
        Y = definition.Y,
        Z = definition.Z,
        Heading = definition.Heading,
        Hull = definition.Hull,
        CargoCapacity = definition.CargoCapacity,
        WeaponResRef = definition.WeaponResRef
    };

    _ships[definition.ShipName] = ship;

    Log.Info(
        $"Ship '{definition.ShipName}' created: " +
        $"Area={ship.AreaResRef}, " +
        $"X={ship.X}, " +
        $"Y={ship.Y}, " +
        $"Z={ship.Z}, " +
        $"Heading={ship.Heading}.");

    _ = LoadSavedShipState(ship);

    return ship;
}
private async Task RepairShip(
    string shipName,
    NwPlayer player)
{
    string playerName =
        player.PlayerName;

    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        await NwTask.SwitchToMainThread();

        player.SendServerMessage(
            "Your ship could not be found.");

        return;
    }

    if (ship.Hull >= MaxHull)
    {
        await NwTask.SwitchToMainThread();

        player.SendServerMessage(
            "Your ship is already at full hull.");

        _sailingNuiService.ShowCombatMessage(
            player,
            "⚓ HULL INTEGRITY\n" +
            "Already at 100%.");

      _sailingNuiService.Update(
    player,
    ship,
    _ships.Values);

        return;
    }

    int previousHull =
        ship.Hull;

    int repairedHull =
        Math.Min(
            MaxHull,
            previousHull + RepairAmount);

    ship.Hull =
        repairedHull;

    // A disabled ship remains stopped after repair.
    if (previousHull <= 0)
    {
        ship.Underway = false;
    }

    await _shipStatePersistenceService.SaveState(
        ship);

    await NwTask.SwitchToMainThread();

    player.SendServerMessage(
        $"You repair the {ship.ShipName}. " +
        $"Hull: {previousHull}% -> {ship.Hull}%.");

    _sailingNuiService.ShowCombatMessage(
        player,
        $"⚒ SHIP REPAIRED\n" +
        $"{ship.ShipName}\n" +
        $"Hull: {previousHull}% → {ship.Hull}%");

  _sailingNuiService.Update(
    player,
    ship,
    _ships.Values);

    Log.Info(
        $"Player {playerName} repaired " +
        $"ship '{ship.ShipName}': " +
        $"Hull={previousHull}->{ship.Hull}.");
}
public void RegisterHelm(
    ShipDefinition definition)
{
    _helmShips[definition.HelmTag] =
        definition.ShipName;

    _shipPlaceableTags[definition.ShipName] =
        definition.PlaceableTag;
}
    private async Task LoadSavedShipState(
    ShipState ship)
{
    SavedShipState? savedState =
        await _shipStatePersistenceService.LoadState(
            ship.ShipName);

    if (savedState == null)
    {
        Log.Info(
            $"No saved state exists for '{ship.ShipName}'. " +
            $"Saving current starting state.");

        await _shipStatePersistenceService.SaveState(
            ship);

        return;
    }

    ship.AreaResRef =
        savedState.AreaResRef;

    ship.X =
        savedState.X;

    ship.Y =
        savedState.Y;

    ship.Z =
        savedState.Z;

    ship.Heading =
        savedState.Heading;

    ship.Underway =
        savedState.Underway;

    ship.Hull =
        savedState.Hull;

    ship.WeaponResRef =
    string.IsNullOrWhiteSpace(
        savedState.WeaponResRef)
        ? "ship_cannon"
        : savedState.WeaponResRef;

        Log.Info(
        $"Applied saved state to ship '{ship.ShipName}': " +
        $"Area={ship.AreaResRef}, " +
        $"X={ship.X}, " +
        $"Y={ship.Y}, " +
        $"Z={ship.Z}, " +
        $"Heading={ship.Heading}, " +
        $"Underway={ship.Underway}, " +
        $"Hull={ship.Hull}.");
}

public ShipState? GetShip(
    string shipName)
{
    _ships.TryGetValue(
        shipName,
        out ShipState? ship);

    return ship;
}

public IReadOnlyCollection<ShipState> GetShips()
{
    return _ships.Values;
}
public bool SetNavigationDestination(
    string shipName,
    string destinationAreaResRef,
    float destinationX,
    float destinationY,
    float destinationZ)
{
    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        Log.Warn(
            $"Cannot set navigation destination: " +
            $"ship '{shipName}' does not exist.");

        return false;
    }

    return _shipNavigationService.SetDestination(
        ship,
        destinationAreaResRef,
        destinationX,
        destinationY,
        destinationZ);
}
public bool TestNavigation(
    string shipName,
    string destinationArea = "ocean_002",
    float destinationX = 30.0f,
    float destinationY = 80.0f)
{
    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        Log.Warn(
            $"Navigation test failed: " +
            $"ship '{shipName}' does not exist.");

        return false;
    }

    // -------------------------------------------------------------
    // Test destination
    //
    // Sea Sprite starts in ocean_01.
    // We deliberately send it into ocean_002.
    // -------------------------------------------------------------

    

    float destinationZ =
        ship.Z;

    Log.Info(
        $"Starting multi-area navigation test: " +
        $"Ship={ship.ShipName}, " +
        $"StartArea={ship.AreaResRef}, " +
        $"Start=(" +
        $"{ship.X:0.00}, " +
        $"{ship.Y:0.00}), " +
        $"DestinationArea={destinationArea}, " +
        $"Destination=(" +
        $"{destinationX:0.00}, " +
        $"{destinationY:0.00})");

    // -------------------------------------------------------------
    // Set the final destination.
    // -------------------------------------------------------------

    if (!SetNavigationDestination(
            shipName,
            destinationArea,
            destinationX,
            destinationY,
            destinationZ))
    {
        Log.Warn(
            $"Multi-area navigation test failed: " +
            "could not set destination.");

        return false;
    }

    // -------------------------------------------------------------
    // Build the route.
    // -------------------------------------------------------------

    ShipNavigationRoute? route =
        _shipRoutePlannerService.BuildRoute(
            ship.ShipName,
            ship.AreaResRef,
            ship.X,
            ship.Y,
            ship.Z,
            destinationArea,
            destinationX,
            destinationY,
            destinationZ);

    if (route == null)
    {
        Log.Warn(
            $"Multi-area navigation test failed: " +
            "route planner returned no route.");

        return false;
    }

    // -------------------------------------------------------------
    // Install the route.
    // -------------------------------------------------------------

    _shipNavigationService.SetRoute(
        ship,
        route);

    Log.Info(
        $"Multi-area navigation route created: " +
        $"Ship={ship.ShipName}, " +
        $"Waypoints={route.Waypoints.Count}");

    for (
        int i = 0;
        i < route.Waypoints.Count;
        i++)
    {
        ShipNavigationWaypoint waypoint =
            route.Waypoints[i];

        Log.Info(
            $"Route waypoint [{i}]: " +
            $"Description={waypoint.Description}, " +
            $"Area={waypoint.AreaResRef}, " +
            $"X={waypoint.X:0.00}, " +
            $"Y={waypoint.Y:0.00}, " +
            $"Z={waypoint.Z:0.00}");

        Log.Info(
            $"Route generated for '{shipName}': " +
        string.Join(
        " -> ",
        route.Waypoints.Select(
            wp =>
         $"{wp.AreaResRef}({wp.X:0},{wp.Y:0})")));
        }

    return true;
}
public void NavigateAllShips()
{
    foreach (ShipState ship in _ships.Values)
    {
        if (_shipNavigationService.IsNavigating(ship))
        {
            NavigateShip(ship);
        }
        else if (ship.Underway &&
                 !string.IsNullOrEmpty(ship.HelmsmanPCKey))
        {
TryMoveShip(
    ship,
    SailingStep);

UpdateDockingState(ship);

if (!string.IsNullOrWhiteSpace(ship.HelmsmanPCKey))
{
    NwPlayer? captain =
        NwModule.Instance.Players.FirstOrDefault(
            p => p.PlayerName == ship.HelmsmanPCKey);

    if (captain != null)
    {
        _chartDiscoveryService.RevealAroundShip(
            captain,
            ship);

        _sailingNuiService.Update(
            captain,
            ship,
            _ships.Values);
    }
}

if (string.IsNullOrWhiteSpace(ship.HelmsmanPCKey))
{
    continue;
}
            }

        _horizonContactService.UpdateContacts(ship);
        

        OceanContact? contact =
            _oceanContactService.GetClosestContact(ship);

        if (contact != null)
        {
            float distance =
                MathF.Sqrt(
                    MathF.Pow(contact.X - ship.X, 2) +
                    MathF.Pow(contact.Y - ship.Y, 2));

            if (!contact.Discovered &&
                distance <= OceanContactService.DiscoveryRange)
            {
                contact.Discovered = true;

                NotifyHelmsman(
                    ship,
                    $"Black sails spotted off the {GetRelativeBearing(ship, contact)}.");
            }

            if (!contact.Spawned &&
                !contact.ConvertedToShip &&
                distance <= OceanContactService.SpawnRange)
            {
                SpawnContactAsShip(contact);

                contact.Spawned = true;
                contact.ConvertedToShip = true;

                _oceanContactService.RemoveContact(contact);

                NotifyHelmsman(
                    ship,
                    "The distant silhouette resolves into the pirate Black Fang!");
            }
                // UpdateDockingState(ship);
                // UpdateSailingNui(ship);
            }
            if (UpdateDockingState(ship))
{
    UpdateSailingNui(ship);
}
        }

    // Run pirate AI once after all ships have finished their movement.
    _pirateAiService.UpdatePirates(_ships.Values);
}

public bool TakeHelm(
    string shipName,
    string pcKey)
{
    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        Log.Warn(
            $"Cannot take helm: " +
            $"ship '{shipName}' does not exist.");

        return false;
    }

    if (ship.HelmsmanPCKey != null)
    {
        return false;
    }

    ship.HelmsmanPCKey =
        pcKey;

    Log.Info(
        $"Ship '{shipName}' helm assigned to " +
        $"PC '{pcKey}'.");

    return true;
}

public void LeaveHelm(
    string shipName)
{
    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        return;
    }

    string? playerName =
        ship.HelmsmanPCKey;

    Log.Info(
        $"Ship '{shipName}' helm released from " +
        $"PC '{playerName}'.");

    ship.HelmsmanPCKey =
    null;

_playerShips.Remove(
    playerName);

if (string.IsNullOrWhiteSpace(
        playerName))
{
    return;
}

    NwPlayer? player =
        NwModule.Instance.Players.FirstOrDefault(
            p =>
                string.Equals(
                    p.PlayerName,
                    playerName,
                    StringComparison.Ordinal));

    if (player == null)
    {
        return;
    }

    if (_shipCrewService.GetRole(
            shipName,
            playerName) ==
        ShipCrewRole.Captain)
    {
        if (_shipCrewService.SetCrewMember(
                shipName,
                player))
        {
            Log.Info(
                $"Player {playerName} is no longer " +
                $"captain of '{shipName}' and is now crew.");
        }
        }
 
    }

private void TurnLeft(
    string shipName)
{
    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        return;
    }

    ship.Heading = ship.Heading switch
    {
        Heading.North => Heading.NorthWest,
        Heading.NorthWest => Heading.West,
        Heading.West => Heading.SouthWest,
        Heading.SouthWest => Heading.South,
        Heading.South => Heading.SouthEast,
        Heading.SouthEast => Heading.East,
        Heading.East => Heading.NorthEast,
        Heading.NorthEast => Heading.North,
        _ => Heading.East
    };

    LogShipState(ship);

    UpdatePhysicalShip(ship);

    _ = _shipStatePersistenceService.SaveState(
        ship);
}

private void TurnRight(
    string shipName)
{
    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        return;
    }

    ship.Heading = ship.Heading switch
    {
        Heading.North => Heading.NorthEast,
        Heading.NorthEast => Heading.East,
        Heading.East => Heading.SouthEast,
        Heading.SouthEast => Heading.South,
        Heading.South => Heading.SouthWest,
        Heading.SouthWest => Heading.West,
        Heading.West => Heading.NorthWest,
        Heading.NorthWest => Heading.North,
        _ => Heading.East
    };

    LogShipState(ship);

    UpdatePhysicalShip(ship);

    _ = _shipStatePersistenceService.SaveState(
        ship);
}

private void MoveAhead(
    string shipName,
    NwPlayer player)
{
    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        return;
    }

    TryMoveShip(
        ship,
        SailingStep);

    UpdateSailingNui(
    ship);
}

private void MoveAstern(
    string shipName,
    NwPlayer player)
{
    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        return;
    }

    TryMoveShip(
        ship,
        -SailingStep);
}

private void TryMoveShip(
    ShipState ship,
    float distance)
{
    if (ship.Hull <= 0)
    {
        Log.Info(
            $"Ship '{ship.ShipName}' " +
            "cannot move because it is disabled.");

        return;
    }

   if (!_sailingAreaService.TryGetArea(
        ship.AreaResRef,
        out SailingArea? area))
    {
        Log.Warn(
            $"Cannot move ship '{ship.ShipName}': " +
            $"sailing area '{ship.AreaResRef}' " +
            "is not registered.");

        return;
    }

    float newX =
        ship.X;

    float newY =
        ship.Y;

    switch (ship.Heading)
    {
        case Heading.North:
            newY += distance;
            break;

        case Heading.NorthEast:
            newX += distance;
            newY += distance;
            break;

        case Heading.East:
            newX += distance;
            break;

        case Heading.SouthEast:
            newX += distance;
            newY -= distance;
            break;

        case Heading.South:
            newY -= distance;
            break;

        case Heading.SouthWest:
            newX -= distance;
            newY -= distance;
            break;

        case Heading.West:
            newX -= distance;
            break;

        case Heading.NorthWest:
            newX -= distance;
            newY += distance;
            break;
    }

// -------------------------------------------------------------
// East boundary
// -------------------------------------------------------------

if (newX > area.MaxX)
{
    string? destinationAreaResRef =
        area.EastAreaResRef;

    if (!CrossBoundary(
            ship,
            destinationAreaResRef,
            "East"))
    {
        return;
            }
           _shipNavigationService.AdvanceWaypoint(ship);

            SailingArea destinationArea =
        _sailingAreaService.GetArea(
            ship.AreaResRef)!;

    SailingLocation? entry = destinationArea.NorthEntry;

ship.X = entry?.X ?? 80f;
ship.Y = entry?.Y ?? destinationArea.MaxY - BoundaryEntryOffset;

    ship.Underway = true;

    LogShipState(ship);

    UpdatePhysicalShip(ship);

    UpdateSailingNui(ship);

    _shipEncounterService.CheckEncounters(
        _ships.Values);

    _ = _shipStatePersistenceService.SaveState(
        ship);

    return;
}
// -------------------------------------------------------------
// West boundary
// -------------------------------------------------------------

else if (newX < area.MinX)
{
    string? destinationAreaResRef =
        area.WestAreaResRef;

    if (!CrossBoundary(
            ship,
            destinationAreaResRef,
            "West"))
    {
        return;
            }
         _shipNavigationService.AdvanceWaypoint(ship);

            SailingArea destinationArea =
        _sailingAreaService.GetArea(ship.AreaResRef)!;

SailingLocation? entry = destinationArea.EastEntry;

ship.X = entry?.X ?? destinationArea.MaxX - BoundaryEntryOffset;
ship.Y = entry?.Y ?? 80f;

LogShipState(ship);

UpdatePhysicalShip(ship);

UpdateSailingNui(ship);

_shipEncounterService.CheckEncounters(
    _ships.Values);

_ = _shipStatePersistenceService.SaveState(
    ship);

return;
        }

// -------------------------------------------------------------
// North boundary
// -------------------------------------------------------------

else if (newY > area.MaxY)
{
    string? destinationAreaResRef =
        area.NorthAreaResRef;

    if (!CrossBoundary(
            ship,
            destinationAreaResRef,
            "North"))
    {
        return;
            }
           _shipNavigationService.AdvanceWaypoint(ship);

            SailingArea destinationArea =
        _sailingAreaService.GetArea(ship.AreaResRef)!;

SailingLocation? entry = destinationArea.SouthEntry;

ship.X = entry?.X ?? 80f;
ship.Y = entry?.Y ?? destinationArea.MinY + BoundaryEntryOffset;

LogShipState(ship);

UpdatePhysicalShip(ship);

UpdateSailingNui(ship);

_shipEncounterService.CheckEncounters(
    _ships.Values);

_ = _shipStatePersistenceService.SaveState(
    ship);

return;
        }

// -------------------------------------------------------------
// South boundary
// -------------------------------------------------------------

else if (newY < area.MinY)
{
    string? destinationAreaResRef =
        area.SouthAreaResRef;

    if (!CrossBoundary(
            ship,
            destinationAreaResRef,
            "South"))
    {
        return;
    }
  _shipNavigationService.AdvanceWaypoint(ship);
    SailingArea destinationArea =
        _sailingAreaService.GetArea(ship.AreaResRef)!;

SailingLocation? entry = destinationArea.NorthEntry;

ship.X = entry?.X ?? 80f;
ship.Y = entry?.Y ?? destinationArea.MaxY - BoundaryEntryOffset;

LogShipState(ship);

UpdatePhysicalShip(ship);

UpdateSailingNui(ship);

UpdateSailingNuiForAllPlayers(ship);

            _shipEncounterService.CheckEncounters(
    _ships.Values);

_ = _shipStatePersistenceService.SaveState(
    ship);

return;
        }

// -------------------------------------------------------------
// Normal movement
// -------------------------------------------------------------

else
{
    if (!_shipObstacleService.CanMoveTo(
            ship.AreaResRef,
            newX,
            newY))
    {
        Log.Info(
            $"Ship '{ship.ShipName}' " +
            $"cannot move to " +
            $"({newX:0.00}, {newY:0.00}) " +
            "because of an obstacle.");

        return;
    }

    ship.X =
        newX;

    ship.Y =
        newY;
}
    {
        if (!_shipObstacleService.CanMoveTo(
                ship.AreaResRef,
                newX,
                newY))
        {
            Log.Info(
                $"Ship '{ship.ShipName}' " +
                $"cannot move to " +
                $"({newX:0.00}, {newY:0.00}) " +
                "because of an obstacle.");

            return;
        }

        ship.X =
            newX;

        ship.Y =
            newY;
    }

    ship.Underway = true;

    LogShipState(ship);

    UpdatePhysicalShip(ship);

    _shipEncounterService.CheckEncounters(
        _ships.Values);

    _ = _shipStatePersistenceService.SaveState(
        ship);
}
 public void NavigateShip(
    ShipState ship)
{
    if (!_shipNavigationService.IsNavigating(ship))
    {
        return;
    }
// -------------------------------------------------------------
// Merchant port stay
// -------------------------------------------------------------

if (ship.ShipType == ShipType.Merchant &&
    ship.IsInPort)
{
    bool stillInPort =
        _merchantPortService.UpdatePortStay(ship);

    if (stillInPort)
    {
        return;
    }

    // ---------------------------------------------------------
    // Port stay finished.
    // Advance to the next route waypoint immediately so we
    // don't re-enter the same port.
    // ---------------------------------------------------------

    ShipNavigationRoute? portRoute =
        _shipNavigationService.GetRoute(ship);

    if (portRoute != null)
    {
        bool routeComplete =
            _shipNavigationService.AdvanceWaypoint(ship);

        if (routeComplete)
        {
            _shipNavigationService.ClearRoute(ship);
            ship.Underway = false;
            return;
        }

        ShipNavigationWaypoint? nextWaypoint =
            _shipNavigationService.GetCurrentWaypoint(ship);

        if (nextWaypoint != null)
        {
            _shipNavigationService.SetDestination(
                ship,
                nextWaypoint.AreaResRef,
                nextWaypoint.X,
                nextWaypoint.Y,
                nextWaypoint.Z);

            Log.Info(
                $"Merchant '{ship.ShipName}' departing port and " +
                $"heading to waypoint: " +
                $"Area={nextWaypoint.AreaResRef}, " +
                $"X={nextWaypoint.X:0.00}, " +
                $"Y={nextWaypoint.Y:0.00}");
        }
    }

    return;
}
    // -------------------------------------------------------------
    // Disabled ship
    // -------------------------------------------------------------

    if (ship.Hull <= 0)
    {
        Log.Info(
            $"Navigation stopped for ship '{ship.ShipName}': ship is disabled.");

        _shipNavigationService.ClearDestination(ship);
        ship.Underway = false;

        return;
    }

// -------------------------------------------------------------
// Waypoint reached
// -------------------------------------------------------------

if (_shipNavigationService.IsCurrentWaypointReached(ship))
{
    ShipNavigationWaypoint? currentWaypoint =
        _shipNavigationService.GetCurrentWaypoint(ship);

    // If we've reached a boundary waypoint, don't advance yet.
    // Let CrossBoundary() advance the route after the area changes.
    if (currentWaypoint != null &&
        string.Equals(
            currentWaypoint.AreaResRef,
            ship.AreaResRef,
            StringComparison.OrdinalIgnoreCase))
    {
        ShipNavigationRoute? route =
            _shipNavigationService.GetRoute(ship);

   if (route != null)
{
    bool isFinalWaypoint =
        route.CurrentWaypointIndex >=
        route.Waypoints.Count - 1;
// -------------------------------------------------
// Merchant port stop
// -------------------------------------------------

if (ship.ShipType == ShipType.Merchant &&
    currentWaypoint != null &&
    !string.IsNullOrWhiteSpace(currentWaypoint.PortId))
{
    if (!_merchantPortService.BeginPortStay(
            ship,
            currentWaypoint))
    {
        // Could not begin port stay.
    }

    return;
}
    // ---------------------------------------------------------
    // Looping route at its final waypoint.
    // AdvanceWaypoint() will wrap the route back to waypoint 0.
    // ---------------------------------------------------------

    if (route.Loop && isFinalWaypoint)
    {
        bool routeComplete =
            _shipNavigationService.AdvanceWaypoint(ship);

        if (routeComplete)
        {
            _shipNavigationService.ClearRoute(ship);
            ship.Underway = false;
            return;
        }

        ShipNavigationWaypoint? newWaypoint =
            _shipNavigationService.GetCurrentWaypoint(ship);

        if (newWaypoint != null)
        {
            _shipNavigationService.SetDestination(
                ship,
                newWaypoint.AreaResRef,
                newWaypoint.X,
                newWaypoint.Y,
                newWaypoint.Z);

            Log.Info(
                $"Ship '{ship.ShipName}' looping to waypoint: " +
                $"Area={newWaypoint.AreaResRef}, " +
                $"X={newWaypoint.X:0.00}, " +
                $"Y={newWaypoint.Y:0.00}");
        }
    }
    else if (
        route.CurrentWaypointIndex + 1 <
        route.Waypoints.Count)
    {
        ShipNavigationWaypoint next =
            route.Waypoints[
                route.CurrentWaypointIndex + 1];

        if (!string.Equals(
                next.AreaResRef,
                ship.AreaResRef,
                StringComparison.OrdinalIgnoreCase))
        {
            // Stay on the boundary waypoint.
            // CrossBoundary() will advance the route.
        }
        else
        {
            bool routeComplete =
                _shipNavigationService.AdvanceWaypoint(ship);

            if (routeComplete)
            {
                _shipNavigationService.ClearRoute(ship);
                ship.Underway = false;
                return;
            }

            ShipNavigationWaypoint? newWaypoint =
                _shipNavigationService.GetCurrentWaypoint(ship);

            if (newWaypoint != null)
            {
                _shipNavigationService.SetDestination(
                    ship,
                    newWaypoint.AreaResRef,
                    newWaypoint.X,
                    newWaypoint.Y,
                    newWaypoint.Z);

                Log.Info(
                    $"Ship '{ship.ShipName}' advancing to waypoint: " +
                    $"Area={newWaypoint.AreaResRef}, " +
                    $"X={newWaypoint.X:0.00}, " +
                    $"Y={newWaypoint.Y:0.00}");
            }
        }
    }
}   }
}
else
{
    ShipNavigationWaypoint? nextWaypoint =
        _shipNavigationService.GetCurrentWaypoint(ship);

    Log.Info(
        $"Ship '{ship.ShipName}' proceeding to next waypoint: " +
        $"Area={nextWaypoint?.AreaResRef}, " +
        $"X={nextWaypoint?.X:0.00}, " +
        $"Y={nextWaypoint?.Y:0.00}");

    // Immediately face the new waypoint instead of waiting
    // for the next navigation tick.
    Heading newHeading =
        _shipNavigationService.GetNavigationHeading(
            ship,
            SailingStep);

    if (ship.Heading != newHeading)
    {
        ship.Heading = newHeading;
        UpdatePhysicalShip(ship);
    }

    UpdateSailingNui(ship);
    _ = _shipStatePersistenceService.SaveState(ship);
}   

// -------------------------------------------------------------
// Final destination reached
// -------------------------------------------------------------

if (_shipNavigationService.IsDestinationReached(ship))
{
    ShipNavigationRoute? route =
        _shipNavigationService.GetRoute(ship);

    // -------------------------------------------------------------
    // Route navigation:
    // Reaching a waypoint is NOT the end of navigation.
    // The waypoint system will advance the route.
    // -------------------------------------------------------------

    if (route != null)
    {
        return;
    }

    // -------------------------------------------------------------
    // Normal one-shot navigation.
    // -------------------------------------------------------------

    _shipNavigationService.CompleteNavigation(ship);

    ship.Underway = false;

    Log.Info(
        $"Ship '{ship.ShipName}' arrived at its navigation destination.");

    UpdateSailingNui(ship);

    return;
}
    // -------------------------------------------------------------
    // Determine obstacle-aware navigation heading
    // -------------------------------------------------------------

    Heading desiredHeading =
        _shipNavigationService.GetNavigationHeading(
            ship,
            SailingStep);

    // -------------------------------------------------------------
    // Turn toward selected heading
    // -------------------------------------------------------------

    if (ship.Heading != desiredHeading)
    {
        ship.Heading = desiredHeading;

        Log.Info(
            $"Navigation heading changed: Ship={ship.ShipName}, Heading={ship.Heading}");

        UpdatePhysicalShip(ship);
        UpdateSailingNui(ship);

        _ = _shipStatePersistenceService.SaveState(ship);

        return;
    }

    // -------------------------------------------------------------
    // Move forward
    // -------------------------------------------------------------

    TryMoveShip(ship, SailingStep);

    UpdateSailingNui(ship);

    UpdateSailingNuiForAllPlayers(ship);

// -------------------------------------------------------------
// Check destination after movement
// -------------------------------------------------------------

if (_shipNavigationService.IsDestinationReached(ship))
{
    ShipNavigationRoute? route =
        _shipNavigationService.GetRoute(ship);

    // -------------------------------------------------------------
    // Looping merchant route:
    // Leave navigation active.
    // The next navigation tick will advance the waypoint.
    // -------------------------------------------------------------

    if (route != null &&
        route.Loop)
    {
        return;
    }

    // -------------------------------------------------------------
    // Normal one-shot navigation
    // -------------------------------------------------------------

    _shipNavigationService.CompleteNavigation(ship);

    ship.Underway = false;

    Log.Info(
        $"Ship '{ship.ShipName}' arrived at its navigation destination.");
}
}


    private SailingLocation? GetDestinationEntry(
    string? destinationAreaResRef,
    string entryDirection)
{
    if (string.IsNullOrWhiteSpace(
            destinationAreaResRef))
    {
        return null;
    }

    SailingArea? destinationArea =
    _sailingAreaService.GetArea(
        destinationAreaResRef);

if (destinationArea == null)
{
    return null;
}

    return entryDirection switch
    {
        "North" => destinationArea.NorthEntry,
        "South" => destinationArea.SouthEntry,
        "East" => destinationArea.EastEntry,
        "West" => destinationArea.WestEntry,
        _ => null
    };
}

private bool CrossBoundary(
    ShipState ship,
    string? destinationAreaResRef,
    string boundary)
{
    if (string.IsNullOrWhiteSpace(
            destinationAreaResRef))
    {
        Log.Warn(
            $"Ship '{ship.ShipName}' reached the " +
            $"{boundary} boundary of " +
            $"{ship.AreaResRef}, but no destination " +
            $"area is configured.");

        return false;
    }

if (!_sailingAreaService.ContainsArea(
        destinationAreaResRef))
    {
        Log.Warn(
            $"Ship '{ship.ShipName}' reached the " +
            $"{boundary} boundary of " +
            $"{ship.AreaResRef}, but destination " +
            $"area '{destinationAreaResRef}' " +
            $"is not registered.");

        return false;
    }

    Log.Info(
        $"Ship '{ship.ShipName}' crossing " +
        $"{boundary} boundary: " +
        $"{ship.AreaResRef} -> " +
        $"{destinationAreaResRef}");

    ship.AreaResRef =
        destinationAreaResRef;

    return true;
}
// -------------------------------------------------------------
// Spawn Pirate Ship
// -------------------------------------------------------------

private void SpawnPirateShip(
    OceanContact contact)
{
    string shipName =
        contact.Id;

    if (_ships.ContainsKey(shipName))
    {
        return;
    }

    ShipState pirate =
        new()
        {
            ShipName = shipName,

            DeckAreaResRef = "pirate_brig_d",

            ShipType = ShipType.Pirate,

            AreaResRef = contact.AreaResRef,
            X = contact.X,
            Y = contact.Y,
            Z = contact.Z,
            Heading = Heading.West,
            Hull = 100,
            Underway = false,
            HelmsmanPCKey = null,
        };

    _ships[shipName] =
        pirate;

    Log.Info(
        $"Spawned pirate ship '{shipName}' " +
        $"at ({contact.X:0.0}, {contact.Y:0.0}) " +
        $"in {contact.AreaResRef}.");

    UpdatePhysicalShip(pirate);

    _ = _shipStatePersistenceService.SaveState(
        pirate);
}
private void UpdatePhysicalShip(
    ShipState ship)
{
    if (!_shipPlaceableTags.TryGetValue(
            ship.ShipName,
            out string? placeableTag))
    {
        Log.Warn(
            $"No physical placeable tag configured " +
            $"for ship '{ship.ShipName}'.");

        return;
    }

    NwArea? area =
        NwModule.Instance.Areas.FirstOrDefault(
            x => x.ResRef == ship.AreaResRef);

    if (area == null)
    {
        Log.Warn(
            $"Cannot update physical ship '{ship.ShipName}': " +
            $"area '{ship.AreaResRef}' was not found.");

        return;
    }

    List<NwPlaceable> placeables =
        NwObject.FindObjectsWithTag<NwPlaceable>(
            placeableTag).ToList();

    if (placeables.Count == 0)
    {
        Log.Warn(
            $"No physical ship placeable found for " +
            $"'{ship.ShipName}' using tag " +
            $"'{placeableTag}'.");

        return;
    }

    float rotation =
        GetHeadingRotation(
            ship.Heading);

    System.Numerics.Vector3 position =
        new(
            ship.X,
            ship.Y,
            ship.Z);

    Location location =
        Location.Create(
            area,
            position,
            rotation);

    foreach (NwPlaceable placeable in placeables)
    {
        placeable.Location =
            location;

        Log.Info(
            $"Updated physical ship '{ship.ShipName}': " +
            $"Tag={placeable.Tag}, " +
            $"Area={ship.AreaResRef}, " +
            $"X={ship.X}, " +
            $"Y={ship.Y}, " +
            $"Z={ship.Z}, " +
            $"Rotation={rotation}");
    }
}
private void SpawnOrUpdatePhysicalShip(
    ShipState ship,
    string placeableResRef,
    string placeableTag)
{
    _shipPlaceableTags[ship.ShipName] =
        placeableTag;

    List<NwPlaceable> placeables =
        NwObject.FindObjectsWithTag<NwPlaceable>(
            placeableTag).ToList();

    if (placeables.Count == 0)
    {
        NwArea? area =
            NwModule.Instance.Areas.FirstOrDefault(
                x => x.ResRef == ship.AreaResRef);

        if (area == null)
        {
            return;
        }

        float rotation =
            GetHeadingRotation(
                ship.Heading);

        Location location =
            Location.Create(
                area,
                new System.Numerics.Vector3(
                    ship.X,
                    ship.Y,
                    ship.Z),
                rotation);

        NwPlaceable placeable =
            NwPlaceable.Create(
                placeableResRef,
                location,
                false);

        placeable.Tag =
            placeableTag;
    }

    UpdatePhysicalShip(ship);
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

private void LogShipState(
    ShipState ship)
{
    Log.Info(
        $"Ship '{ship.ShipName}' state: " +
        $"Area={ship.AreaResRef}, " +
        $"X={ship.X}, " +
        $"Y={ship.Y}, " +
        $"Z={ship.Z}, " +
        $"Heading={ship.Heading}, " +
        $"Underway={ship.Underway}, " +
        $"Hull={ship.Hull}");
}

private void UpdateSailingNui(
    ShipState ship)
{
    if (ship.IsDocking)
    {
        return;
    }

    foreach (
        KeyValuePair<string, string> entry in _playerShips)
    {
        if (!string.Equals(
                entry.Value,
                ship.ShipName,
                StringComparison.Ordinal))
        {
            continue;
        }

        NwPlayer? player =
            NwModule.Instance.Players
                .FirstOrDefault(
                    p =>
                        string.Equals(
                            p.PlayerName,
                            entry.Key,
                            StringComparison.Ordinal));

        if (player == null ||
            player.ControlledCreature == null ||
            !player.ControlledCreature.IsValid)
        {
            continue;
        }

       _sailingNuiService.Update(
    player,
    ship,
    _ships.Values);
    }
}
private void UpdateSailingNuiForAllPlayers(
    ShipState movedShip)
{
    foreach (NwPlayer player in
        NwModule.Instance.Players)
    {
        if (player.ControlledCreature == null ||
            !player.ControlledCreature.IsValid)
        {
            continue;
        }

        // Only update players who are currently sailing.
        if (!_playerShips.ContainsKey(player.PlayerName))
        {
            continue;
        }

        ShipState? playerShip =
            _ships.Values.FirstOrDefault(
                ship =>
                    string.Equals(
                        _playerShips[player.PlayerName],
                        ship.ShipName,
                        StringComparison.Ordinal));

        if (playerShip == null)
        {
            continue;
        }

        _sailingNuiService.Update(
            player,
            playerShip,
            _ships.Values);
    }
}
private void HandleShipEncounterStarted(
    ShipEncounter encounter)
{
    HandleEncounterForShip(
        encounter,
        encounter.ShipA);

    HandleEncounterForShip(
        encounter,
        encounter.ShipB);
}

private void HandleEncounterForShip(
    ShipEncounter encounter,
    ShipState ship)
{
    // Only notify player-controlled ships.
    if (ship.ShipType != ShipType.Player)
    {
        return;
    }

    if (string.IsNullOrWhiteSpace(
            ship.HelmsmanPCKey))
    {
        return;
    }

    NwPlayer? player =
        NwModule.Instance.Players.FirstOrDefault(
            p =>
                string.Equals(
                    p.PlayerName,
                    ship.HelmsmanPCKey,
                    StringComparison.Ordinal));

    if (player == null)
    {
        return;
    }

    ShipState targetShip =
        ReferenceEquals(
            encounter.ShipA,
            ship)
            ? encounter.ShipB
            : encounter.ShipA;

    player.SendServerMessage(
        $"You have encountered the " +
        $"{targetShip.ShipName}.");

    Log.Info(
        $"Player encounter notification: " +
        $"Player={player.PlayerName}, " +
        $"Ship={ship.ShipName}, " +
        $"Target={targetShip.ShipName}, " +
        $"Distance={encounter.Distance:0.00}");

    _sailingNuiService.Update(
        player,
        ship,
        _ships.Values);
}

private void HandleShipEncounterEnded(
    ShipEncounter encounter)
{
    HandleEncounterEndedForShip(
        encounter,
        encounter.ShipA);

    HandleEncounterEndedForShip(
        encounter,
        encounter.ShipB);
}

private void HandleEncounterEndedForShip(
    ShipEncounter encounter,
    ShipState ship)
{
    if (ship.ShipType != ShipType.Player)
    {
        return;
    }

    if (string.IsNullOrWhiteSpace(
            ship.HelmsmanPCKey))
    {
        return;
    }

    NwPlayer? player =
        NwModule.Instance.Players.FirstOrDefault(
            p =>
                string.Equals(
                    p.PlayerName,
                    ship.HelmsmanPCKey,
                    StringComparison.Ordinal));

    if (player == null)
    {
        return;
    }

    player.SendServerMessage(
        "The other ship has moved out of encounter range.");

    _sailingNuiService.Update(
        player,
        ship,
        _ships.Values);

    Log.Info(
        $"Player encounter ended: " +
        $"Player={player.PlayerName}, " +
        $"Ship={ship.ShipName}, " +
        $"OtherShip=" +
        $"{(ReferenceEquals(encounter.ShipA, ship) ? encounter.ShipB.ShipName : encounter.ShipA.ShipName)}");
}
private void TradeWithTarget(
    string shipName,
    NwPlayer player)
{
    ShipState? ship =
        GetShip(shipName);

    if (ship == null)
    {
        Log.Warn(
            $"Cannot trade from ship '{shipName}': " +
            "ship does not exist.");

        return;
    }

    if (!_shipEncounterService.TryGetTarget(
            ship,
            out ShipState? targetShip,
            out ShipEncounter? encounter) ||
        targetShip == null ||
        encounter == null)
    {
        player.SendServerMessage(
            "There is no ship close enough to trade with.");

        return;
    }

    if (targetShip.ShipType != ShipType.Merchant)
    {
        player.SendServerMessage(
            $"The {targetShip.ShipName} is not a merchant vessel.");

        return;
    }

    if (string.IsNullOrWhiteSpace(
            targetShip.CurrentTradePortId))
    {
        player.SendServerMessage(
            $"The {targetShip.ShipName} is currently sailing " +
            $"and has no active trade market.");

        return;
    }

    OpenMerchantTradeWindow(
        player,
        ship,
        targetShip);

    Log.Info(
        $"Merchant trade window opened: " +
        $"Player={player.PlayerName}, " +
        $"Merchant={targetShip.ShipName}, " +
        $"Port={targetShip.CurrentTradePortId}");
}

    private static string FormatMerchantCargo(
    ShipState ship)
{
    if (ship.Cargo.Count == 0)
    {
        return "Empty";
    }

    return string.Join(
        ", ",
        ship.Cargo.Select(
            cargo =>
                $"{cargo.ItemId}={cargo.Quantity}"));
}
private void OpenMerchantTradeWindow(
    NwPlayer player,
    ShipState playerShip,
    ShipState merchant)
{
    NwCreature? creature =
        player.LoginCreature;

    if (creature == null)
    {
        player.SendServerMessage(
            "Your character could not be found.");

        return;
    }

    int playerGold =
        (int)creature.Gold;

    int playerCargoUsed =
        playerShip.Cargo.Sum(
            cargo => cargo.Quantity);

    string playerCargoText =
        playerShip.Cargo.Count == 0
            ? "Empty"
            : string.Join(
                ", ",
                playerShip.Cargo.Select(
                    cargo =>
                        $"{cargo.ItemId}={cargo.Quantity}"));

    int merchantCargoUsed =
        merchant.Cargo.Sum(
            cargo => cargo.Quantity);

    string merchantCargoText =
        merchant.Cargo.Count == 0
            ? "Empty"
            : string.Join(
                ", ",
                merchant.Cargo.Select(
                    cargo =>
                        $"{cargo.ItemId}={cargo.Quantity}"));

    string marketText =
        BuildMarketText(
            merchant);

    NuiLabel title =
    new(
        $"TRADE WITH {merchant.ShipName}");

NuiLabel port =
    new(
        $"Port: {merchant.CurrentTradePortId}");

NuiLabel playerGoldLabel =
    new(
        _tradePlayerGoldBind);

NuiLabel playerCargoLabel =
    new(
        _tradePlayerCargoBind);

NuiLabel merchantGoldLabel =
    new(
        _tradeMerchantGoldBind);

NuiLabel merchantCargoLabel =
    new(
        _tradeMerchantCargoBind);
    NuiLabel marketLabel =
        new(
            marketText);

    NuiButton buyButton =
        new(
            "BUY 1 GRAIN");

    buyButton.Id =
        "merchant_trade_buy_1";

    NuiButton sellButton =
        new(
            "SELL 1 TIMBER");

    sellButton.Id =
        "merchant_trade_sell_1";

    NuiButton closeButton =
        new(
            "CLOSE");

    closeButton.Id =
        "merchant_trade_close";

    NuiColumn column =
        new();

    column.Children.Add(
        title);

    column.Children.Add(
        port);

    column.Children.Add(
        playerGoldLabel);

    column.Children.Add(
        playerCargoLabel);

    column.Children.Add(
        merchantGoldLabel);

    column.Children.Add(
        merchantCargoLabel);

    column.Children.Add(
        marketLabel);

    column.Children.Add(
        buyButton);

    column.Children.Add(
        sellButton);

    column.Children.Add(
        closeButton);

    NuiWindow window =
        new(
            column,
            NuiProperty<string>.CreateValue(
                "Merchant Trade"));

    window.Geometry =
        new NuiRect(
            -1.0f,
            -1.0f,
            600.0f,
            500.0f);

    window.Closable =
        true;

if (!player.TryCreateNuiWindow(
        window,
        out NuiWindowToken token,
        MerchantTradeWindowId))
{
    Log.Warn(
        $"Failed to create merchant trade window " +
        $"for player {player.PlayerName}.");

    return;
}

_merchantTradeTokens[player.PlayerName] =
    token;
token.SetBindValue(
    _tradePlayerGoldBind,
    $"Your Gold: {(int)creature.Gold}");

token.SetBindValue(
    _tradePlayerCargoBind,
    $"Your Cargo: " +
    $"{playerCargoUsed}/{playerShip.CargoCapacity} " +
    $"| {playerCargoText}");

token.SetBindValue(
    _tradeMerchantGoldBind,
    $"Merchant Gold: " +
    $"{merchant.MerchantGold}");

token.SetBindValue(
    _tradeMerchantCargoBind,
    $"Merchant Cargo: " +
    $"{merchantCargoUsed}/{merchant.CargoCapacity} " +
    $"| {merchantCargoText}");
    Log.Info(
        $"Merchant trade NUI created: " +
        $"Player={player.PlayerName}, " +
        $"Merchant={merchant.ShipName}");
}
    private void BuyFromMerchant(
        NwPlayer player,
    int quantity)
{
    ShipState? playerShip =
        _ships.Values.FirstOrDefault(
            ship =>
                string.Equals(
                    ship.HelmsmanPCKey,
                    player.PlayerName,
                    StringComparison.Ordinal));

    if (playerShip == null)
    {
        player.SendServerMessage(
            "You are not currently at the helm of a ship.");

        return;
    }

    if (!_shipEncounterService.TryGetTarget(
            playerShip,
            out ShipState? merchant,
            out ShipEncounter? encounter) ||
        merchant == null ||
        encounter == null)
    {
        player.SendServerMessage(
            "There is no merchant close enough to trade with.");

        return;
    }

    if (merchant.ShipType != ShipType.Merchant)
    {
        player.SendServerMessage(
            "That ship is not a merchant.");

        return;
    }

    if (string.IsNullOrWhiteSpace(
            merchant.CurrentTradePortId))
    {
        player.SendServerMessage(
            "The merchant is not currently trading at a port.");

        return;
    }

    NwCreature? creature =
        player.LoginCreature;

    if (creature == null)
    {
        player.SendServerMessage(
            "Your character could not be found.");

        return;
    }

    bool success =
        _merchantTradeService.TryBuy(
            creature,
            playerShip,
            merchant,
            "grain",
            quantity,
            out string message);

    player.SendServerMessage(
        message);

    if (success)
    {
        Log.Info(
            $"Player trade completed: " +
            $"Player={player.PlayerName}, " +
            $"Ship={playerShip.ShipName}, " +
            $"Merchant={merchant.ShipName}, " +
            $"Item=grain, " +
            $"Quantity={quantity}");
RefreshMerchantTradeWindow(
    player);
        _sailingNuiService.Update(
            player,
            playerShip,
            _ships.Values);
    }
}
private void SellToMerchant(
    NwPlayer player,
    int quantity)
{
    ShipState? playerShip =
        _ships.Values.FirstOrDefault(
            ship =>
                string.Equals(
                    ship.HelmsmanPCKey,
                    player.PlayerName,
                    StringComparison.Ordinal));

    if (playerShip == null)
    {
        player.SendServerMessage(
            "You are not currently at the helm of a ship.");

        return;
    }

    if (!_shipEncounterService.TryGetTarget(
            playerShip,
            out ShipState? merchant,
            out ShipEncounter? encounter) ||
        merchant == null ||
        encounter == null)
    {
        player.SendServerMessage(
            "There is no merchant close enough to trade with.");

        return;
    }

    if (merchant.ShipType != ShipType.Merchant)
    {
        player.SendServerMessage(
            "That ship is not a merchant.");

        return;
    }

    if (string.IsNullOrWhiteSpace(
            merchant.CurrentTradePortId))
    {
        player.SendServerMessage(
            "The merchant is not currently trading at a port.");

        return;
    }

    NwCreature? creature =
        player.LoginCreature;

    if (creature == null)
    {
        player.SendServerMessage(
            "Your character could not be found.");

        return;
    }

    bool success =
        _merchantTradeService.TrySell(
            creature,
            playerShip,
            merchant,
            "timber",
            quantity,
            out string message);

    player.SendServerMessage(
        message);

if (success)
{
    RefreshMerchantTradeWindow(
        player);

    _sailingNuiService.Update(
        player,
        playerShip,
        _ships.Values);
}
}
private string BuildMarketText(
    ShipState merchant)
{
    if (string.IsNullOrWhiteSpace(
            merchant.CurrentTradePortId))
    {
        return "Market: CLOSED";
    }

    // We need the existing port definition from
    // MerchantTradeService. Do not duplicate prices here.
    return
        $"Market: {merchant.CurrentTradePortId}\n" +
        "Merchant sells: grain 8 gp\n" +
        "Merchant buys: timber 18 gp";
}
private void RefreshMerchantTradeWindow(
    NwPlayer player)
{
    if (!_merchantTradeTokens.TryGetValue(
            player.PlayerName,
            out NuiWindowToken token))
    {
        return;
    }

    NwCreature? creature =
        player.LoginCreature;

    if (creature == null)
    {
        return;
    }

    ShipState? playerShip =
        _ships.Values.FirstOrDefault(
            ship =>
                string.Equals(
                    ship.HelmsmanPCKey,
                    player.PlayerName,
                    StringComparison.Ordinal));

    if (playerShip == null)
    {
        return;
    }

    if (!_shipEncounterService.TryGetTarget(
            playerShip,
            out ShipState? merchant,
            out ShipEncounter? encounter) ||
        merchant == null ||
        encounter == null ||
        merchant.ShipType != ShipType.Merchant)
    {
        return;
    }

    int playerGold =
        
        (int)creature.Gold;

    int playerCargoUsed =
        playerShip.Cargo.Sum(
            cargo => cargo.Quantity);

    string playerCargoText =
        playerShip.Cargo.Count == 0
            ? "Empty"
            : string.Join(
                ", ",
                playerShip.Cargo.Select(
                    cargo =>
                        $"{cargo.ItemId}={cargo.Quantity}"));

    int merchantCargoUsed =
        merchant.Cargo.Sum(
            cargo => cargo.Quantity);

    string merchantCargoText =
        merchant.Cargo.Count == 0
            ? "Empty"
            : string.Join(
                ", ",
                merchant.Cargo.Select(
                    cargo =>
                        $"{cargo.ItemId}={cargo.Quantity}"));

    token.SetBindValue(
        _tradePlayerGoldBind,
        $"Your Gold: {playerGold}");

    token.SetBindValue(
        _tradePlayerCargoBind,
        $"Your Cargo: " +
        $"{playerCargoUsed}/{playerShip.CargoCapacity} " +
        $"| {playerCargoText}");

    token.SetBindValue(
        _tradeMerchantGoldBind,
        $"Merchant Gold: " +
        $"{merchant.MerchantGold}");

    token.SetBindValue(
        _tradeMerchantCargoBind,
        $"Merchant Cargo: " +
        $"{merchantCargoUsed}/{merchant.CargoCapacity} " +
        $"| {merchantCargoText}");
}
}
