using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;
[ServiceBinding(typeof(HelmService))]
public class HelmService : IDisposable
{
    private const string StartingAreaResRef = "ocean_01";
    private const float SailingStep = 1.0f;
    private const float AreaMaxX = 640.0f;
    private const float AreaMaxY = 640.0f;
    //private const bool PersistShips = false;

    private const float BoundaryEntryOffset = 5.0f;
    private const int RepairAmount = 25;

    private const string SouthportShipyardTag =
        "southport_shipyard";

    private const string SouthportPortId =
        "southport";
    private readonly Dictionary<string, string> _helmShips = new()
    {
        ["sailing_helm"] = "Sea Sprite",
        ["sailing_helm_black_pearl"] = "Black Pearl"
    };

    private readonly ShipRegistryService _registry;
    private readonly Dictionary<string, IslandLocation> _initialStoragePorts = new(StringComparer.Ordinal);
    private NwModule? _module;
    private bool _disposed;
    private readonly ShipMovementService _movement;
    private readonly ShipPortService _ports;
    private readonly ShipRetrievalNuiService _retrieval;
    private readonly ShipPartyTransitionService _partyTravel;
    private readonly HostileBoardingService? _hostileBoarding;
    private IReadOnlyCollection<ShipState> Ships => _registry.GetShips();
    private readonly ShipSpellEffectStateService _shipSpellEffectStateService;
    private readonly Dictionary<string, string> _playerShips = new();

    private readonly SailingAreaService _sailingAreaService;
    private readonly MerchantTradeService _merchantTradeService;
    private readonly ShipTradeNuiService _tradeNui;
    private readonly MerchantPortService _merchantPortService;
    private readonly SailingNuiService _sailingNuiService;

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

    private readonly ShipStorageService
        _shipStorageService;
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    public HelmService(
        ShipRegistryService registry,
        ShipMovementService movement,
        ShipPortService ports,
        ShipRetrievalNuiService retrieval,
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
        ShipTradeNuiService tradeNui,
        ShipSpellEffectStateService shipSpellEffectStateService,
        ShipStorageService shipStorageService,
        SailingAreaService sailingAreaService, ShipPartyTransitionService partyTravel, HostileBoardingService? hostileBoarding = null)
    {
        _registry = registry;
        _movement = movement;
        _ports = ports;
        _retrieval = retrieval;
        _partyTravel = partyTravel;
        _hostileBoarding = hostileBoarding;
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
        _physicalShipService.PlayerDetached += HandlePlayerDetached;

        _chartDiscoveryService = chartDiscoveryService;

        _merchantPortService = merchantPortService;

        _pirateAiService =
    pirateAiService;

        _merchantTradeService =
        merchantTradeService;
        _tradeNui = tradeNui;

        _shipStorageService =
        shipStorageService;

        _shipSpellEffectStateService =
        shipSpellEffectStateService;

        _shipEncounterService.EncounterStarted +=
    HandleShipEncounterStarted;

        _shipEncounterService.EncounterEnded +=
            HandleShipEncounterEnded;

        _shipBoardingService.BoardingCompleted -=
    HandleBoardingCompleted;

        _shipBoardingService.BoardingCompleted +=
            HandleBoardingCompleted;
    }

    internal void InitializeModule(NwModule module)
    {
        if (_disposed || _module != null) return;
        module.OnClientLeave += HandleClientLeave;
        _module = module;
    }

    public void RefreshWorldInteractions()
    {
        if (_disposed || _module == null) return;
        foreach (string helmTag in _helmShips.Keys)
        {
            foreach (NwPlaceable helm in
                NwObject.FindObjectsWithTag<NwPlaceable>(helmTag))
            {
                helm.OnLeftClick -= HandleHelmClick;
                helm.OnLeftClick += HandleHelmClick;
            }
        }

        foreach (IslandLocation island in
            _islandService.GetIslands())
        {
            if (string.IsNullOrWhiteSpace(
                    island.ShipyardTag))
            {
                continue;
            }

            foreach (NwPlaceable shipyard in
                NwObject.FindObjectsWithTag<NwPlaceable>(
                    island.ShipyardTag))
            {
                shipyard.OnLeftClick -=
                    HandleShipyardClick;

                shipyard.OnLeftClick +=
                    HandleShipyardClick;

                //Log.Info(
                //$"Shipyard interaction registered: " +
                //$"Island={island.Id}, " +
                //$"Tag={island.ShipyardTag}, " +
                //$"Area={shipyard.Area?.ResRef}");
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
        _physicalShipService.RegisterRepresentation(definition.ShipName, definition.PlaceableTag);
        if (string.IsNullOrWhiteSpace(definition.HelmTag)) return;
        if (_helmShips.TryGetValue(definition.HelmTag, out string? registered) && registered != definition.ShipName)
            throw new InvalidOperationException($"Helm tag '{definition.HelmTag}' is assigned to two ships.");
        _helmShips[definition.HelmTag] = definition.ShipName;

        if (_disposed || _module == null) return;

        foreach (NwPlaceable helm in NwObject.FindObjectsWithTag<NwPlaceable>(definition.HelmTag))
        {
            helm.OnLeftClick -= HandleHelmClick;
            helm.OnLeftClick += HandleHelmClick;
        }
        //Log.Info(
        //$"Registered helm '{definition.HelmTag}' for {definition.ShipName}.");
    }

    private void HandlePlayerDetached(string shipName, string playerName)
    {
        bool matches = !_playerShips.TryGetValue(playerName, out string? association) || association == shipName;
        LeaveHelm(shipName, playerName);
        if (!matches) return;
        _playerShips.Remove(playerName);
        _shipBoardingService.CancelPlayer(playerName);
        NwPlayer? player = NwModule.Instance.Players.FirstOrDefault(p => p.PlayerName == playerName);
        if (player != null)
        {
            player.OnNuiEvent -= HandleSailingNuiEvent;
            _sailingNuiService.Close(player);
        }
    }

    private void HandleClientLeave(ModuleEvents.OnClientLeave evt)
    {
        foreach (ShipState ship in Ships.Where(s => s.HelmsmanPCKey == evt.Player.PlayerName))
            LeaveHelm(ship.ShipName, evt.Player.PlayerName);
        _shipBoardingService.CancelPlayer(evt.Player.PlayerName);
        _playerShips.Remove(evt.Player.PlayerName);
        _sailingNuiService.Close(evt.Player);
    }

    private readonly ShipRoutePlannerService
        _shipRoutePlannerService;


    private void HandleHelmClick(
    PlaceableEvents.OnLeftClick obj)
    {
        //Log.Info(
        //$"Sailing helm clicked: " +
        //$"Tag={obj.Placeable.Tag}, " +
        //$"ResRef={obj.Placeable.ResRef}");

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

                LeaveHelm(shipName, player.PlayerName);

                player.SendServerMessage(
                    "You must be aboard the ship to take the helm.");

                return;
            }

            player.SendServerMessage(
                $"You take the helm of the {shipName}.");

            //Log.Info(
            //$"Player {player.PlayerName} " +
            //$"took the helm of the {shipName}.");


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

            //Log.Info(
            //$"Player {player.PlayerName} attempted to take " +
            //$"the helm of the {shipName}, " +
            //$"but it was occupied.");
        }
    }

    private void HandleBoardTestClick(PlaceableEvents.OnLeftClick evt)
    { if (GetShip("Sea Sprite") is { } ship) _partyTravel.OpenBoarding(evt.ClickedBy, ship, evt.Placeable); }
    private void HandleBoardingPointClick(PlaceableEvents.OnLeftClick evt)
    {
        NwPlayer player = evt.ClickedBy;
        string? shipName = player.LoginCreature?.GetObjectVariable<LocalVariableString>("SAILING_DOCKED_SHIP").Value;
        if (shipName == null || GetShip(shipName) is not { } ship) return;
        _partyTravel.OpenBoarding(player, ship, evt.Placeable);
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

        if (distance <= island.DockRadius && !string.IsNullOrWhiteSpace(island.ShipyardTag))
        {
            ship.CanDock = true;
            ship.NearbyIslandId = island.Id;

            //Log.Info(
            //$"Ship '{ship.ShipName}' entered docking range of {island.Name}.");
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
        if (!_sailingNuiService.OwnsEvent(obj)) return;

        if (obj.EventType == NuiEventType.Close)
        {
            if (_playerShips.TryGetValue(obj.Player.PlayerName, out string? currentShip))
                LeaveHelm(currentShip, obj.Player.PlayerName);
            _playerShips.Remove(obj.Player.PlayerName);
            obj.Player.OnNuiEvent -= HandleSailingNuiEvent;
            _sailingNuiService.Close(obj.Player);
            return;
        }
        //Log.Info(
        //$"Sailing NUI event: " +
        //$"Player={obj.Player.PlayerName}, " +
        //$"Event={obj.EventType}, " +
        //$"Element={obj.ElementId}");

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

        if (!ship.IsActiveWorld || ship.HelmsmanPCKey != obj.Player.PlayerName ||
            !_physicalShipService.IsPlayerPresent(shipName, obj.Player)) return;

        if (obj.EventType == NuiEventType.Click && obj.ElementId == "sailing_target_select")
        {
            _sailingNuiService.SelectTarget(obj.Player, ship, obj.ArrayIndex);
            _sailingNuiService.Update(obj.Player, ship, Ships);
            return;
        }
        if (obj.EventType == NuiEventType.MouseUp)
        {
            switch (obj.ElementId)
            {
                case "ahead_button":
                case "stop_button":
                case "astern_button":
                case "left_button":
                case "right_button":
                    HandleNavigationCommand(ship, obj.ElementId);
                    break;

                case "test_navigation_button":
                    TestNavigation(
                    shipName);
                    break;

                case "hail_contact_button":
                    HailOceanContact(shipName, obj.Player);
                    break;

                case "hail_button":
                    HailTarget(
                        shipName,
                        obj.Player);
                    break;
                case "port_market_button":
                    if (GetShip(shipName) is { } marketShip) _tradeNui.OpenPort(obj.Player, marketShip);
                    break;
                case "trade_button":
                    TradeWithTarget(
                        shipName,
                        obj.Player);
                    break;
                case "dock_button":
                    ObserveTransfer(DockShip(shipName, obj.Player));
                    return;

                case "board_button":
                    RequestBoarding(
                        shipName,
                        obj.Player, _sailingNuiService.GetSelectedSailingTarget(obj.Player, ship));
                    break;

                case "hostile_board_button":
                    if (_hostileBoarding != null) ObserveTransfer(_hostileBoarding.BoardAsync(obj.Player, ship,
                        _sailingNuiService.GetSelectedSailingTarget(obj.Player, ship)));
                    break;

                case "accept_board_button":
                    ObserveTransfer(AcceptBoarding(shipName, obj.Player));
                    break;

                case "reject_board_button":
                    RejectBoarding(
                        shipName,
                        obj.Player);
                    break;

                case "attack_button":
                    _ = AttackTarget(
                    shipName,
                   obj.Player, _sailingNuiService.GetSelectedSailingTarget(obj.Player, ship));
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
    Ships);
        }

        if (obj.EventType == NuiEventType.MouseUp &&
            obj.ElementId == "leave_button")
        {
            LeaveHelm(shipName, obj.Player.PlayerName);
            ship.Underway = false;
            _playerShips.Remove(
                obj.Player.PlayerName);

            obj.Player.SendServerMessage(
                $"You leave the helm of the {shipName}.");

            _sailingNuiService.Close(
                obj.Player);

            obj.Player.OnNuiEvent -=
                HandleSailingNuiEvent;

            //Log.Info(
            //$"Player {obj.Player.PlayerName} " +
            //$"left the helm of the {shipName}.");

            return;
        }

    }

    public async Task AttackTarget(
        string shipName,
        NwPlayer player, ShipState? target)
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
                await _shipCombatService.TryAttack(ship, target, player);

            // Keep native reporting on the NWN main thread. Persistence is queued
            // independently and is not a durability guarantee for this hit.

            await NwTask.SwitchToMainThread();

            // -----------------------------------------------------------------
            // Cooldown
            // -----------------------------------------------------------------

            if (result.HasNoWeapons)
            { player.SendServerMessage("This vessel is unarmed and cannot make ship weapon attacks. Ship spellcasting follows the normal caster rules."); return; }
            if (result.IsNotAuthorized || result.InvalidAttacker || result.AttackerDisabled)
            {
                player.SendServerMessage(result.IsNotAuthorized ? "Only the aboard captain may operate weapons." : "Your ship cannot attack in its current state.");
                return;
            }

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

                //Log.Info(
                //$"Player {playerName} attempted to attack " +
                //$"from '{ship.ShipName}', but weapons are " +
                //$"reloading. " +
                //$"Remaining={result.CooldownRemaining.TotalSeconds:0.00}s.");

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

                //Log.Info(
                //$"Player {playerName} attempted to attack " +
                //$"from '{ship.ShipName}', but no valid target exists.");

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

                //Log.Info(
                //$"Player {playerName} attempted to attack " +
                //$"from '{ship.ShipName}', but the target was " +
                //$"out of weapon range. " +
                //$"Distance={result.Distance:0.00}, " +
                //$"Range={result.Weapon.MaxRange:0.00}.");

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

                //Log.Info(
                //$"Player {playerName} attempted to attack " +
                //$"from '{ship.ShipName}', but the target was " +
                //$"outside the weapon firing arc. " +
                //$"Weapon={result.Weapon.DisplayName}, " +
                //$"Arc={result.Weapon.Arc}.");

                return;
            }

            // -----------------------------------------------------------------
            // Disabled target
            // -----------------------------------------------------------------

            if ((!result.Success && result.TargetDisabled) ||
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
                $"Hull: {result.PreviousHull}/{targetShip.MaximumHull} → " +
                $"{targetShip.Hull}/{targetShip.MaximumHull}";

            player.SendServerMessage(
                $"You fire upon the {targetShip.ShipName}.");

            player.SendServerMessage(
                $"{targetShip.ShipName} hull: " +
                $"{targetShip.Hull}/{targetShip.MaximumHull}.");

            _sailingNuiService.ShowCombatMessage(
                player,
                combatMessage);

            //Log.Info(
            //$"Player {playerName} attacked " +
            //$"'{targetShip.ShipName}' from " +
            //$"'{ship.ShipName}'. " +
            //$"Weapon={result.Weapon.DisplayName}, " +
            //$"Damage={result.Damage}, " +
            //$"Hull={result.PreviousHull}->{targetShip.Hull}.");

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

                //Log.Info(
                //$"Ship '{targetShip.ShipName}' has been disabled " +
                //$"by '{ship.ShipName}'.");
            }

            // -----------------------------------------------------------------
            // Refresh attacker's NUI
            // -----------------------------------------------------------------

            _sailingNuiService.Update(
                player,
                ship,
                Ships);
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

        //Log.Info(
        //$"Player {playerName} equipped " +
        //$"'{weapon.DisplayName}' on " +
        //$"ship '{ship.ShipName}'.");

        _sailingNuiService.Update(
            player,
            ship,
            Ships);
    }

    private ShipState? SpawnContactAsShip(OceanContact contact)
    {
        ShipState? ship = _registry.GetShips().FirstOrDefault(s => s.ShipId == contact.Id);
        if (ship == null)
        {
            ship = OceanContactShipFactory.Create(contact);
            if (ship == null || _registry.GetShip(ship.ShipName) != null) return null;
            _registry.Register(ship);
        }
        // Commit conversion before optional representation work. Asset failures cannot
        // leave a contact repeatedly attempting to register the same ship every tick.
        contact.Spawned = contact.ConvertedToShip = true;
        _oceanContactService.RemoveContact(contact);
        if (!string.IsNullOrWhiteSpace(contact.ShipResRef) && !string.IsNullOrWhiteSpace(contact.ShipTag))
            try { _physicalShipService.EnsureRepresentation(ship, contact.ShipResRef, contact.ShipTag); }
            catch (Exception ex) { Log.Warn(ex, "Contact {0} converted as an abstract ship; representation could not be created.", contact.Id); }
        _ = _shipStatePersistenceService.SaveState(ship);
        return ship;
    }
    //boarding check

    //docking
    private async Task DockShip(string shipName, NwPlayer player)
    {
        ShipState? ship = GetShip(shipName);
        if (ship != null && !await _ports.DockAsync(ship, player) && player.IsValid)
            player.SendServerMessage("Your ship cannot dock here.");
    }
    //boarding
    private void BoardShip(string shipName, NwPlayer? player)
    {
        if (player != null) ObserveTransfer(_physicalShipService.BoardShipAsync(shipName, player));
    }
    //hail
    private void HailTarget(string shipName, NwPlayer player)
    {
        ShipState? source = GetShip(shipName);
        ShipState? target = source == null ? null : _sailingNuiService.GetSelectedSailingTarget(player, source);
        if (source == null || target == null || !_shipEncounterService.IsInEncounter(source, target))
        { player.SendServerMessage("Select a nearby ship to hail."); return; }
        player.SendServerMessage($"You hail the {target.ShipName}.");
        if (target.ShipType == ShipType.Merchant) player.SendServerMessage($"The {target.ShipName} acknowledges your signal.");
        if (target.HelmsmanPCKey != null)
            NwModule.Instance.Players.FirstOrDefault(p => p.PlayerName == target.HelmsmanPCKey)?
                .SendServerMessage($"The {source.ShipName} is hailing you.");
    }

    private void HailOceanContact(string shipName, NwPlayer player)
    {
        ShipState? source = GetShip(shipName);
        OceanContact? contact = source == null ? null : _oceanContactService.GetClosestContact(source);
        if (source == null || contact == null || MathF.Sqrt(MathF.Pow(contact.X - source.X, 2) + MathF.Pow(contact.Y - source.Y, 2)) > OceanContactService.AttackRange)
        { player.SendServerMessage("There is no ocean contact within hailing range."); return; }
        player.SendServerMessage(contact.Type switch
        {
            EncounterType.Merchant => $"The {contact.Name} acknowledges your signal.",
            EncounterType.Pirate => $"The {contact.Name} raises black colors and ignores your hail.",
            _ => $"The {contact.Name} offers no reply."
        });
    }

    public void RequestBoarding(
    string shipName,
    NwPlayer player, ShipState? target)
    {
        ShipState? ship =
            GetShip(shipName);

        if (ship == null)
        {
            return;
        }

        if (_shipBoardingService.TryRequestBoarding(
                ship,
                target,
                player.PlayerName,
                out ShipBoardingRequest? request) &&
            request != null)
        {
            player.SendServerMessage(
                $"You request permission to board the " +
                $"{request.TargetShip.ShipName}.");

            //Log.Info(
            //$"Player {player.PlayerName} requested boarding: " +
            //$"{request.RequestingShip.ShipName} -> " +
            //$"{request.TargetShip.ShipName}.");

            _sailingNuiService.Update(
             player,
             ship,
             Ships);

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
          Ships);
                }
            }

            return;
        }

        player.SendServerMessage(
            "You cannot request boarding right now.");

        //Log.Info(
        //$"Player {player.PlayerName} could not request " +
        //$"boarding from '{shipName}'.");
    }

    private async Task AcceptBoarding(
        string shipName,
        NwPlayer player)
    {
        ShipBoardingRequest? request = await _shipBoardingService.TryAcceptBoardingAsync(player.PlayerName);
        if (!player.IsValid) return;
        if (request != null)
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
            Ships);
            }

            //Log.Info(
            //$"Player {player.PlayerName} accepted boarding " +
            //$"from '{request.RequestingShip.ShipName}'.");
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
             Ships);
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

            //Log.Info(
            //$"Player {player.PlayerName} rejected boarding " +
            //$"from '{request.RequestingShip.ShipName}'.");
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
             Ships);
        }

        //Log.Info(
        //$"Boarding completed: " +
        //$"{request.RequestingPlayerName} is now aboard " +
        //$"{request.TargetShip.ShipName}.");
    }

    public ShipState CreateShip(ShipDefinition definition)
    {
        definition.Validate();
        IslandLocation? initialPort = definition.InitialStoredPortId == null ? null :
            _islandService.GetIslands().FirstOrDefault(i => i.Id == definition.InitialStoredPortId);
        if (definition.InitialStoredPortId != null && initialPort == null)
            throw new ArgumentException("Ship default storage port is not configured.", nameof(definition));
        if (!_sailingAreaService.TryGetArea(definition.OceanAreaResRef, out SailingArea? configuredArea) || configuredArea == null ||
            definition.X < configuredArea.MinX || definition.X > configuredArea.MaxX ||
            definition.Y < configuredArea.MinY || definition.Y > configuredArea.MaxY)
            throw new ArgumentException("Ship definition is outside the configured sailing world.", nameof(definition));
        if (_helmShips.TryGetValue(definition.HelmTag, out string? assigned) && assigned != definition.ShipName)
            throw new ArgumentException("Ship definition reuses another ship's helm tag.", nameof(definition));
        ShipState ship = new()
        {
            ShipName = definition.ShipName,
            SpritePrefix = definition.SpritePrefix,
            DeckAreaResRef = definition.DeckAreaResRef,
            CabinAreaResRef = definition.CabinAreaResRef,
            AreaResRef = definition.OceanAreaResRef,
            ShipType = definition.ShipType,
            X = definition.X,
            Y = definition.Y,
            Z = definition.Z,
            Heading = definition.Heading,
            ShipId = definition.StableId,
            DefinitionId = definition.DefinitionId ?? definition.StableId,
            AllowsWeapons = definition.AllowsWeapons,
            RepresentationTag = definition.PlaceableTag,
            RentalPoolId = definition.DefinitionId == CutterRentalFleet.DefinitionId ? definition.InitialStoredPortId : null,
            MaximumHull = definition.Hull,
            Hull = definition.Hull,
            IsInitializing = _shipStatePersistenceService.IsEnabled,
            CargoCapacity = definition.CargoCapacity,
            WeaponResRef = definition.WeaponResRef
        };

        _registry.Register(ship);
        RegisterShipDefinition(definition);
        if (initialPort != null) _initialStoragePorts[ship.ShipId] = initialPort;
        // With persistence disabled there is no saved-state authority to query.
        // Enabled stores apply this default only after a successful missing-record lookup.
        if (!_shipStatePersistenceService.IsEnabled)
            _shipStorageService.InitializeDefaultStorage(ship, null, initialPort);

        //Log.Info(
        //$"Ship '{definition.ShipName}' created: " +
        //$"Area={ship.AreaResRef}, " +
        //$"X={ship.X}, " +
        //$"Y={ship.Y}, " +
        //$"Z={ship.Z}, " +
        //$"Heading={ship.Heading}.");



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

        if (!ship.IsActiveWorld || !_shipCombatService.CanOperateWeapons(ship.ShipName, player)) return;

        if (ship.Hull >= ship.MaximumHull)
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
          Ships);

            return;
        }

        int previousHull =
            ship.Hull;

        ship.RepairHull(RepairAmount);

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
            $"Hull: {previousHull}/{ship.MaximumHull} -> {ship.Hull}/{ship.MaximumHull}.");

        _sailingNuiService.ShowCombatMessage(
            player,
            $"⚒ SHIP REPAIRED\n" +
            $"{ship.ShipName}\n" +
            $"Hull: {previousHull}/{ship.MaximumHull} → {ship.Hull}/{ship.MaximumHull}");

        _sailingNuiService.Update(
          player,
          ship,
          Ships);

        //Log.Info(
        //$"Player {playerName} repaired " +
        //$"ship '{ship.ShipName}': " +
        //$"Hull={previousHull}->{ship.Hull}.");
    }
    public void RegisterHelm(ShipDefinition definition) => RegisterShipDefinition(definition);
    private Task? _initialization;
    private bool _catalogueLoaded;
    public Task InitializeFleet() => _initialization is { IsCompleted: false } ? _initialization :
        _initialization = InitializeFleetCore();

    private async Task InitializeFleetCore()
    {
        if (!_shipStatePersistenceService.IsEnabled) return;
        if (!_catalogueLoaded)
        {
            IReadOnlyCollection<SavedShipState> savedFleet = await _shipStatePersistenceService.LoadFleet();
            await NwTask.SwitchToMainThread();
            foreach (SavedShipState saved in savedFleet)
            {
                if (saved.ShipId == null || _registry.GetShip(saved.ShipId) != null) continue;
                ShipState? npc = ShipSnapshotCodec.CreateRestoredNpc(saved);
                if (npc == null) { Log.Warn("Saved dynamic ship {0} has no valid reconstructable NPC definition.", saved.ShipId); continue; }
                try { _registry.Register(npc); }
                catch (Exception ex) { Log.Error(ex, "Saved NPC registration failed for {0}.", saved.ShipId); }
            }
            _catalogueLoaded = true;
        }
        await Task.WhenAll(Ships.Where(s => s.IsInitializing).Select(s => LoadSavedShipState(s)));
        await NwTask.SwitchToMainThread();
        _oceanContactService.ReconcileRestoredShips(Ships);
    }

    public async Task<bool> CheckpointFleet()
    {
        if (!_shipStatePersistenceService.IsEnabled) return true;
        Task<bool>[] snapshots = Ships.Where(s => !s.IsInitializing && !s.IsDocking && !s.IsEconomyBusy).Select(_shipStatePersistenceService.TrySaveState).ToArray();
        return (await Task.WhenAll(snapshots).ConfigureAwait(false)).All(success => success);
    }

    public async Task RecoverPlayerContextAsync(NwPlayer player)
    {
        if (_hostileBoarding != null && await _hostileBoarding.RecoverPlayerAsync(player)) return;
        await NwTask.SwitchToMainThread();
        if (_physicalShipService.IsTransferPending(player.PlayerName) || _physicalShipService.RecoverPlayerMembership(player)) return;
        string? area = player.ControlledCreature?.Area?.ResRef;
        if (area == null) return;
        ShipState[] stored = Ships.Where(s => (s.IsStored || CutterRentalFleet.IsCutter(s)) && !s.IsInitializing &&
            _physicalShipService.GetPhysicalShip(s.ShipName) is { } mapping &&
            (string.Equals(area, mapping.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(area, mapping.CabinAreaResRef, StringComparison.OrdinalIgnoreCase))).ToArray();
        if (stored.Length == 1) await _ports.RecoverStoredPlayerAsync(stored[0], player);
    }

    private static async void ObserveTransfer(Task transfer)
    {
        try { await transfer; }
        catch (Exception ex) { Log.Error(ex, "Sailing player transfer interaction failed."); }
    }

    public void SynchronizeWorld()
    {
        RefreshWorldInteractions();
        _physicalShipService.RegisterPhysicalShipInteractions();
        foreach (ShipState ship in Ships.Where(s => !s.IsInitializing))
        {
            try
            {
                if (ship.ShipType != ShipType.Player && !string.IsNullOrWhiteSpace(ship.RepresentationTag) &&
                    !string.IsNullOrWhiteSpace(ship.RepresentationResRef))
                    _physicalShipService.EnsureRepresentation(ship, ship.RepresentationResRef, ship.RepresentationTag);
                _physicalShipService.Synchronize(ship);
            }
            catch (Exception ex) { Log.Error(ex, "Ship world synchronization failed for {0}.", ship.ShipId); }
        }
        foreach (NwPlayer player in NwModule.Instance.Players.ToArray())
            ObserveTransfer(RecoverPlayerContextAsync(player));
    }

    private async Task LoadSavedShipState(ShipState ship)
    {
        if (!_shipStatePersistenceService.IsEnabled) return;
        try
        {
            SavedShipState? saved = await _shipStatePersistenceService.LoadState(ship.ShipId, ship.ShipName);
            await NwTask.SwitchToMainThread();
            if (!_registry.IsCurrent(ship)) return;
            bool initializedStorage = false;
            if (saved != null)
            {
                if (!_sailingAreaService.TryGetArea(saved.AreaResRef, out SailingArea? area) || area == null ||
                    saved.X < area.MinX || saved.X > area.MaxX || saved.Y < area.MinY || saved.Y > area.MaxY)
                    throw new InvalidOperationException("Saved ship position is outside the configured sailing world.");
                if (!ShipSnapshotCodec.TryRestore(ship, saved, out ShipNavigationRoute? route))
                    throw new InvalidOperationException("Invalid saved ship snapshot.");
                if (ship.IsStored && !_islandService.GetIslands().Any(i => i.Id == ship.StoredPortId))
                    throw new InvalidOperationException("Saved storage port is not configured.");
                if (route != null)
                {
                    if (route.Waypoints.Any(w => !_sailingAreaService.TryGetArea(w.AreaResRef, out SailingArea? waypointArea) ||
                        waypointArea == null || w.X < waypointArea.MinX || w.X > waypointArea.MaxX ||
                        w.Y < waypointArea.MinY || w.Y > waypointArea.MaxY))
                        throw new InvalidOperationException("Saved route refers to an unknown area or an invalid position.");
                    _shipNavigationService.SetRoute(ship, route);
                    ship.Underway = !ship.IsInPort;
                }
                else if (ship.ShipType != ShipType.Merchant || saved.SnapshotJson != null)
                    _shipNavigationService.CancelNavigation(ship);
            }
            else
            {
                _initialStoragePorts.TryGetValue(ship.ShipId, out IslandLocation? initialPort);
                initializedStorage = _shipStorageService.InitializeDefaultStorage(ship, saved, initialPort);
            }
            if (initializedStorage)
            {
                _shipNavigationService.CancelNavigation(ship);
                _physicalShipService.Synchronize(ship);
                if (!await _shipStatePersistenceService.TrySaveInitialState(ship))
                    throw new InvalidOperationException("Initial stored ship snapshot could not be saved; retrieval remains blocked.");
                await NwTask.SwitchToMainThread();
                if (!_registry.IsCurrent(ship)) return;
            }
            ship.IsInitializing = false;
            if (ship.IsStored || ship.IsDisabled) _shipNavigationService.CancelNavigation(ship);
            else if (ship.ShipType == ShipType.Merchant && _shipNavigationService.GetRoute(ship) is { } installed)
            {
                // Bootstrap and restoration install routes behind the initialization gate.
                // Activate the destination only after saved state has been validated.
                _shipNavigationService.SetRoute(ship, installed);
                ship.Underway = !ship.IsInPort;
            }
            _physicalShipService.Synchronize(ship);
            if (saved == null && !initializedStorage) await _shipStatePersistenceService.SaveState(ship);
        }
        catch (Exception ex)
        {
            // Keep this ship excluded from the world until its saved state can be restored safely.
            ship.IsInitializing = true;
            Log.Error(ex, "Ship startup is blocked for {0}.", ship.ShipId);
        }
    }

    public ShipState? GetShip(string shipName) => _registry.GetShip(shipName);
    public IReadOnlyCollection<ShipState> GetShips() => Ships;

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

        //Log.Info(
        //$"Starting multi-area navigation test: " +
        //$"Ship={ship.ShipName}, " +
        //$"StartArea={ship.AreaResRef}, " +
        //$"Start=(" +
        //$"{ship.X:0.00}, " +
        //$"{ship.Y:0.00}), " +
        //$"DestinationArea={destinationArea}, " +
        //$"Destination=(" +
        //$"{destinationX:0.00}, " +
        //$"{destinationY:0.00})");

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

        //Log.Info(
        //$"Multi-area navigation route created: " +
        //$"Ship={ship.ShipName}, " +
        //$"Waypoints={route.Waypoints.Count}");

        for (
            int i = 0;
            i < route.Waypoints.Count;
            i++)
        {
            ShipNavigationWaypoint waypoint =
                route.Waypoints[i];

            //Log.Info(
            //$"Route waypoint [{i}]: " +
            //$"Description={waypoint.Description}, " +
            //$"Area={waypoint.AreaResRef}, " +
            //$"X={waypoint.X:0.00}, " +
            //$"Y={waypoint.Y:0.00}, " +
            //$"Z={waypoint.Z:0.00}");

            //Log.Info(
            //$"Route generated for '{shipName}': " +
            //string.Join(
            //" -> ",
            //route.Waypoints.Select(
            //wp =>
            //$"{wp.AreaResRef}({wp.X:0},{wp.Y:0})")));
        }

        return true;
    }
    public async Task NavigateAllShips()
    {
        _physicalShipService.CleanupPlayers();
        ShipState[] fleet = Ships.ToArray();
        _shipNavigationService.PruneRemovedShips(fleet);
        foreach (ShipState ship in fleet)
        {
            if (!ship.IsActiveWorld) continue;
            try
            {
                AdvanceShipMovement(ship);
                UpdateDockingState(ship);
                if (ship.ShipType != ShipType.Player || ship.IsDisabled || string.IsNullOrWhiteSpace(ship.HelmsmanPCKey)) continue;
                // Conversions mutate the registry, so movement enumerates a fleet snapshot.
                foreach (OceanContact contact in _oceanContactService.GetVisibleContacts(ship).ToArray())
                {
                    float distance = MathF.Sqrt(MathF.Pow(contact.X - ship.X, 2) + MathF.Pow(contact.Y - ship.Y, 2));
                    if (!contact.Discovered && distance <= OceanContactService.DiscoveryRange)
                    {
                        contact.Discovered = true;
                        NotifyHelmsman(ship, $"A {contact.Type} contact is spotted off the {GetRelativeBearing(ship, contact)}.");
                    }
                    if (!contact.ConvertedToShip && distance <= OceanContactService.SpawnRange &&
                        (contact.Type == EncounterType.Pirate || contact.Type == EncounterType.Merchant))
                    {
                        if (SpawnContactAsShip(contact) != null)
                            NotifyHelmsman(ship, $"The silhouette resolves into {contact.Name}.");
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex, "Navigation failed for {0}.", ship.ShipId); }
        }
        fleet = Ships.ToArray();
        _shipEncounterService.CheckEncounters(fleet); // Exactly one fleet reconciliation per tick.
        if (_hostileBoarding != null) await _hostileBoarding.TickAsync();
        await NwTask.SwitchToMainThread();
        _shipBoardingService.Cleanup();
        Dictionary<string, NwPlayer> players = NwModule.Instance.Players.ToDictionary(p => p.PlayerName, StringComparer.Ordinal);
        foreach (ShipState ship in fleet.Where(s => s.IsActiveWorld))
            foreach (string name in _physicalShipService.GetPlayersAboard(ship.ShipName))
                if (players.TryGetValue(name, out NwPlayer? player))
                    try { _chartDiscoveryService.RevealAroundShip(player, ship); }
                    catch (Exception ex) { Log.Error(ex, "Chart refresh failed for {0}.", name); }
        await _pirateAiService.UpdatePirates(fleet);
        await NwTask.SwitchToMainThread();
        foreach (ShipState pirate in fleet.Where(s => s.ShipType == ShipType.Pirate && s.IsActiveWorld))
            try { UpdatePhysicalShip(pirate); }
            catch (Exception ex) { Log.Error(ex, "Pirate representation refresh failed for {0}.", pirate.ShipId); }
        foreach (var viewer in _playerShips.ToArray())
        {
            if (!players.TryGetValue(viewer.Key, out NwPlayer? player)) { _playerShips.Remove(viewer.Key); continue; }
            ShipState? ship = GetShip(viewer.Value);
            if (ship == null)
            {
                _sailingNuiService.Close(player); // Retire chart overlays whose vessel left the registry.
                continue;
            }
            if (!ship.IsActiveWorld) continue;
            try { _sailingNuiService.Update(player, ship, fleet); }
            catch (Exception ex) { Log.Error(ex, "Sailing refresh failed for {0}.", viewer.Key); }
        }
    }

    // Shared with the fleet tick so command regression checks exercise its actual movement branch.
    private void AdvanceShipMovement(ShipState ship)
    {
        if (!ship.IsActiveWorld) return;
        if (_shipNavigationService.IsNavigating(ship)) NavigateShip(ship);
        else if (ship.Underway && !string.IsNullOrWhiteSpace(ship.HelmsmanPCKey))
            TryMoveShip(ship, SailingStep * _shipSpellEffectStateService.GetMovementMultiplier(ship));
    }

    // The NUI ownership/presence checks run before dispatch. Keep the commands usable
    // without native UI objects so regression checks can exercise the same handlers.
    private bool HandleNavigationCommand(ShipState ship, string elementId)
    {
        switch (elementId)
        {
            case "ahead_button":
                _shipNavigationService.CancelNavigation(ship);
                if (ship.IsActiveWorld && !ship.IsDisabled) ship.Underway = true;
                UpdateSailingNui(ship);
                return true;
            case "stop_button":
            case "astern_button":
                _shipNavigationService.CancelNavigation(ship);
                _ = _shipStatePersistenceService.SaveState(ship);
                UpdateSailingNui(ship);
                return true;
            case "left_button":
                TurnLeft(ship.ShipName);
                return true;
            case "right_button":
                TurnRight(ship.ShipName);
                return true;
            default:
                return false;
        }
    }

    public bool TakeHelm(
        string shipName,
        string pcKey)
    {
        ShipState? ship =
            GetShip(shipName);

        NwPlayer? actor = NwModule.Instance.Players.FirstOrDefault(p => p.IsValid && p.PlayerName == pcKey);
        if (ship == null || !ship.IsActiveWorld || actor == null || !_physicalShipService.CanCommandShip(ship, actor) || !_physicalShipService.IsPlayerPresent(shipName, actor))
        {
            Log.Warn(
                $"Cannot take helm: " +
                $"ship '{shipName}' does not exist.");

            return false;
        }

        if (ship.HelmsmanPCKey == pcKey) return true;
        if (ship.HelmsmanPCKey != null || Ships.Any(s => s.HelmsmanPCKey == pcKey))
        {
            return false;
        }

        ship.HelmsmanPCKey =
            pcKey;

        //Log.Info(
        //$"Ship '{shipName}' helm assigned to " +
        //$"PC '{pcKey}'.");

        return true;
    }

    public void LeaveHelm(
        string shipName, string? expectedPlayer = null)
    {
        ShipState? ship =
            GetShip(shipName);

        if (ship == null || (expectedPlayer != null && ship.HelmsmanPCKey != expectedPlayer))
        {
            return;
        }

        string? playerName =
            ship.HelmsmanPCKey;

        //Log.Info(
        //$"Ship '{shipName}' helm released from " +
        //$"PC '{playerName}'.");

        if (playerName != null) ShipCrewLifecycle.ReleaseOwnHelm(ship, playerName);

        if (playerName != null && _playerShips.GetValueOrDefault(playerName) == shipName) _playerShips.Remove(playerName);

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
                //Log.Info(
                //$"Player {playerName} is no longer " +
                //$"captain of '{shipName}' and is now crew.");
            }
        }

    }

    private void TurnLeft(
        string shipName)
    {
        ShipState? ship =
            GetShip(shipName);

        if (ship == null || !ship.IsActiveWorld || ship.IsDisabled) return;
        // Manual steering takes over the course without changing propulsion.
        _shipNavigationService.ClearRoute(ship);
        _shipNavigationService.ClearDestination(ship);

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

        if (ship == null || !ship.IsActiveWorld || ship.IsDisabled) return;
        // Manual steering takes over the course without changing propulsion.
        _shipNavigationService.ClearRoute(ship);
        _shipNavigationService.ClearDestination(ship);

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

        float movementDistance =
            SailingStep *
            _shipSpellEffectStateService.GetMovementMultiplier(ship);

        TryMoveShip(
            ship,
            movementDistance);

        UpdateSailingNui(
        ship);
    }

    private void TryMoveShip(ShipState ship, float distance)
    {
        if (!_movement.TryMove(ship, distance)) return;
        UpdatePhysicalShip(ship);
        _ = _shipStatePersistenceService.SaveState(ship);
    }
    public void NavigateShip(ShipState ship)
    {
        if (!_shipNavigationService.IsNavigating(ship)) return;
        if (ship.ShipType == ShipType.Merchant && ship.IsInPort)
        {
            if (_merchantPortService.UpdatePortStay(ship)) return;
            if (_shipNavigationService.AdvanceWaypoint(ship))
            {
                _shipNavigationService.CancelNavigation(ship);
                return;
            }
        }
        else if (_shipNavigationService.IsCurrentWaypointReached(ship))
        {
            ShipNavigationWaypoint? waypoint = _shipNavigationService.GetCurrentWaypoint(ship);
            if (ship.ShipType == ShipType.Merchant && !string.IsNullOrWhiteSpace(waypoint?.PortId) &&
                _merchantPortService.BeginPortStay(ship, waypoint))
            {
                _ = _shipStatePersistenceService.SaveState(ship);
                return;
            }
            // Advance ALL intermediate waypoints, including the source side of an area crossing.
            if (_shipNavigationService.AdvanceWaypoint(ship))
            {
                _shipNavigationService.CancelNavigation(ship);
                _ = _shipStatePersistenceService.SaveState(ship);
                return;
            }
        }
        if (_shipNavigationService.GetRoute(ship) == null && _shipNavigationService.IsDestinationReached(ship))
        {
            _shipNavigationService.CompleteNavigation(ship);
            ship.Underway = false;
            _ = _shipStatePersistenceService.SaveState(ship);
            return;
        }
        float distance = SailingStep * _shipSpellEffectStateService.GetMovementMultiplier(ship);
        Heading heading = _shipNavigationService.GetNavigationHeading(ship, distance);
        if (ship.Heading != heading)
        {
            ship.Heading = heading;
            UpdatePhysicalShip(ship);
        }
        TryMoveShip(ship, _shipNavigationService.GetMovementDistance(ship, heading, distance));
    }





    // -------------------------------------------------------------
    // Spawn Pirate Ship
    // -------------------------------------------------------------

    private void UpdatePhysicalShip(ShipState ship) => _physicalShipService.Synchronize(ship);

    private void LogShipState(
        ShipState ship)
    {
        //Log.Info(
        //$"Ship '{ship.ShipName}' state: " +
        //$"Area={ship.AreaResRef}, " +
        //$"X={ship.X}, " +
        //$"Y={ship.Y}, " +
        //$"Z={ship.Z}, " +
        //$"Heading={ship.Heading}, " +
        //$"Underway={ship.Underway}, " +
        //$"Hull={ship.Hull}");
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
         Ships);
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

        //Log.Info(
        //$"Player encounter notification: " +
        //$"Player={player.PlayerName}, " +
        //$"Ship={ship.ShipName}, " +
        //$"Target={targetShip.ShipName}, " +
        //$"Distance={encounter.Distance:0.00}");


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



        //Log.Info(
        //$"Player encounter ended: " +
        //$"Player={player.PlayerName}, " +
        //$"Ship={ship.ShipName}, " +
        //$"OtherShip=" +
        //$"{(ReferenceEquals(encounter.ShipA, ship) ? encounter.ShipB.ShipName : encounter.ShipA.ShipName)}");
    }
    private void TradeWithTarget(string shipName, NwPlayer player)
    {
        ShipState? source = GetShip(shipName);
        if (source != null) _tradeNui.Open(player, source, _sailingNuiService.GetSelectedSailingTarget(player, source));
    }

    private void HandleShipyardClick(
        PlaceableEvents.OnLeftClick obj)
    {
        NwPlayer player =
            obj.ClickedBy;

        //Log.Info(
        //$"Shipyard clicked: " +
        //$"Player={player.PlayerName}, " +
        //$"Tag={obj.Placeable.Tag}, " +
        //$"Area={obj.Placeable.Area?.ResRef}");

        IslandLocation? island =
            _islandService.GetIslandByShipyardTag(
                obj.Placeable.Tag);

        if (island == null)
        {
            Log.Warn(
                $"No island configured for shipyard tag " +
                $"'{obj.Placeable.Tag}'.");

            player.SendServerMessage(
                "This shipyard is not properly configured.");

            return;
        }

        if (!string.Equals(obj.Placeable.Area?.ResRef, island.LandingArea, StringComparison.OrdinalIgnoreCase) ||
            obj.Placeable.Area != player.ControlledCreature?.Area)
        { player.SendServerMessage("This shipyard is outside its configured port area."); return; }
        _retrieval.Open(player, island.Id);
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shipEncounterService.EncounterStarted -= HandleShipEncounterStarted;
        _shipEncounterService.EncounterEnded -= HandleShipEncounterEnded;
        _shipBoardingService.BoardingCompleted -= HandleBoardingCompleted;
        _physicalShipService.PlayerDetached -= HandlePlayerDetached;
        NwModule? module = _module;
        _module = null;
        if (module == null) return;
        module.OnClientLeave -= HandleClientLeave;
        if (!module.IsValid) return;
        foreach (NwPlayer player in module.Players.ToArray())
        { player.OnNuiEvent -= HandleSailingNuiEvent; _sailingNuiService.Close(player); }
        foreach (string tag in _helmShips.Keys)
            foreach (NwPlaceable obj in NwObject.FindObjectsWithTag<NwPlaceable>(tag)) obj.OnLeftClick -= HandleHelmClick;
        foreach (IslandLocation island in _islandService.GetIslands())
            if (island.ShipyardTag != null)
                foreach (NwPlaceable obj in NwObject.FindObjectsWithTag<NwPlaceable>(island.ShipyardTag)) obj.OnLeftClick -= HandleShipyardClick;
        foreach (NwPlaceable obj in NwObject.FindObjectsWithTag<NwPlaceable>("ship_boarding_point")) obj.OnLeftClick -= HandleBoardingPointClick;
        foreach (NwPlaceable obj in NwObject.FindObjectsWithTag<NwPlaceable>("board_test")) obj.OnLeftClick -= HandleBoardTestClick;
    }
}
